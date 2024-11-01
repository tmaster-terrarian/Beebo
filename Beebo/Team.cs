using System;
using System.Collections.Generic;

namespace Beebo;

[Flags]
public enum Team
{
    Neutral = 1,
    Player = 2,
    Enemy = 4,
}

public class TeamInfo
{
    public Team CanHurt { get; }
    public Team CanHeal { get; }

    private static readonly Dictionary<Team, TeamInfo> teams = [];

    private TeamInfo(Team canHurt, Team canHeal)
    {
        CanHurt = canHurt;
        CanHeal = canHeal;
    }

    public static bool RegisterTeamInfo(Team representingTeam, Team canHurt = (Team)(-1), Team canHeal = (Team)(-1))
        => teams.TryAdd(representingTeam, new(canHurt, canHeal));

    public static TeamInfo? GetTeamInfo(Team representingTeam)
        => teams.TryGetValue(representingTeam, out TeamInfo info) ? info : null;

    public static bool CheckCanHurt(Team origin, Team target)
        => GetTeamInfo(origin)?.CanHurt.HasFlag(target) ?? false;

    public static bool CheckCanHeal(Team origin, Team target)
        => GetTeamInfo(origin)?.CanHeal.HasFlag(target) ?? false;

    static TeamInfo()
    {
        RegisterTeamInfo(
            Team.Neutral,
            canHurt: (Team)(-1), // -1 is 0b11111111111111111111111111111111, which matches all teams
            canHeal: (Team)(-1)
        );
        RegisterTeamInfo(
            Team.Player,
            canHurt: Team.Neutral | Team.Enemy,
            canHeal: Team.Player
        );
        RegisterTeamInfo(
            Team.Enemy,
            canHurt: Team.Neutral | Team.Player,
            canHeal: Team.Enemy
        );
    }
}
