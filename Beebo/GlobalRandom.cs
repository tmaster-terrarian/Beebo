using System;

namespace Beebo;

public static class GlobalRandom
{
    /// <summary>for enemy attacks</summary>
    public static Random EnemyAttacks { get; }

    /// <summary>for player attacks</summary>
    public static Random PlayerAttacks { get; }

    /// <summary>for visuals only</summary>
    public static Random Vfx { get; }

    // used invoke the static constructor as soon as possible
    internal static void Initialize() {}

    static GlobalRandom()
    {
        EnemyAttacks = new();
        PlayerAttacks = new();
        Vfx = new();
    }
}
