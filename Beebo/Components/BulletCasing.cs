using System;
using Jelly;
using Jelly.Components;
using Jelly.Graphics;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.Components;

public class BulletCasing : Actor
{
    float alpha = 12;

    float finalAngle = 0;

    float rotationSpeed = 0;
    float rotationSpeedMax = MathHelper.ToRadians(30);
    float rotationAccel = MathHelper.ToRadians(1.5f);

    float reboundStrength = 0.6f;
    int bounces = 0;
    int bouncesMax = 0;

    float gravity = 0.2f;

    bool done = false;

    Texture2D texture;

    public float Angle { get; set; } = 0;

    public int ImageFacing { get; set; } = 1;

    public override void OnCreated()
    {
        bouncesMax = Random.Shared.Next(3);
        texture = ContentLoader.LoadTexture("Images/Entities/casing");

        Width = 2;
        Height = 2;
    }

    public override void Update()
    {
        if(Center.Y > Scene.Height)
        {
            Scene.Entities.Remove(Entity);
            return;
        }

        if(done)
        {
            if(!CheckColliding(BottomEdge.Shift(0, 1)))
            {
                done = false;
                alpha = Random.Shared.Next(5, 9);
                Facing = Random.Shared.NextSingle() >= 0.5f ? -1 : 1;
                bouncesMax = Random.Shared.Next(3); 
            }
            else
            {
                velocity.Y = 0;
                velocity.X = MathUtil.Approach(velocity.X, 0, 0.1f);

                var c = Scene.CollisionSystem.SolidPlace(BottomEdge.Shift(0, 1));
                if(c != null)
                {
                    velocity = c.velocity;
                }

                Angle = MathUtil.Approach(Angle, finalAngle, MathHelper.ToRadians(20f));

                alpha = MathUtil.Approach(alpha, 0, 0.2f);

                if(alpha == 0)
                {
                    Scene.Entities.Remove(Entity);
                }

                return;
            }
        }

        velocity.Y = MathUtil.Approach(velocity.Y, 20, gravity);
        velocity.X = MathUtil.Approach(velocity.X, 0, 0.01f);

        rotationSpeed = MathUtil.Approach(rotationSpeed, rotationSpeedMax, rotationAccel);

        MoveX(velocity.X, () => {
            rotationSpeed = 0;
            Facing = -Facing;
            velocity.X = -velocity.X * 0.9f;
        });

        MoveY(velocity.Y, () => {
            rotationSpeed = 0;
            if(bounces < bouncesMax)
            {
                bounces++;
                velocity.Y = -velocity.Y * reboundStrength;
                velocity.X *= 0.75f;
            }
            else
            {
                done = true;
                finalAngle = MathF.Round(Angle / MathF.PI) * MathF.PI;
            }
        });

        Angle -= rotationSpeed * Facing;
    }

    public override void Draw()
    {
        Renderer.SpriteBatch.Draw(
            texture,
            Center.ToVector2(),
            null,
            Color.White * alpha,
            MathHelper.ToRadians(MathF.Round(MathHelper.ToDegrees(Angle) / 8) * 8),
            new Vector2(2, 1),
            new Vector2(1, ImageFacing),
            SpriteEffects.None, 0
        );
    }

    bool removed;

    public override void Removed(Entity entity)
    {
        Cleanup();
    }

    public override void EntityRemoved(Scene scene)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if(removed) return;

        texture = null;

        Visible = false;

        removed = true;
    }
}
