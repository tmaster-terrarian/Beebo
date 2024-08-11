using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class EntityDef : ContentDef
{
    public IList<Component>? Components { get; set; }

    public Point Position { get; set; }

    public bool Enabled { get; set; }

    public bool Visible { get; set; }

    public Entity Build()
    {
        var entity = new Entity(Position)
        {
            Components = {
                Components
            }
        };

        return entity;
    }
}
