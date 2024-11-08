using System;
using System.IO;
using System.Reflection;

namespace Beebo;

public static class FileLocations
{
    private static readonly Assembly assembly = Assembly.GetEntryAssembly();

    public static string ProgramPath => Path.GetDirectoryName(assembly.Location);

    public static string ContentPath => Path.Combine(ProgramPath, "Content");

    public static string ModsPath => Path.Combine(ProgramPath, "mods");

    public static string SaveDataPath
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), AppMetadata.Name);
}
