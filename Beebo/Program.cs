internal class Program
{
    private static void Main(string[] args)
    {
        using var game = new Beebo.Main();

        if(args.Length > 0)
        {
            if(args[0] == "-dedServer")
            {
                game.Server = true;
            }
        }

        game.Run();
    }
}
