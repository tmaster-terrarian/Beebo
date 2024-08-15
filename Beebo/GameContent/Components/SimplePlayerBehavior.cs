using System;
using System.Text.Json.Serialization;
using Beebo.Net;
using Jelly;
using Jelly.Components;
using Jelly.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.GameContent.Components;

public class SimplePlayerBehavior : Component
{
    [JsonInclude] private float yRemainder;
    [JsonInclude] private float xRemainder;

    private readonly PlayerInputMapping inputMapping = new();

    private const string ArrowTexturePath = "Images/Entities/SimplePlayer/arrow";

    [JsonInclude] private Vector2 velocity = Vector2.Zero;

    public float MaxSpeed { get; set; } = 4;

    [JsonIgnore]
    public Vector2 Velocity
    {
        get => velocity;
        set {
            if(velocity != value)
            {
                velocity = value;
                MarkForSync();
            }
        }
    }

    public override void Added(Entity entity)
    {
        base.Added(entity);
    }

    public override void Update()
    {
        Entity.MarkForSync();

        if(!CanUpdateLocally) return;

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

    public override void DrawUI()
    {
        base.DrawUI();

        // dont want to draw arrows pointing to other players if there are no other players
        if(!P2PManager.InLobby)
            return;

        foreach(var e in Scene.Entities)
        {
            if(e.Components.Get<SimplePlayerBehavior>() is null) continue;
            if(!e.Enabled) continue;

            if(e.EntityID == Entity.EntityID) continue;

            Color color = Color.LightGray;
            if(e.Components.Get<SpriteComponent>() is SpriteComponent spriteComponent)
            {
                color = spriteComponent.Color;
            }

            Renderer.SpriteBatch.Draw(
                Main.LoadContent<Texture2D>(ArrowTexturePath),
                Entity.Position.ToVector2() - Vector2.UnitY * 14,
                null,
                color,
                (e.Position.ToVector2() - Entity.Position.ToVector2()).ToRotation(),
                new(16, 16),
                1,
                SpriteEffects.None,
                0
            );
        }
    }
}
