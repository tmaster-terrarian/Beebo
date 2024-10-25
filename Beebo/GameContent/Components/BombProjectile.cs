using System;
using Jelly;
using Jelly.Components;
using Jelly.Graphics;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.GameContent.Components;

public class BombProjectile : Projectile
{
    private int bounces;
    private int bouncesMax = 1;

    private float frame;

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        DestroyOnCollision = false;
    }

    public override void Update()
    {
        frame += 0.125f;
        base.Update();

        velocity.Y = MathUtil.Approach(velocity.Y, 20, 0.1f * Time.DeltaTime * 60);

        if(Center.Y > Scene.Height + 16)
        {
            Explode();
        }
    }

    public override void Draw()
    {
        while(frame > 2)
            frame -= 2;

        var texture = ContentLoader.Load<Texture2D>("Images/Entities/bomb");
        Rectangle drawFrame = GraphicsUtil.GetFrameInStrip(texture, frame, 2);

        Renderer.SpriteBatch.Draw(
            texture,
            Center.ToVector2(),
            drawFrame,
            Color.White,
            0, new Vector2(8, 8),
            1, SpriteEffects.None,
            0
        );
    }

    protected override void OnCollideX()
    {
        if(bounces < bouncesMax)
        {
            bounces++;

            var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(MathUtil.CeilToInt(velocity.X), 0));

            velocity.X *= -0.75f;

            if(w is not null)
                velocity.X += w.velocity.X;
        }
        else
        {
            velocity.X = 0;
            Explode();
        }
    }

    protected override void OnCollideY()
    {
        if(bounces < bouncesMax && velocity.Y > 0)
        {
            bounces++;

            var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(MathUtil.CeilToInt(velocity.X), 0));

            velocity.Y = -(velocity.Y * 0.75f) - 0.75f;
            velocity.X -= 0.01f * Math.Sign(velocity.X);

            if(w is not null)
            {
                velocity.Y += w.velocity.Y * 0.5f;
                velocity.X += w.velocity.X * 0.1f;
            }
        }
        else
        {
            velocity.Y = 0;
            Explode();
        }
    }

    public void Explode()
    {
        Rectangle explosionHitbox = new Rectangle(Center - new Point(21, 36), new Point(46, 50));

        foreach(var entity in Scene.Entities.FindAllWithComponent<Unit>())
        {
            if(entity.GetComponent<Moveable>() is not Moveable moveable)
                return;

            if(moveable.Intersects(explosionHitbox))
            {
                UnitManager.Damage(new() {
                    Target = entity.GetComponent<Unit>(),
                    Attacker = Scene.Entities.FindByID(Owner)?.GetComponent<Unit>(),
                    Damage = Damage,
                });
            }
        }

        Scene.Entities.Remove(Entity);
    }
}
