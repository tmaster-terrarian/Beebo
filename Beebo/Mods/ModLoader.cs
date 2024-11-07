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
using System.Text;

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

    public static JsonSerializerOptions SerializerOptions { get; } = new() {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = {
            new JsonStringEnumConverter<ModDependency.DependencyKind>(),
        },
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
    {
        return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
    }

    internal static void DoBeforeRun()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

        Main.Logger.LogInfo($"Crawling for mod assemblies...");

        Directory.CreateDirectory(ModsPathPath);
        foreach(var modFolder in Directory.EnumerateDirectories(ModsPathPath))
        {

            foreach(var fullPath in Directory.EnumerateFiles(modFolder, "*.dll"))
            {
                Main.Logger.LogInfo($"  Found assembly {Path.GetFileName(fullPath)} ({fullPath})");

                ReadAssembly(fullPath, modFolder, true);

                if(!loadedPaths.Contains(modFolder))
                    loadedPaths.Add(modFolder);
            }

            var libPath = Path.Combine(modFolder, "lib");
            if(Directory.Exists(libPath))
            {
                foreach(var fullPath in Directory.EnumerateFiles(libPath, "*.dll", SearchOption.AllDirectories))
                {
                    Main.Logger.LogInfo($"  Found assembly {Path.GetFileName(fullPath)} ({fullPath})");

                    ReadAssembly(fullPath, modFolder, false);

                    if(!loadedPaths.Contains(modFolder))
                        loadedPaths.Add(modFolder);
                }
            }
        }

        {
            StringBuilder modListLog = new("Mods to load:");
            for(int i = 0; i < loadedMods.Count; i++)
            {
                var mod = loadedMods[i];

                modListLog.Append("\n  - ");
                modListLog.Append(loadedMods[i].DisplayName);
                if(mod.DisplayName != mod.Guid)
                {
                    modListLog.Append(" (");
                    modListLog.Append(loadedMods[i].Guid);
                    modListLog.Append(')');
                }
            }
            Main.Logger.LogInfo(modListLog);
        }

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
        }

        loadedMods.Sort((a, b) => a.loadIndex - b.loadIndex);

        // post import

        {
            StringBuilder modListLog = new StringBuilder("Successfully loaded ").Append(loadedMods.Count).Append(" mods:");
            for(int i = 0; i < loadedMods.Count; i++)
            {
                var mod = loadedMods[i];

                modListLog.Append("\n  - ");
                modListLog.Append(mod.loadIndex);
                modListLog.Append(": ");
                modListLog.Append(loadedMods[i].DisplayName);
                if(mod.DisplayName != mod.Guid)
                {
                    modListLog.Append(" (");
                    modListLog.Append(loadedMods[i].Guid);
                    modListLog.Append(')');
                }
            }
            Main.Logger.LogInfo(modListLog);
        }

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

            ModJson modJson = JsonSerializer.Deserialize<ModJson>(File.ReadAllText(jsonPath), SerializerOptions);

            nonStandardMods.Add(modJson.Guid, modJson);

            LoadedMod mod = new()
            {
                isNonStandard = true,
                Assembly = assembly,
                Guid = modJson.Guid,
                DisplayName = modJson.Name ?? modJson.Guid,
                Version = SemVersion.Parse(modJson.Version, SemVersionStyles.OptionalPatch),
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
            {
                Main.Logger.LogError(new AggregateException($"Failed to resolve dependencies for {mod.Guid} due to errors", errors));
                mod.failedLoad = true;
                resolvedGuids.Add(mod.Guid);
                return;
            }
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

        var depMod = loadedMods.FirstOrDefault(m => m.Guid == dep.Guid);
        if (depMod != null)
        {
            string dependencyKindText = "requires a";
            if (dep.Kind == ModDependency.DependencyKind.Incompatible)
                dependencyKindText = "is incompatible with any";

            try
            {
                var versionRangeOptions = SemVersionRangeOptions.OptionalPatch | SemVersionRangeOptions.IncludeAllPrerelease;

                bool versionMatches = SemVersionRange.Parse(dep.VersionRange, versionRangeOptions)
                    .Contains(loadedMods.First(m => m.Guid == dep.Guid).Version);

                // min and max
                if ((dep.Kind == ModDependency.DependencyKind.Incompatible ^ versionMatches)
                    || (dep.Kind == ModDependency.DependencyKind.Optional && versionMatches))
                {
                    ResolveDependencies(depMod);
                    depIsAvailable = true;
                }
                else
                    throw new Exception($"{mod.Guid} {dependencyKindText} version of {dep.Guid} that falls within the range {dep.VersionRange}, but {depMod.Version} was found");
            }
            catch(Exception e)
            {
                errors.Add(e);
            }

            if (dep.Kind != ModDependency.DependencyKind.Incompatible)
            {
                mod.availableDependencies.Add(dep.Guid, depIsAvailable);
            }

            switch(dep.Kind)
            {
                case ModDependency.DependencyKind.Required:
                    if(depIsAvailable)
                        Main.Logger.LogInfo($"  {dep.Guid} {dep.VersionRange}: Required dependency met");
                    else
                        Main.Logger.LogError($" {dep.Guid} {dep.VersionRange}: Required dependency was not met");
                    break;
                case ModDependency.DependencyKind.Optional:
                    if(depIsAvailable)
                        Main.Logger.LogInfo($"  {dep.Guid} {dep.VersionRange}: Optional dependency met");
                    else
                        Main.Logger.LogWarning($"  {dep.Guid} {dep.VersionRange}: Optional dependency was not met");
                    break;
            }

            return;
        }

        if (dep.Kind != ModDependency.DependencyKind.Incompatible)
        {
            mod.availableDependencies.Add(dep.Guid, false);
        }

        if (dep.Kind == ModDependency.DependencyKind.Required)
        {
            throw new Exception($"{mod.Guid} requires a version of {dep.Guid} that falls within the range {dep.VersionRange}, but {dep.Guid} is missing");
        }

        switch(dep.Kind)
        {
            case ModDependency.DependencyKind.Required:
                if(depIsAvailable)
                    Main.Logger.LogInfo($"  {dep.Guid} {dep.VersionRange}: Required dependency met");
                else
                    Main.Logger.LogWarning($"  {dep.Guid} {dep.VersionRange}: Required dependency was not met");
                break;
            case ModDependency.DependencyKind.Optional:
                if(depIsAvailable)
                    Main.Logger.LogInfo($"  {dep.Guid} {dep.VersionRange}: Optional dependency met");
                else
                    Main.Logger.LogWarning($"  {dep.Guid} {dep.VersionRange}: Optional dependency was not met");
                break;
        }
    }

    internal static void DoInitialize()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance?.OnInitialize();
        }
    }

    internal static void DoRegistriesInit()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance?.OnRegistriesInit();
        }
    }

    internal static void DoLoadContent()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance?.OnLoadContent();
        }
    }

    internal static void DoEndRun()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance?.OnEndRun();
        }
    }
}
