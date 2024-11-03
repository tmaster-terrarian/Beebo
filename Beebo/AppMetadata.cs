namespace Beebo;

public static class AppMetadata
{
    public const string Name = "Beebo";
    public const int Build = 1;

    public static string Version { get; } = "0.1.0";

    public static string CombinedVersionString => $"{Version}-{Build}";

    public static string HumanReadableVersionString => $"v{Version} Build {Build}";
}
