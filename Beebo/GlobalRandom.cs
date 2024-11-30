using System;

namespace Beebo;

public static class GlobalRandom
{
    public static Random EnemyAttacks { get; }

    static GlobalRandom()
    {
        EnemyAttacks = new();
    }
}
