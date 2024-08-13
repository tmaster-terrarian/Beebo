using System;
using System.Collections.Generic;
using System.IO;
using Steamworks;

internal class Program
{
    public static bool UseSteamworks { get; private set; } = true;
    public static ulong LobbyToJoin { get; set; } = 0;

    internal static StreamWriter LogFile { get; private set; }

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
        _consoleError.OnWrite += Log;
        _consoleError.OnWriteFormatted += LogFormatted;
        _consoleError.OnWriteLine += LogLine;
        _consoleError.OnWriteLineFormatted += LogLineFormatted;

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

        try
        {
            game.Run();
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    private static void Log(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            LogFile.Write(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);

            if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
            {
                int i = callback.Index ?? 0;
                int i2 = i + (callback.Count ?? buffer.Length) - 1;
                Beebo.Main.WriteChatMessage(string.Concat(buffer[i..i2]), CSteamID.Nil, true, true);
            }
        }
        else
        {
            LogFile.Write(callback.Value);

            if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
            {
                Beebo.Main.WriteChatMessage(callback.Value.ToString(), CSteamID.Nil, true, true);
            }
        }
    }

    private static void LogFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.Write(callback.Format, callback.Arg);

        if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
        {
            Beebo.Main.WriteChatMessage(string.Format(callback.Format, callback.Arg), CSteamID.Nil, true, true);
        }
    }

    private static void LogLine(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            LogFile.WriteLine(buffer, callback.Index ?? 0, callback.Count ?? buffer.Length);

            if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
            {
                int i = callback.Index ?? 0;
                int i2 = i + (callback.Count ?? buffer.Length) - 1;
                Beebo.Main.WriteChatMessage(string.Concat(buffer[i..i2]), CSteamID.Nil, true, true);
            }
        }
        else
        {
            LogFile.WriteLine(callback.Value);

            if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
            {
                Beebo.Main.WriteChatMessage(callback.Value.ToString(), CSteamID.Nil, true, true);
            }
        }
    }

    private static void LogLineFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.WriteLine(callback.Format, callback.Arg);

        if(Beebo.Main.Debug.Enabled && Beebo.Main.Debug.LogToChat)
        {
            Beebo.Main.WriteChatMessage(string.Format(callback.Format, callback.Arg), CSteamID.Nil, true, true);
        }
    }
}
