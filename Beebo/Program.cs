using System.Collections.Generic;

internal class Program
{
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
        }

        game.Run();
    }
}
