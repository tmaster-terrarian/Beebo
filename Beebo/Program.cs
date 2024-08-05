using System.Collections.Generic;

internal class Program
{
    public static bool UseSteamworks { get; private set; } = true;
    public static ulong LobbyToJoin { get; set; } = 0;

    private static void Main(string[] args)
    {
        using var game = new Beebo.Main();

        if(args.Length > 0)
        {
            List<string> list = new(args);

            if(list.Contains("-dedServer"))
                game.Server = true;

            if(list.Contains("-noSteam"))
                UseSteamworks = false;

            {
                int idx = list.IndexOf("+connect_lobby") + 1;
                if(idx > 0)
                {
                    if(idx < list.Count && ulong.TryParse(list[idx], System.Globalization.NumberStyles.Integer, null, out ulong res))
                        LobbyToJoin = res;
                }
            }
        }

        game.Run();
    }
}
