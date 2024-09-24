using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Graphics;

namespace Beebo;

public class BeeboContentProvider : ContentProvider
{
    private static readonly Dictionary<string, Texture2D> loadedTextures = [];
    private static readonly List<string> pathsThatDontWork = [];

    public bool UseContentPipeline { get; set; } = true;

    public override Texture2D? GetTexture(string pathName)
    {
        if(Main.Instance.Server) return null;

        if(pathsThatDontWork.Contains(pathName)) return null;

        if(loadedTextures.TryGetValue(pathName, out Texture2D value)) return value;

        try
        {
            var texture = UseContentPipeline ? Main.LoadContent<Texture2D>(pathName) : Texture2D.FromFile(Renderer.GraphicsDevice, pathName);
            loadedTextures.Add(pathName, texture);
            return texture;
        }
        catch(System.Exception e)
        {
            pathsThatDontWork.Add(pathName);
            Main.Logger.LogError(e);
            return null;
        }
    }
}
