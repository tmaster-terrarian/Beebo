using Microsoft.Xna.Framework;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class SpriteDef : RegistryEntry
{
    public string TexturePath { get; set; }
    public Point Pivot { get; set; }
    public Rectangle Hitbox { get; set; }

    public Rectangle GetHitboxOffset(Point scale)
    {
        return Hitbox.Shift(new Point(-Pivot.X * scale.X, -Pivot.Y * scale.Y));
    }
}
