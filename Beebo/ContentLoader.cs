using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Graphics;
using System;
using Microsoft.Xna.Framework.Content;

namespace Beebo;

public class ContentLoader : ContentProvider
{
    private static readonly Dictionary<string, Texture2D> loadedTextures = [];
    private static readonly List<string> pathsThatDontWork = [];

    private static ContentManager _content;

    public bool UseContentPipeline { get; set; } = true;

    public override Texture2D? GetTexture(string pathName)
    {
        if(pathsThatDontWork.Contains(pathName)) return null;

        if(loadedTextures.TryGetValue(pathName, out Texture2D value)) return value;

        try
        {
            var texture = UseContentPipeline ? Load<Texture2D>(pathName) : Texture2D.FromFile(Renderer.GraphicsDevice, pathName);
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

    private static readonly List<string> missingAssets = [];

    public ContentLoader(ContentManager content)
    {
        _content = content;
    }

    public static T Load<T>(string assetName)
    {
        if(missingAssets.Contains(assetName)) return default;

        try
        {
            return _content.Load<T>(assetName);
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e.GetType().FullName + $": The content file \"{assetName}\" was not found.");
            missingAssets.Add(assetName);
            return default;
        }
    }
}
