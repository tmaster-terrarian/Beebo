using Jelly;
using Jelly.Components;

namespace Beebo.GameContent.Components;

public class Bullet : Actor
{
    public float Direction { get; set; }

    int life = 200;

    public override void OnCreated()
    {
        Width = 4;
        Height = 4;
        bboxOffset = new(-2, -2);
    }

    public override void EntityAdded(Scene scene)
    {
        Entity.AddComponent(new SpriteComponent {
            TexturePath = "Images/Entities/bullet",
            Pivot = new(8, 8),
        });
    }

    public override void Update()
    {
        life--;
        if(life == 0)
            Scene.Entities.Remove(Entity);

        MoveX(velocity.X, () => Scene.Entities.Remove(Entity));
        MoveY(velocity.Y, () => Scene.Entities.Remove(Entity));
    }
}
