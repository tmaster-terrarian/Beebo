using Jelly;
using Jelly.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Beebo.GameContent.Components;

public class SimplePlayerBehavior : Actor
{
    private readonly PlayerInputMapping inputMapping = new();

    public float MaxSpeed { get; set; } = 4;

    public override void EntityAwake()
    {
        Entity.X = 100;
        Entity.Y = 100;

        Entity.GetComponent<SpriteComponent>().TexturePath = "Images/Entities/SimplePlayer/idle.png";
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
                velocity.X = Util.Approach(velocity.X, 0, 0.08f * delta);
            }

            velocity.X = Util.Approach(velocity.X, MaxSpeed, 0.12f * delta);
        }
        else if(inputDir == -1)
        {
            if(velocity.X > 0)
            {
                velocity.X = Util.Approach(velocity.X, 0, 0.08f * delta);
            }

            velocity.X = Util.Approach(velocity.X, -MaxSpeed, 0.12f * delta);
        }
        else
        {
            velocity.X = Util.Approach(velocity.X, 0, 0.16f * delta);
        }

        velocity.Y = Util.Approach(velocity.Y, 20, 0.2f * delta);

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
