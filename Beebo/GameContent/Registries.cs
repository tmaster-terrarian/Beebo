namespace Beebo.GameContent;

public static class Registries
{
    public static SceneRegistry SceneRegistry { get; } = new();

    public static void Init()
    {
        SceneRegistry.Init();
    }
}
