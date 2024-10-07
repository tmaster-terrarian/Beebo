using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo;

public static class GraphicsUtil
{
    public static Rectangle GetFrameInStrip(Texture2D texture, float currentFrame, int totalFrames)
    {
        int width = texture.Width / totalFrames;
        return new((int)currentFrame * width, 0, width, texture.Height);
    }
}
