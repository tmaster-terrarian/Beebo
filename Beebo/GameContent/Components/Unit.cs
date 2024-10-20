using Jelly;

namespace Beebo.GameContent.Components;

public class Unit : Component
{
    public int HpMax { get; set; } = 1;

    public int Hp { get; set; }

    public Team Team { get; set; }

    public Unit()
    {
        Hp = HpMax;
    }
}
