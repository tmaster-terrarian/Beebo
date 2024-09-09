using System;
using System.Text.Json.Serialization;
using Jelly;
using Microsoft.Xna.Framework;

namespace Beebo.GameContent.Components;

public class SimplePlayerBehavior : Component
{
    [JsonInclude] private float yRemainder;
    [JsonInclude] private float xRemainder;

    private readonly PlayerInputMapping inputMapping = new();

    [JsonInclude] private Vector2 velocity = Vector2.Zero;

    public float MaxSpeed { get; set; } = 4;

    [JsonIgnore]
    public Vector2 Velocity
    {
        get => velocity;
        set => velocity = value;
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);
    }

    public override void Update()
    {
        Point input = new(
            inputMapping.Right.IsDown.ToInt32() - inputMapping.Left.IsDown.ToInt32(),
            inputMapping.Down.IsDown.ToInt32() - inputMapping.Up.IsDown.ToInt32()
        );

        var velocity = Velocity;

        var delta = Providers.DeltaTime * 60;

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

        Velocity = velocity;

        MoveX(velocity.X, null);
        MoveY(velocity.Y, null);
    }

    public virtual void MoveX(float amount, Action? onCollide)
    {
        xRemainder += amount;
        int move = Util.RoundToInt(xRemainder);
        xRemainder -= move;

        if(move != 0)
        {
            int sign = Math.Sign(move);
            while(move != 0)
            {
                Entity.X += sign;
                move -= sign;
            }
        }
    }

    public virtual void MoveY(float amount, Action? onCollide)
    {
        yRemainder += amount;
        int move = Util.RoundToInt(yRemainder);
        yRemainder -= move;

        if(move != 0)
        {
            int sign = Math.Sign(move);
            while(move != 0)
            {
                Entity.Y += sign;
                move -= sign;
            }
        }
    }
}
