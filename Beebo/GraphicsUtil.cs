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

    public static Rectangle GetFrameInStrip(Texture2D texture, float currentFrame, int totalFramesX, int totalFramesY)
    {
        int width = texture.Width / totalFramesX;
        int height = texture.Height / totalFramesY;
        return new(
            (int)currentFrame % totalFramesX * width,
            (int)currentFrame / totalFramesX * height,
            width,
            height
        );
    }
}
