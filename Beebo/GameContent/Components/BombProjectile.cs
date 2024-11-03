using System;
using Jelly;
using Jelly.Components;
using Jelly.Graphics;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.GameContent.Components;

public class BombProjectile : Projectile
{
    private int bounces;
    private int bouncesMax = 1;

    private float frame;

    private SoundEffectInstance throwSound = null;

    private bool exploded;

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        DestroyOnCollision = false;
        GravityScale = 0.5f;
        EffectedByGravity = true;

        throwSound = AudioRegistry.GetDefStatic("bomb_throw").Play();
    }

    public override void Update()
    {
        frame += 0.125f * Time.DeltaTime * 60;

        foreach(var entity in Scene.Entities.FindAllWithComponent<BulletProjectile>())
        {
            if(entity.GetComponent<BulletProjectile>() is not BulletProjectile bullet)
                continue;

            if(bullet.Center.DistanceSquared(Center) <= 36)
            {
                Scene.Entities.Remove(entity);
                Explode(true);
                break;
            }
        }

        base.Update();

        if(Center.Y > Scene.Height + 16)
        {
            Explode();
            return;
        }
    }

    public override void Draw()
    {
        while(frame > 2)
            frame -= 2;

        var texture = ContentLoader.LoadTexture("Images/Entities/bomb");
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

            AudioRegistry.GetDefStatic("bomb_bounce").Play();

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

            AudioRegistry.GetDefStatic("bomb_bounce").Play();

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

    public void Explode(bool big = false)
    {
        if(exploded)
            return;

        exploded = true;

        Rectangle explosionHitbox = big
        ? new Rectangle(
            Center - new Point(28, 48),
            new Point(61, 66)
        )
        : new Rectangle(
            Center - new Point(21, 36),
            new Point(46, 50)
        );

        Scene.Entities.Add(new Entity(new(Center.X, Center.Y)) {
            Components = {
                new ExplosionEffect {
                    Framerate = big ? 0.25f : 1/3f,
                    Scale = big ? 1.3f : 1f
                }
            }
        });

        int dmg = big ? Damage * 2 : Damage;

        Main.Camera.SetShake(big ? 6 : 4, big ? 60 : 40);

        throwSound?.Stop();
        AudioRegistry.GetDefStatic("bomb_explosion").Play();

        foreach(var entity in Scene.Entities.FindAllWithComponent<Moveable>())
        {
            if(entity.GetComponent<Moveable>() is not Moveable moveable)
                return;

            if(moveable.Intersects(explosionHitbox))
            {
                if(entity.GetComponent<Unit>() is Unit unit)
                {
                    UnitManager.Damage(new() {
                        Target = unit,
                        Attacker = Scene.Entities.FindByID(Owner)?.GetComponent<Unit>(),
                        Damage = dmg,
                    });
                }

                if(moveable is BulletCasing casing)
                {
                    var boostX = MathF.Cos((casing.Center - (Center + new Point(0, 6))).ToRotation());
                    var boostY = MathF.Sin((casing.Center - (Center + new Point(0, 6))).ToRotation());

                    if(casing.CheckColliding(casing.Hitbox.Shift(0, 1), !casing.CollidesWithJumpthroughs))
                    {
                        entity.Y -= 2;
                    }

                    if(Math.Abs(casing.velocity.X) < 3f)
                        casing.velocity.X = 3 * boostX;
                    else
                        casing.velocity.X += 2f * boostX;

                    if(Math.Abs(casing.velocity.Y) < 3f)
                        casing.velocity.Y = (casing.OnGround ? -3f * Math.Abs(boostY) : 3 * boostY) - 1;
                    else
                        casing.velocity.Y += 2f * boostY;
                }
            }
        }

        if(Main.Player?.GetComponent<Player>() is Player player)
        {
            if(player.Intersects(explosionHitbox))
            {
                var unit = player.Entity.GetComponent<Unit>();
                if(TeamInfo.CheckCanHurt(Team, unit.Team))
                {
                    UnitManager.Damage(new() {
                        Target = unit,
                        Attacker = Scene.Entities.FindByID(Owner)?.GetComponent<Unit>(),
                        Damage = dmg,
                    });
                }

                if(Team == unit.Team && big)
                {
                    var spd = player.CurrentMoveSpeed / 2;
                    var boostX = spd * MathF.Cos((player.BottomMiddle - new Point(0, 8) - Center).ToRotation());
                    var boostY = spd * MathF.Sin((player.BottomMiddle - new Point(0, 8) - Center).ToRotation());

                    if(Math.Abs(player.velocity.X) < 3.5f * spd)
                        player.velocity.X = 5 * boostX;
                    else
                        player.velocity.X += 2 * boostX;

                    if(Math.Abs(player.velocity.Y) < 3.5f * spd)
                        player.velocity.Y = 5 * boostY;
                    else
                        player.velocity.Y += 2 * boostY;
                }
            }
        }

        if(big)
        {
            Main.FreezeTimer = 0.1f;
        }

        Scene.Entities.Remove(Entity);
    }
}
