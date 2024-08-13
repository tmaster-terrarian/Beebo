using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.GameContent;
using Jelly.Tamperment;
using System.Text.Json.Serialization;

namespace Beebo.GameContent;

public class EntityDef : ContentDef
{
    public IList<Component>? Components { get; set; }

    public Point Position { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Visible { get; set; } = true;

    public int Tag { get; set; }

    public int Depth { get; set; }

    public int? NetID { get; set; } = null;

    [JsonInclude] internal long? EntityID { get; set; } = null;

    public Entity Build(Scene scene)
    {
        var entity = new Entity(Position, NetID ?? -1)
        {
            Enabled = Enabled,
            Visible = Visible,
            Tag = Tag,
            Depth = Depth,
        };

        if(EntityID is not null)
            entity.SetEntityID(EntityID);

        if(Components is not null)
            entity.Components.Add(Components);

        scene.Entities.Add(entity);

        return entity;
    }

    public static explicit operator EntityDef(Entity entity) => new EntityDef
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
}
