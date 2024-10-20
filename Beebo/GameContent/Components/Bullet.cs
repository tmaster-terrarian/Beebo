using Jelly;
using Jelly.Components;

namespace Beebo.GameContent.Components;

public class Bullet : Projectile
{
    private int lifetime = 200;

    public float Direction { get; set; }

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        bboxOffset = new(-2, -2);
        DestroyOnCollision = true;
    }

    public override void EntityAwake()
    {
        Entity.AddComponent(new SpriteComponent {
            TexturePath = "Images/Entities/bullet",
            Pivot = new(8, 8),
            Rotation = Direction,
        });
    }

    public override void Update()
    {
        lifetime--;
        if(lifetime == 0)
            Scene.Entities.Remove(Entity);
        else
            base.Update();
    }
}
