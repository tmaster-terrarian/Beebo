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
            list.Add((EntityDef)entity);
        }

        return list;
    }

    public override string ToString()
    {
        return Serialize(true);
    }

    public string Serialize(bool pretty = false)
    {
        var options = SceneRegistry.SerializerOptions;

        options.WriteIndented = pretty;

        var ret = JsonSerializer.Serialize(this, options);

        return ret;
    }

    public static SceneDef? Deserialize(string json, bool pretty = true)
    {
        return JsonSerializer.Deserialize<SceneDef>(json, SceneRegistry.SerializerOptions);
    }
}
