using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

using Microsoft.Xna.Framework;

using Beebo;
using Beebo.GameContent;

using Semver;
using System.Text.Json.Serialization;
using System.Linq;

namespace Beebo.Mods;

public static class ModLoader
{
    private static int loadNum;

    internal static readonly List<LoadedMod> loadedMods = [];
    internal static readonly List<string> loadedPaths = [];
    internal static readonly List<string> loadedGuids = [];

    internal static readonly List<string> resolvedGuids = [];

    internal static readonly Dictionary<string, ModJson> nonStandardMods = [];

    public static string ModsPathPath => Path.Combine(Main.ProgramPath, "mods");

    private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
    {
        return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
    }

    internal static void DoBeforeRun()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

        Directory.CreateDirectory(ModsPathPath);
        foreach(var modFolder in Directory.EnumerateDirectories(ModsPathPath))
        {
            foreach(var fullPath in Directory.EnumerateFiles(modFolder, "*.dll"))
            {
                Main.Logger.LogInfo($"Found assembly {Path.GetFileName(fullPath)} ({fullPath})");

                ReadAssembly(fullPath, modFolder, true);

                if(!loadedPaths.Contains(modFolder))
                    loadedPaths.Add(modFolder);
            }

            var libPath = Path.Combine(modFolder, "lib");
            if(Directory.Exists(libPath))
            {
                foreach(var fullPath in Directory.EnumerateFiles(libPath, "*.dll", SearchOption.AllDirectories))
                {
                    Main.Logger.LogInfo($"Found assembly {Path.GetFileName(fullPath)} ({fullPath})");

                    ReadAssembly(fullPath, modFolder, false);

                    if(!loadedPaths.Contains(modFolder))
                        loadedPaths.Add(modFolder);
                }
            }
        }

        Main.Logger.LogInfo($"Mods to load:\n  - {string.Join("\n  - ", loadedGuids)}");

        resolvedGuids.Clear();

        for(int i = 0; i < loadedMods.Count; i++)
        {
            LoadedMod mod = loadedMods[i];

            Main.Logger.LogInfo($"Resolving dependencies for {mod.Guid}...");

            ResolveDependencies(mod);

            if(mod.failedLoad)
            {
                loadedMods.RemoveAt(i);
                loadedGuids.Remove(mod.Guid);
                i--;
                continue;
            }

            Main.Logger.LogInfo("done");
        }

        // post import

