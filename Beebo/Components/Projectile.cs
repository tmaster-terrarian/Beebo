using System.Collections.Generic;
using Jelly;
using Jelly.Components;
using Jelly.Utilities;

namespace Beebo.Components;

public class Projectile : Actor
{
    public bool DestroyOnCollision { get; set; }

    public int Damage { get; set; }

    public long Owner { get; set; }

    public Team Team { get; set; }

    public int MaxPierce { get; set; } = 1;

    public int Lifetime { get; set; } = -1;

    public bool EffectedByGravity { get; set; }

    private readonly List<long> piercedEntities = [];

    public float GravityScale { get; set; } = 1;

    private bool dead;

    public override void OnCreated()
    {
        CollidesWithJumpthroughs = false;
        NudgeOnMove = false;
    }

    public override void Update()
    {
        bool onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(Lifetime > 0)
        {
            Lifetime--;
            if(Lifetime == 0)
            {
                Scene.Entities.Remove(Entity);
                return;
            }
        }

        if(EffectedByGravity && !OnGround)
        {
            velocity.Y = MathUtil.Approach(velocity.Y, 20, 0.2f * GravityScale * (Time.DeltaTime * 60f));
        }

        MoveX(velocity.X, HandleCollisionX);
        MoveY(velocity.Y, HandleCollisionY);

        if(dead)
            return;

        int pierce = 0;
        Scene.Entities.FindAllWithComponent<Actor>().ForEach((e) => {
            var actor = e.GetComponent<Actor>();

            if(!piercedEntities.Contains(e.EntityID)
            && e.EntityID != Owner
            && actor.Intersects(Hitbox)
            && e.GetComponent<Unit>() is Unit unit
            && unit.Team != Team)
            {
                if(pierce < MaxPierce)
                {
                    pierce++;
                    piercedEntities.Add(e.EntityID);

                    OnCollideUnit(unit);
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
            dead = true;
        }
    }

    private void HandleCollisionY()
    {
        OnCollideY();

        if(DestroyOnCollision)
        {
            Scene.Entities.Remove(Entity);
            dead = true;
        }
    }

    protected virtual void OnCollideUnit(Unit unit)
    {
        UnitManager.Damage(new DamageContext {
            Damage = Damage,
            Attacker = Scene.Entities.FindByID(Owner)?.GetComponent<Unit>(),
            Target = unit
        });
    }

    protected virtual void OnCollideX() {}
    protected virtual void OnCollideY() {}
}
