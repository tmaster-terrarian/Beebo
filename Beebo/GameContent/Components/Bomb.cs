using Jelly;
using Jelly.Components;
using Jelly.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Beebo.GameContent.Components;

public class Bomb : Projectile
{
    private int bounces;
    private int maxBounces = 2;

    private float frame;

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        bboxOffset = new(-2, -2);
        DestroyOnCollision = true;
    }

    public override void Update()
    {
        frame += 0.125f;
        base.Update();

        if(Center.Y > Scene.Height + 16)
        {
            Explode();
        }
    }

    public override void Draw()
    {
        while(frame > 2)
            frame -= 2;

        var texture = Main.LoadContent<Texture2D>("Images/Entities/bomb");
        Rectangle drawFrame = GraphicsUtil.GetFrameInStrip(texture, frame, 2);
    }

    protected override void OnCollideX()
    {
        if(bounces < maxBounces)
        {
            bounces++;

            var w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(MathUtil.CeilToInt(velocity.X), 0));

            velocity.X *= -0.75f;

            if(w is not null)
                velocity.X += w.velocity.X;
        }
        else
        {
            Explode();
        }
    }

    protected void Explode()
    {
        // foreach(var entity in Scene.Entities)
        // {
        //     if(entity.GetComponent<Unit>() is Unit unit)
        //     {
        //         UnitManager.Damage(new() {
        //             Target = unit,
        //             Damage = Damage,
        //         });
        //     }
        // }
    }
}
