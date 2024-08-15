using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;
using Jelly.Unsafe;
using System.Text.Json;

namespace Beebo.GameContent;

[JsonAutoPolymorphic]
public class JsonEntity
{
    public string Name { get; set; } = null;

    public IList<Component>? Components { get; set; }

    public Point Position { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Visible { get; set; } = true;

    public int? Tag { get; set; } = null;

    public int? Depth { get; set; } = null;

    public int? NetID { get; set; } = null;

    [JsonInclude] internal long? EntityID { get; set; } = null;

    public Entity Create(Scene scene, bool skipSync = true)
    {
        var entity = new Entity(Position, NetID ?? -1)
        {
            Enabled = Enabled,
            Visible = Visible,
            Tag = Tag ?? 0,
            Depth = Depth ?? 0,
        };

        if(EntityID is not null)
            entity.SetEntityID(EntityID);

        if(Components is not null)
            entity.Components.Add(Components);

        if(skipSync)
            entity.IgnoreNextSync();

        scene.Entities.Add(entity);

        return entity;
    }

    public static explicit operator JsonEntity(Entity entity) => new JsonEntity
    {
        Enabled = entity.Enabled,
        Position = entity.Position,
        Visible = entity.Visible,
        Components = [.. entity.Components],
        Depth = entity.Depth,
        Tag = entity.Tag,
        NetID = entity.NetID,
        EntityID = entity.EntityID,
    };

    public string Serialize(bool pretty = false)
    {
        var options = RegistryManager.SerializerOptions;
        options.WriteIndented = pretty;

        return JsonSerializer.Serialize(this, options);
    }

    public static JsonEntity? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<JsonEntity>(json, RegistryManager.SerializerOptions);
    }
}

public static class EntityExtensions
{
    public static string Serialize(this Entity scene, bool pretty = false)
    {
        var options = RegistryManager.SerializerOptions;
        options.WriteIndented = pretty;

        return JsonSerializer.Serialize((JsonEntity)scene, options);
    }

    public static bool ContainsType<T>(this IList<Component> components) where T : Component
    {
        foreach(var c in components)
            if(c is T)
                return true;
        return false;
    }
}