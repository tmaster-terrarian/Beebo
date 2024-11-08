using System;
using Beebo.Components;

namespace Beebo;

public class DamageContext : EventArgs
{
    public Unit Attacker { get; set; }
    public Unit Target { get; set; }

    public int Damage { get; set; }
}
