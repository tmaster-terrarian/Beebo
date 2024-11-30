using System.Collections.Generic;
using System.Text.Json;

using Jelly;
using Jelly.GameContent;
using Jelly.Graphics;
using Jelly.Utilities;

namespace Beebo.GameContent;

public class SceneDef : RegistryEntry
{
    public IList<JsonEntity> Entities { get; set; } = [];

    public JsonCollisions Collisions { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    public bool? CreatePlayer { get; set; }

    public Scene Build()
    {
        var scene = new Scene {
            Name = Name,
            Width = Width ?? Renderer.ScreenSize.X,
            Height = Height ?? (Renderer.ScreenSize.Y + 12),
        };

        if(Collisions is not null)
        {
            if(Collisions.Tiles is not null)
            {
                for(int y = 0; y < Collisions.Tiles.Length; y++)
                {
                    for(int x = 0; x < Collisions.Tiles[0].Length; x++)
                    {
                        scene.CollisionSystem.SetTile(Collisions.Tiles[y][x], new(x, y));
                    }
                }
            }

            scene.CollisionSystem.Visible = Collisions.Visible;

            scene.CollisionSystem.JumpThroughs.Clear();
            scene.CollisionSystem.JumpThroughs.AddRange(Collisions.JumpThroughs ?? []);
            scene.CollisionSystem.JumpThroughSlopes.Clear();
            scene.CollisionSystem.JumpThroughSlopes.AddRange(Collisions.JumpThroughSlopes ?? []);
            scene.CollisionSystem.Slopes.Clear();
            scene.CollisionSystem.Slopes.AddRange(Collisions.Slopes ?? []);
        }

        foreach(var e in Entities ?? [])
        {
            e.Build(scene);
        }

        return scene;
    }

    public static explicit operator SceneDef(Scene scene) => new SceneDef
    {
        Entities = [.. GetEntityDefs(scene.Entities)],
        Name = scene.Name,
    };

    private static IList<JsonEntity> GetEntityDefs(EntityList entities)
    {
        IList<JsonEntity> list = [];

        var _entities = entities.ToArray();

        foreach(var entity in _entities)
        {
            list.Add((JsonEntity)entity);
        }

        return list;
    }

    public override string ToString()
    {
        return Serialize(true);
    }

    public string Serialize(bool pretty = false)
    {
        var options = RegistryManager.SerializerOptions;
        options.WriteIndented = pretty;

        return JsonSerializer.Serialize(this, options);
    }

    public static SceneDef? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SceneDef>(json, RegistryManager.SerializerOptions);
    }
}

public static class SceneExtensions
{
    public static string Serialize(this Scene scene, bool pretty = false)
    {
        var options = RegistryManager.SerializerOptions;
        options.WriteIndented = pretty;

        return JsonSerializer.Serialize((SceneDef)scene, options);
    }

    public static string Serialize(this Component component, bool pretty = false)
    {
        var options = RegistryManager.SerializerOptions;
        options.WriteIndented = pretty;

        return JsonSerializer.Serialize(component, options);
    }
}
