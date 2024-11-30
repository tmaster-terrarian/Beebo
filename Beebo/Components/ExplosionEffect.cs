using System;
using Jelly;
using Jelly.Graphics;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.Components;

public class ExplosionEffect : Component
{
    private Texture2D texture;
    private readonly int frameNumber = 17;
    private int frame;
    private int frameCounter;
    private float frameRemainder;
    private int frameDuration = 3;
    private bool done;

    public float Scale { get; set; } = 1;

    public int FrameDuration {
        get => frameDuration;
        set => frameDuration = Math.Abs(value);
    }

    public float Framerate {
        get => 1f / FrameDuration;
        set => FrameDuration = (int)(1f / value);
    }

    public float FramerateInSeconds {
        get => Framerate * 60f;
        set => Framerate = value / 60f;
    }

    public override void OnCreated()
    {
        texture = ContentLoader.LoadTexture("Images/Entities/explosion");
        Entity.Depth = 40;
    }

    public override void Update()
    {
        if(done)
            return;

        if(frameCounter >= Math.Abs(frameDuration))
        {
            frameCounter = 0;
            frame++;

            if(frame == frameNumber - 1)
            {
                done = true;
                Scene.Entities.Remove(Entity);
                return;
            }
        }

        frameRemainder += 1;
        int f = MathUtil.RoundToInt(frameRemainder);
        frameRemainder -= f;

        frameCounter += f;
    }

    public override void Draw()
    {
        if(done)
            return;

        var rect = GraphicsUtil.GetFrameInStrip(texture, frame + (float)frameCounter / frameDuration, frameNumber);
        Vector2 pivot = new(21, 36);

        Renderer.SpriteBatch.Draw(
            texture,
            Entity.Position.ToVector2(),
            rect,
            Color.White,
            0, pivot,
            Scale, SpriteEffects.None,
            0
        );
    }
}
