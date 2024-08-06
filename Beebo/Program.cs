using System;
using System.Collections.Generic;
using System.IO;

internal class Program
{
    public static bool UseSteamworks { get; private set; } = true;
    public static ulong LobbyToJoin { get; set; } = 0;

    public static StreamWriter LogFile { get; private set; }

    private static TextWriter oldOut;
    private static TextWriter oldError;
    private static Jelly.IO.TextWriterWrapper _consoleOut;
    private static Jelly.IO.TextWriterWrapper _consoleError;

    private static void Main(string[] args)
    {
        LogFile = File.CreateText(Path.Combine(Beebo.Main.ProgramPath, "latest.log"));
        LogFile.AutoFlush = true;

        oldOut = Console.Out;
        oldError = Console.Error;

        _consoleOut = new(oldOut);
        _consoleOut.OnWrite += Log;
        _consoleOut.OnWriteFormatted += LogFormatted;
        _consoleOut.OnWriteLine += LogLine;
        _consoleOut.OnWriteLineFormatted += LogLineFormatted;

        _consoleError = new(oldError);
        _consoleError.OnWrite += Error;
        _consoleError.OnWriteFormatted += ErrorFormatted;
        _consoleError.OnWriteLine += ErrorLine;
        _consoleError.OnWriteLineFormatted += ErrorLineFormatted;

        Console.SetOut(_consoleOut);
        Console.SetError(_consoleError);

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

    private static void Log(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
            LogFile.Write(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);
        else
            LogFile.Write(callback.Value);
    }

    private static void LogFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.Write(callback.Format, callback.Arg);
    }

    private static void LogLine(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
            LogFile.WriteLine(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);
        else
            LogFile.WriteLine(callback.Value);
    }

    private static void LogLineFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.WriteLine(callback.Format, callback.Arg);
    }

    // these duplicates exist because for some reason it doesn't work otherwise

    private static void Error(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
            LogFile.Write(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);
        else
            LogFile.Write(callback.Value);
    }

    private static void ErrorFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.Write(callback.Format, callback.Arg);
    }

    private static void ErrorLine(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
            LogFile.WriteLine(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);
        else
            LogFile.WriteLine(callback.Value);
    }

    private static void ErrorLineFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.WriteLine(callback.Format, callback.Arg);
    }
}
