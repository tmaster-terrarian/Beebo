using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Beebo.GameContent;

using Jelly;
using Jelly.Graphics;
using System.Linq;
using System;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace Beebo.Graphics;

public class TileRenderer(Scene scene) : ExtendedRenderer
{
    private readonly int[,] _tileInfo = new int[scene.CollisionSystem.Width,scene.CollisionSystem.Height];
    private readonly Dictionary<int, Texture2D> _textures = [];

    public Scene Scene { get; } = scene;

    public int Width { get; } = scene.CollisionSystem.Width;
    public int Height { get; } = scene.CollisionSystem.Height;

    private RenderTarget2D renderTarget;

    private readonly Dictionary<char, int> shapeKey = new() {
        {'-', -1},
        {'0', 0},
        {'1', 1}
    };

    public override void SceneBegin(Scene _)
    {
        _textures[-1] = ContentLoader.Load<Texture2D>($"Images/Level/tilesets/template");

        int get(TilesetDef def, int x, int y)
        {
            if(Scene.CollisionSystem.InWorld(x, y))
            {
                int tile = Scene.CollisionSystem.GetTile(x, y);

                if((def.Ignores?.Contains(TilesetRegistry.GetNameFromID(tile))) ?? false)
                    return 0;
                else
                    return Math.Sign(tile);
            }
            else
                return 1;
        }

        for(int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                int tile = Scene.CollisionSystem.GetTile(x, y);
                if(tile == 0) continue;

                var def = TilesetRegistry.GetDefStatic(tile) ?? TilesetRegistry.GetDefStatic(0);
                _textures.TryAdd(tile & 0x00FFFF, ContentLoader.Load<Texture2D>($"Images/Level/tilesets/{def.Name}"));

                int u = get(def, x, y - 1);
                int r = get(def, x + 1, y) << 1;
                int d = get(def, x, y + 1) << 2;
                int l = get(def, x - 1, y) << 3;

                int ul = get(def, x - 1, y - 1) << 4;
                int ur = get(def, x + 1, y - 1) << 5;
                int dl = get(def, x - 1, y + 1) << 6;
                int dr = get(def, x + 1, y + 1) << 7;

                int id = tile << 8;

                _tileInfo[x, y] = id | dl | dr | ur | ul | l | d | r | u;
            }
        }

        renderTarget = new(Renderer.GraphicsDevice, Scene.Width, Scene.Height);

        Renderer.GraphicsDevice.SetRenderTarget(renderTarget);
        Renderer.GraphicsDevice.Clear(Color.Transparent);

        Renderer.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        for(int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                DrawTile(x, y);
            }
        }

        Renderer.SpriteBatch.End();
        Renderer.GraphicsDevice.SetRenderTarget(null);

        Main.Logger.LogInfo(JsonSerializer.Serialize(TilesetRegistry.GetDefStatic(0), RegistryManager.SerializerOptions));
        Main.Logger.LogInfo(JsonSerializer.Serialize(TilesetRegistry.GetDefStatic(1), RegistryManager.SerializerOptions));
    }

    public override void BeginDraw(GameTime gameTime)
    {
        Renderer.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
    }

    private void DrawTile(int x, int y)
    {
        int tile = _tileInfo[x, y];
        int id = (tile >> 8) & 0x00FFFF;
        if(id == 0) return;

        int shapeMask = tile & 0x0000FF;

        var def = TilesetRegistry.GetDefStatic(id);

        Rectangle srcRect = new(0, 0, 16, 16);
        Rectangle dest = new(x * 16, y * 16, 16, 16);

        List<TilesetRule> collection;

        if(def.CopyFrom is not null)
        {
            collection = CombineRules(def);
        }
        else
            collection = [..(def.Rules ?? [])];

        bool match = false;

        bool check(TilesetRule rule)
        {
            for (int _y = 0; _y < 3; _y++)
            {
                for (int _x = 0; _x < 3; _x++)
                {
                    int tileMatcher = shapeKey[rule.Pattern[_y][_x]];
                    if (tileMatcher == -1)
                        continue;

                    var bit = ((_y * 3) + _x) switch
                    {
                        0 => 16, 1 => 1,  2 => 32,
                        3 => 8,  4 => 0,  5 => 2,
                        6 => 64, 7 => 4,  8 => 128,
                        _ => -1
                    };

                    if (bit <= 0)
                        continue;

                    if((tileMatcher == 0 && (bit & shapeMask) != 0) || (tileMatcher == 1 && (bit & shapeMask) == 0))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        foreach (var rule in collection)
        {
            match = check(rule);
            if(match)
            {
                Main.Logger.LogInfo($"match found at {x}, {y}, id:{id}, value:{tile}, mask:{shapeMask}, shape:\n{string.Join('\n',rule.Pattern)}");
                srcRect.X = rule.U * 16;
                srcRect.Y = rule.V * 16;
                break;
            }
        }

        if(!match) // :(
        {
            Renderer.SpriteBatch.Draw(_textures[-1], dest, new Rectangle(16 * 7, 16 * 3, 16, 16), Color.White);
            return;
        }

        Renderer.SpriteBatch.Draw(_textures[id], dest, srcRect, Color.White);
    }

    private static List<TilesetRule> CombineRules(TilesetDef def)
    {
        HashSet<TilesetRule> list = new(new RuleComparer());

        foreach(var rule in def.Rules ?? [])
        {
            list.Add(rule);
        }

        if(def.CopyFrom is not null && TilesetRegistry.GetDefStatic(def.CopyFrom) is TilesetDef basis && basis.Name != def.Name)
        {
            foreach(var rule in basis.Rules)
            {
                list.Add(rule);
            }
        }

        return [..list];
    }

    private class RuleComparer : IEqualityComparer<TilesetRule>
    {
        public bool Equals(TilesetRule x, TilesetRule y)
        {
            return x.Pattern == y.Pattern;
        }

        public int GetHashCode([DisallowNull] TilesetRule obj)
        {
            return obj.Pattern.GetHashCode();
        }
    }
}
