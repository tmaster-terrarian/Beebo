using System.Collections.Generic;
using System.Text.Json;
using Jelly;
using Jelly.GameContent;
using Jelly.Utilities;

namespace Beebo.GameContent;

public class SceneDef : ContentDef
{
    public IList<EntityDef> Entities { get; set; } = [];

    public Scene Build()
    {
        var scene = new Scene(Name.GetHashCode())
        {
            Name = Name,
        };

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

    private static IList<EntityDef> GetEntityDefs(EntityList entities)
    {
        IList<EntityDef> list = [];

        var _entities = entities.ToArray();

        foreach(var entity in _entities)
        {
            list.Add(new EntityDef {
                Enabled = entity.Enabled,
                Position = entity.Position,
                Visible = entity.Visible,
                Components = [.. entity.Components],
                Depth = entity.Depth,
                Tag = entity.Tag
            });
        }

        return list;
    }

    public override string ToString()
    {
        return Serialize();
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, SceneRegistry.SerializerOptions);
    }

    public static SceneDef? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SceneDef>(json, SceneRegistry.SerializerOptions);
    }
}
