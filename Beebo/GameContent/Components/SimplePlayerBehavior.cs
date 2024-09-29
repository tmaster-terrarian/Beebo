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
        // Entity.X = 100;
        // Entity.Y = 100;
    }

    public override void Update()
    {
        Point input = new(
            inputMapping.Right.IsDown.ToInt32() - inputMapping.Left.IsDown.ToInt32(),
            inputMapping.Down.IsDown.ToInt32() - inputMapping.Up.IsDown.ToInt32()
        );

        if(Main.PlayerControlsDisabled) input = Point.Zero;

        var delta = Time.DeltaTime * 60;

        if(input.X == 1)
        {
            if(velocity.X < 0)
            {
                velocity.X = Util.Approach(velocity.X, 0, 0.08f * delta);
            }

            velocity.X = Util.Approach(velocity.X, MaxSpeed, 0.12f * delta);
        }
        else if(input.X == -1)
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

        if(input.Y == 1)
        {
            if(velocity.Y < 0)
            {
                velocity.Y = Util.Approach(velocity.Y, 0, 0.08f * delta);
            }

            velocity.Y = Util.Approach(velocity.Y, MaxSpeed, 0.12f * delta);
        }
        else if(input.Y == -1)
        {
            if(velocity.Y > 0)
            {
                velocity.Y = Util.Approach(velocity.Y, 0, 0.08f * delta);
            }

            velocity.Y = Util.Approach(velocity.Y, -MaxSpeed, 0.12f * delta);
        }
        else
        {
            velocity.Y = Util.Approach(velocity.Y, 0, 0.16f * delta);
        }

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

public class PlayerInputMapping
{
    public MappedInput Right { get; set; } = new(Keys.D);
    public MappedInput Left { get; set; } = new(Keys.A);
    public MappedInput Down { get; set; } = new(Keys.S);
    public MappedInput Up { get; set; } = new(Keys.W);
    public MappedInput Jump { get; set; } = new(Keys.Space);
    public MappedInput Fire { get; set; } = new(MouseButtons.LeftButton);
}
