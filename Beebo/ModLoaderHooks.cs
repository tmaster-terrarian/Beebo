using Microsoft.Xna.Framework;

using Beebo;
using Beebo.GameContent;

namespace Beebo;

/// <summary>
/// Modders can use MonoMod to inject their mod loader code here
/// </summary>
public static class ModLoaderHooks
{
    /// <summary>
    /// Called before <see cref="Game.Run()"/>, useful for loading assemblies
    /// </summary>
    public static void BeforeRun()
    {
        
    }

    /// <summary>
    /// Called during <see cref="Main.Initialize()"/>, useful for mod entrypoint
    /// </summary>
    public static void Initialize()
    {
        
    }

    /// <summary>
    /// Called during <see cref="RegistryManager.RegistriesInit()"/>, useful for mod content
    /// </summary>
    public static void RegistriesInit()
    {
        
    }

    /// <summary>
    /// Called during <see cref="Main.LoadContent()"/>, useful for loading mod resources
    /// </summary>
    public static void LoadContent()
    {
        
    }

    /// <summary>
    /// Called after <see cref="Game.Run()"/>, useful for cleaning up before exit
    /// </summary>
    public static void EndRun()
    {
        
    }
}
