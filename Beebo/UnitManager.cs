using Beebo.GameContent.Components;

namespace Beebo;

public static class UnitManager
{
    public static void Damage(DamageContext ctx)
    {
        ctx.Target.Hp -= ctx.Damage;
    }
}
