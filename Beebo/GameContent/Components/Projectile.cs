using Jelly;
using Jelly.Components;

namespace Beebo.GameContent.Components;

public class Projectile : Actor
{
    public bool DestroyOnCollision { get; set; }

    public int Damage { get; set; }

    public long Owner { get; set; }

    public int MaxPierce { get; set; }

    public override void Update()
    {
        Update();

        MoveX(velocity.X, HandleCollisionX);
        MoveY(velocity.Y, HandleCollisionY);

        int pierce = 0;
        Scene.Entities.FindAllWithComponent<Actor>().ForEach((e) => {
            var actor = e.GetComponent<Actor>();
            if(e.EntityID != Owner && actor.Intersects(Hitbox) && e.GetComponent<Unit>() is Unit unit)
            {
                if(pierce < MaxPierce)
                {
                    
                    pierce++;
                }
                else if(DestroyOnCollision)
                    Scene.Entities.Remove(Entity);
            }
        });
    }

    private void HandleCollisionX()
    {
        OnCollideX();

        if(DestroyOnCollision)
        {
            Scene.Entities.Remove(Entity);
        }
    }

    private void HandleCollisionY()
    {
        OnCollideY();

        if(DestroyOnCollision)
        {
            Scene.Entities.Remove(Entity);
        }
    }

    protected virtual void OnCollideX() {}
    protected virtual void OnCollideY() {}
}
