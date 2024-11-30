using System.IO;
using System.Text.Json;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.Components;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class SceneRegistry : Registry<SceneDef>
{
    public override void Init()
    {
        string path = Path.Combine(FileLocations.DataPath, "levels");
        foreach(var file in Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            Add(fileName);
        }

        // foreach(var file in Directory.EnumerateFiles(Path.Combine(path, "Tracks"), "*.ldtkl", SearchOption.TopDirectoryOnly))
        // {
        //     AddLDtk(file, Path.GetFileNameWithoutExtension(file));
        // }

        Main.Logger.LogInfo($"Registered Scenes:\n  - {string.Join("\n  - ", this.Keys)}");
    }

    private SceneDef Add(string name)
    {
        var def = LoadFromFile(name);
        if(def is not null)
        {
            def.Name = name;
            Register(def);
        }
        return def;
    }

    private bool AddLDtk(string path, string name)
    {
        SceneDef scene = new() {
            Name = name,
        };

        var level = LDtk.LDtkLevel.FromFile(path);

        var entityLayer = level.LayerInstances[0];
        var tileLayer = level.LayerInstances[1];

        Point gridSize = new(tileLayer.CellWidth, tileLayer.CellHeight);

        scene.Width = gridSize.X * CollisionSystem.TileSize;
        scene.Height = gridSize.Y * CollisionSystem.TileSize;

        scene.Collisions.Tiles = new int[gridSize.Y][];

        int c = 0;
        var array = tileLayer.IntGridCsv;
        for(int y = 0; y < gridSize.Y; y++)
        {
            scene.Collisions.Tiles[y] = new int[gridSize.X];
            for(int x = 0; x < gridSize.X; x++)
            {
                if(array[c] == 1)
                    scene.Collisions.Tiles[y][x] = 1;
                c++;
            }
        }

        for(int i = 0; i < entityLayer.EntityInstances.Length; i++)
        {
            var entity = entityLayer.EntityInstances[i];

            if(entity.Identifier == "Ledge")
            {
                scene.Entities.Add(new() {
                    Position = entity.PixelCoord,
                    Enabled = true,
                    Visible = true,
                    Components = [
                        new Solid {
                            DefaultBehavior = false,
                            Width = entity.Width,
                            Height = entity.Height,
                        }
                    ]
                });
            }

            if(entity.Identifier == "JumpThrough")
                scene.Collisions.JumpThroughs.Add(new(entity.PixelCoord, new(entity.Width, MathHelper.Max(entity.Height - 1, 1))));

            if(entity.Identifier.EndsWith("Slope"))
            {
                Point point1 = entity.PixelCoord;
                Point point2 = new Point(((JsonElement)entity.FieldInstances[0].Value)[0].GetProperty("cx").GetInt32() * CollisionSystem.TileSize, ((JsonElement)entity.FieldInstances[0].Value)[0].GetProperty("cy").GetInt32() * CollisionSystem.TileSize);

                if(entity.Identifier == "JumpThrough_Slope")
                    scene.Collisions.JumpThroughSlopes.Add(new(point1, point2, 2));
                if(entity.Identifier == "Slope")
                    scene.Collisions.Slopes.Add(new(point1, point2, 2));
            }
        }

        return Register(scene);
    }

    public static SceneDef? LoadFromFile(string path)
    {
        return SceneDef.Deserialize(File.ReadAllText(Path.Combine(FileLocations.DataPath, "levels", $"{path}.json")));
    }
}
