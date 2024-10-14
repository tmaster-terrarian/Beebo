using Jelly;
using Jelly.Components;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Beebo.GameContent.Components;

public class SimplePlayerBehavior : Actor
{
    private readonly PlayerInputMapping inputMapping = new();

    public float MaxSpeed { get; set; } = 4;

    private SpriteComponent Sprite => Entity.GetComponent<SpriteComponent>();

    public override void EntityAwake()
    {
        Entity.X = 100;
        Entity.Y = 100;

        Width = 8;
        Height = 14;

        Sprite.TexturePath = "Images/Entities/SimplePlayer/idle";

        var tex = Main.LoadContent<Texture2D>(Sprite.TexturePath);
        Sprite.Pivot = new(tex.Width / 2, tex.Height);

        bboxOffset = new Point(-Width / 2, -Height);
    }

    public override void Update()
    {
        int inputDir = inputMapping.Right.IsDown.ToInt32() - inputMapping.Left.IsDown.ToInt32();

        bool wasOnGround = OnGround;
        bool onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(Main.PlayerControlsDisabled) inputDir = 0;

        var delta = Time.DeltaTime * 60;

        if(inputDir == 1)
        {
            if(velocity.X < 0)
            {
                velocity.X = MathUtil.Approach(velocity.X, 0, 0.08f * delta);
            }

            velocity.X = MathUtil.Approach(velocity.X, MaxSpeed, 0.12f * delta);
        }
        else if(inputDir == -1)
        {
            if(velocity.X > 0)
            {
                velocity.X = MathUtil.Approach(velocity.X, 0, 0.08f * delta);
            }

            velocity.X = MathUtil.Approach(velocity.X, -MaxSpeed, 0.12f * delta);
        }
        else
        {
            velocity.X = MathUtil.Approach(velocity.X, 0, 0.16f * delta);
        }

        velocity.Y = MathUtil.Approach(velocity.Y, 20, 0.2f * delta);

        MoveX(velocity.X, () => {
            velocity.X = 0;
        });
        MoveY(velocity.Y, () => {
            velocity.Y = 0;
        });

        if(Left < 0)
        {
            Entity.X = 0;
            velocity.X = 0;
        }

        if(Right > Scene.Width)
        {
            Entity.X = Scene.Width - Width;
            velocity.X = 0;
        }

        if(Top < 0)
        {
            Entity.Y = 0;
            velocity.Y = 0;
        }

        if(Bottom > Scene.Height)
        {
            Entity.Y = Scene.Height - Height;
            velocity.Y = 0;
        }
    }
}
