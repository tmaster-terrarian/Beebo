using System.Collections.Generic;
using Beebo.GameContent;
using Jelly;
using Jelly.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo;

public static class Extensions
{
    public static string ToStringUTF8(this IEnumerable<byte> value)
    {
        return System.Text.Encoding.UTF8.GetString([..value]);
    }

    public static string ToStringASCII(this IEnumerable<byte> value)
    {
        return System.Text.Encoding.ASCII.GetString([..value]);
    }

    public static Vector2 SafeNormalize(this Vector2 vector)
    {
        Vector2 vector2 = Vector2.Normalize(vector);
        if(float.IsNaN(vector2.X) || float.IsInfinity(vector2.X) || float.IsSubnormal(vector2.X)) vector2.X = 0;
        if(float.IsNaN(vector2.Y) || float.IsInfinity(vector2.Y) || float.IsSubnormal(vector2.Y)) vector2.Y = 0;

        return vector2;
    }

    public static int GetTile(this CollisionSystem collisionSystem, int x, int y)
    {
        return collisionSystem.GetTile(new(x, y));
    }

    public static void Draw(this CollisionSystem collisionSystem)
    {
        for(int x = 0; x < collisionSystem.Width; x++)
        {
            for(int y = 0; y < collisionSystem.Height; y++)
            {
                int tile = collisionSystem.GetTile(x, y);
                if(tile == 0)
                    continue;

                var tex = ContentLoader.Load<Texture2D>("Images/Level/wall");
                if(tex == null)
                    continue;

                Renderer.SpriteBatch.Draw(
                    tex,
                    new Rectangle(
                        x * CollisionSystem.TileSize,
                        y * CollisionSystem.TileSize,
                        CollisionSystem.TileSize,
                        CollisionSystem.TileSize
                    ),
                    Color.White
                );
            }
        }
    }
}
