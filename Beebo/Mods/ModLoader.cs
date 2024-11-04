using Microsoft.Xna.Framework;

using Beebo;
using Beebo.GameContent;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.Loader;
using System;

namespace Beebo.Mods;

public static class ModLoader
{
    internal static readonly List<LoadedMod> loadedMods = [];

    public static string ModsPath => Path.Combine(Main.ProgramPath, "mods");

    internal static void DoBeforeRun()
    {
        Directory.CreateDirectory(ModsPath);
        foreach(var fullPath in Directory.EnumerateDirectories(ModsPath, "*.dll", SearchOption.AllDirectories))
        {
            ReadAssembly(fullPath);
        }

        foreach (var mod in loadedMods)
        {
            mod.Instance.OnBeforeRun();
        }
    }

    private static void ReadAssembly(string path)
    {
        Assembly assembly = Assembly.LoadFile(path);

        foreach(var type in assembly.GetTypes())
        {
            if(type.IsSubclassOf(typeof(Mod)))
            {
                ImportMod(new(assembly, path, type));
                break;
            }
        }
    }

    private static void ImportMod(LoadedMod mod)
    {
        mod.Instance = (Mod)mod.Entry.GetConstructor(Type.EmptyTypes).Invoke(null);
        loadedMods.Add(mod);
    }

    internal static void DoInitialize()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance.OnInitialize();
        }
    }

    internal static void DoRegistriesInit()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance.OnRegistriesInit();
        }
    }

    internal static void DoLoadContent()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance.OnLoadContent();
        }
    }

    internal static void DoEndRun()
    {
        foreach (var mod in loadedMods)
        {
            mod.Instance.OnEndRun();
        }
    }
}