        foreach(var mod in loadedMods)
        {
            if(!mod.isNonStandard)
            {
                mod.Instance = (Mod)mod.MainClass.GetConstructor(Type.EmptyTypes).Invoke(null);
                mod.Instance.logger = new(mod.DisplayName);
                mod.Instance.OnBeforeRun();
            }
        }
    }

    private static void ReadAssembly(string path, string basePath, bool standard)
    {
        Assembly assembly = null;

        try
        {
            assembly = Assembly.Load(File.ReadAllBytes(path));
        }
        catch(Exception e)
        {
            var name = assembly?.FullName ?? Path.GetFileNameWithoutExtension(path);

            Main.Logger.LogError(
                new Exception($"Failed to load assembly {name} ({Path.GetRelativePath(Main.ProgramPath, path)})", e)
            );

            return;
        }

        if(standard)
        {
            foreach(var type in assembly.GetTypes())
            {
                try
                {
                    if(type.IsSubclassOf(typeof(Mod)) && type.IsPublic)
                    {
                        LoadedMod mod = new() {
                            Assembly = assembly,
                            BasePath = basePath,
                            MainClass = type
                        };

                        ImportMod(mod);
                        return;
                    }
                }
                catch(Exception e)
                {
                    Main.Logger.LogError(new Exception($"Failed to load assembly {assembly.FullName} ({path})", e));
                    return;
                }
            }
        }
        else if(!loadedPaths.Contains(basePath))
        {
            // fallback for libraries and assemblies that lack a Mod class
            // in this case the mod *must* have a mod.json file at its root

            var jsonPath = Path.Combine(basePath, "mod.json");

            if(!File.Exists(jsonPath))
            {
                Main.Logger.LogError(new Exception($"Failed to load assembly {assembly.FullName} ({path})", new Exception("The mod is library-only but does not contain the required mod.json file")));
                return;
            }

            ModJson modJson = JsonSerializer.Deserialize<ModJson>(File.ReadAllText(jsonPath), new JsonSerializerOptions {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = {
                    new JsonStringEnumConverter<ModDependency.DependencyKind>(),
                    new JsonStringEnumConverter<ModDependency.VersionSpecificity>(),
                },
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            });

            nonStandardMods.Add(modJson.Guid, modJson);

            LoadedMod mod = new()
            {
                isNonStandard = true,
                Assembly = assembly,
                Guid = modJson.Guid,
                DisplayName = modJson.Name ?? modJson.Guid,
                Version = SemVersion.Parse(modJson.Version),
                VersionString = modJson.Version,
            };

            loadedMods.Add(mod);
            loadedGuids.Add(mod.Guid);
        }
    }

    private static void ImportMod(LoadedMod mod)
    {
        if(mod.MainClass.GetCustomAttribute<ModInfoAttribute>() is not ModInfoAttribute modInfo)
        {
            throw new Exception($"Mod class is missing ModInfo attribute");
        }

        mod.Guid = modInfo.Guid ?? throw new NullReferenceException("ModInfo GUID cannot be null");
        mod.DisplayName = modInfo.DisplayName ?? mod.Guid;

        mod.VersionString = modInfo.VersionString ?? throw new NullReferenceException("ModInfo Version cannot be null");
        mod.Version = modInfo.Version;
        // can safely assume that if VersionString exists, then Version must exist

        if(modInfo.DisplayName == null)
        {
            Main.Logger.LogWarning($"{mod.Guid} does not have a display name set and will use its GUID instead");
        }

        loadedMods.Add(mod);
        loadedGuids.Add(mod.Guid);
    }

    private static void ResolveDependencies(LoadedMod mod)
    {
        if(resolvedGuids.Contains(mod.Guid)) return;

        try
        {
            List<Exception> errors = [];

            if(!mod.isNonStandard)
            {
                foreach(var attr in Attribute.GetCustomAttributes(mod.MainClass, typeof(ModDependency), false))
                {
                    var dep = (ModDependency)attr;
                    ReadDependency(mod, errors, dep);
                }
            }
            else
            {
                var jsonMod = nonStandardMods[mod.Guid];
                HashSet<ModDependency> dependencyList = new(
                    [
                        ..jsonMod.Dependencies?.Incompatible ?? [],
                        ..jsonMod.Dependencies?.Required ?? [],
                        ..jsonMod.Dependencies?.Optional ?? []
                    ],
                    ModDependency.GetGuidEqualityComparer()
                );

                foreach(var dep in dependencyList)
                {
                    ReadDependency(mod, errors, dep);
                }
            }

            if (errors.Count > 0)
                throw new Exception($"Failed to resolve dependencies for {mod.Guid} due to the following errors:\n  {string.Join("\n  ", errors)}");
        }
        catch(Exception e)
        {
            Main.Logger.LogError(e);
            mod.failedLoad = true;
            resolvedGuids.Add(mod.Guid);
            return;
        }

        resolvedGuids.Add(mod.Guid);
        mod.loadIndex = loadNum++;
    }

    private static void ReadDependency(LoadedMod mod, List<Exception> errors, ModDependency dep)
    {
        bool depIsAvailable = false;

        int index = loadedGuids.IndexOf(dep.Guid);
        if (index != -1)
        {
            string message = null;
            string dependencyKindText = "requires a";
            if (dep.Kind == ModDependency.DependencyKind.Incompatible)
                dependencyKindText = "is incompatible with any";

            if (dep.MinimumVersion != null)
            {
                if (dep.MaximumVersion != null)
                {
                    // min and max
                    if (dep.Kind == ModDependency.DependencyKind.Incompatible ^
                        loadedMods[loadedGuids.IndexOf(dep.Guid)].Version.Satisfies(
                            UnbrokenSemVersionRange.Inclusive(
                                SemVersion.Parse(dep.MinimumVersion, (SemVersionStyles)dep.Specificity).WithoutMetadata(),
                                SemVersion.Parse(dep.MaximumVersion, (SemVersionStyles)dep.Specificity).WithoutMetadata(),
                                true
                            )
                        )
                    )
                    {
                        ResolveDependencies(loadedMods[index]);
                        depIsAvailable = true;
                    }
                    else
                        message = $"{mod.Guid} {dependencyKindText} version of {dep.Guid} that falls within the range {GetFancy(dep.MinimumVersion, dep.Specificity)}..{GetFancy(dep.MaximumVersion, dep.Specificity)}, but {loadedMods[index].Version} was found";
                }
                else
                {
                    // min
                    if (dep.Kind == ModDependency.DependencyKind.Incompatible ^
                        loadedMods[loadedGuids.IndexOf(dep.Guid)].Version.Satisfies(
                            UnbrokenSemVersionRange.AtLeast(
                                SemVersion.Parse(dep.MinimumVersion, (SemVersionStyles)dep.Specificity).WithoutMetadata(), true
                            )
                        )
                    )
                    {
                        ResolveDependencies(loadedMods[index]);
                        depIsAvailable = true;
                    }
                    else
                        message = $"{mod.Guid} {dependencyKindText} version of {dep.Guid} that is greater than or equal to {GetFancy(dep.MinimumVersion, dep.Specificity)}, but {loadedMods[index].Version} was found";
                }
            }
            else
            {
                // no specific version
                if (dep.Kind == ModDependency.DependencyKind.Incompatible)
                {
                    message = $"{mod.Guid} is incompatible with {dep.Guid}, but {dep.Guid} was found";
                }
                else
                {
                    ResolveDependencies(loadedMods[index]);
                    depIsAvailable = true;
                }
            }

            if (message != null)
            {
                errors.Add(new Exception(message));
            }

            if (dep.Kind != ModDependency.DependencyKind.Incompatible)
            {
                mod.availableDependencies.Add(dep.Guid, depIsAvailable);
            }

            return;
        }

        if (dep.Kind == ModDependency.DependencyKind.Required)
        {
            string message = "";
            if (dep.MinimumVersion != null)
            {
                if (dep.MaximumVersion != null)
                {
                    // min and max
                    message = $"{mod.Guid} requires a version of {dep.Guid} that falls within the range {GetFancy(dep.MinimumVersion, dep.Specificity)}..{GetFancy(dep.MaximumVersion, dep.Specificity)}, but {dep.Guid} is missing";
                }
                else
                {
                    // min
                    message = $"{mod.Guid} requires a version of {dep.Guid} that is greater than or equal to {GetFancy(dep.MinimumVersion, dep.Specificity)}, but {dep.Guid} is missing";
                }
            }
            else
            {
                // no specific version
                message = $"{mod.Guid} requires {dep.Guid}, but {dep.Guid} is missing";
            }

            errors.Add(new Exception(message));

            return;
        }
    }

    private static string GetFancy(string version, ModDependency.VersionSpecificity specificity)
    {
        var split = version.Split('.');
        return specificity switch
        {
            ModDependency.VersionSpecificity.OptionalPatch => $"{split[0]}.{split[1]}.x",
            ModDependency.VersionSpecificity.OptionalMinorPatch => $"{split[0]}.x",
            ModDependency.VersionSpecificity.ExactMatch or _ => SemVersion.Parse(version).ToString(),
        };
    }

    internal static void DoInitialize()
    {
        foreach (var mod in loadedMods)
        {
            if(!mod.isNonStandard)
                mod.Instance.OnInitialize();
        }
    }

    internal static void DoRegistriesInit()
    {
        foreach (var mod in loadedMods)
        {
            if(!mod.isNonStandard)
                mod.Instance.OnRegistriesInit();
        }
    }

    internal static void DoLoadContent()
    {
        foreach (var mod in loadedMods)
        {
            if(!mod.isNonStandard)
                mod.Instance.OnLoadContent();
        }
    }

    internal static void DoEndRun()
    {
        foreach (var mod in loadedMods)
        {
            if(!mod.isNonStandard)
                mod.Instance.OnEndRun();
        }
    }
}
