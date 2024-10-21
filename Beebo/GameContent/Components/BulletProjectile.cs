using Jelly;
using Jelly.Components;

namespace Beebo.GameContent.Components;

public class BulletProjectile : Projectile
{
    public float Direction { get; set; }

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        bboxOffset = new(-2, -2);
        DestroyOnCollision = true;
        Lifetime = 200;
    }

    public override void EntityAwake()
    {
        Entity.AddComponent(new SpriteComponent {
            TexturePath = "Images/Entities/bullet",
            Pivot = new(8, 8),
            Rotation = Direction,
        });
    }
}
