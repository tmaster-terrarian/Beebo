namespace Beebo;

public static class AppMetadata
{
    public const string Name = "Beebo";
    public const string Version = "0.1.0";
    public const int Build = 1;

    public static string CombinedVersionString => $"{Name}-{Version}-{Build}";

    public static string HumanReadableVersionString => $"v{Version} Build {Build}";
}
