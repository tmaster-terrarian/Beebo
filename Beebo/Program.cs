using System.Collections.Generic;

internal class Program
{
    public static bool UseSteamworks { get; private set; } = true;

    private static void Main(string[] args)
    {
        using var game = new Beebo.Main();

        if(args.Length > 0)
        {
            List<string> list = new(args);

            if(list.Contains("-dedServer"))
            {
                game.Server = true;
            }

            if(list.Contains("-noSteam"))
            {
                UseSteamworks = false;
            }

            if(list.Contains("+connect_lobby"))
            {
                int idx = list.IndexOf("+connect_lobby") + 1;

                if(ulong.TryParse(list[idx], System.Globalization.NumberStyles.Integer, null, out ulong res))
                    game.LobbyToJoin = res;
            }
        }

        game.Run();
    }
}
