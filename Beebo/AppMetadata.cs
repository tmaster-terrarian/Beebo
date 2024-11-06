using Semver;

namespace Beebo;

public static class AppMetadata
{
    public const string Name = "Beebo";

    public static SemVersion Version { get; } = SemVersion.Parse("0.1.0");

    public static string VersionString => $"{Version}";
}
