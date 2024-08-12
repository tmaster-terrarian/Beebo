using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class EntityDef : ContentDef
{
    public IList<Component>? Components { get; set; }

    public Point Position { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Visible { get; set; } = true;

    public int Tag { get; set; }

    public int Depth { get; set; }

    public Entity Build(Scene scene)
    {
        var entity = new Entity()
        {
            Enabled = Enabled,
            Visible = Visible,
            Position = Position,
            Tag = Tag,
            Depth = Depth,
        };

        entity.Components.Add(Components);

        scene.Entities.Add(entity);

        return entity;
    }
}
