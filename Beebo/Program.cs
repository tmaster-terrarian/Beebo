using System;
using System.Collections.Generic;
using System.IO;

using Beebo;

using Jelly;

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
        LogFile.NewLine = "\n";

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

        using var game = new Main();

        if(args.Length > 0)
        {
            List<string> list = new(args);

            if(list.Contains("-noSteam"))
                UseSteamworks = false;

            {
                int idx;
                if((idx = list.IndexOf("+connect_lobby") + 1) > 0)
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
            throw;
        }
    }

    private static void Log(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            int i = callback.Index ?? 0, c = callback.Count ?? buffer.Length;
            LogFile.Write(buffer, i, c);

            Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.Write(buffer, i, c);
            Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

            if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
            {
                Chat.WriteChatMessage(string.Concat(string.Concat(buffer[i..(i + c)])), CSteamID.Nil, true, true);
            }
        }
        else
        {
            LogFile.Write(callback.Value);

            Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.Write(callback.Value);
            Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

            if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
            {
                Chat.WriteChatMessage(callback.Value.ToString(), CSteamID.Nil, true, true);
            }
        }
    }

    private static void LogFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.Write(callback.Format, callback.Arg);

        Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.Write(callback.Format, callback.Arg);
        Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

        if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
        {
            Chat.WriteChatMessage(string.Format(callback.Format, callback.Arg), CSteamID.Nil, true, true);
        }
    }

    private static void LogLine(object sender, Jelly.IO.TextWriterEventArgs callback)
    {
        if(callback.Value is char[] buffer)
        {
            int i = callback.Index ?? 0, c = callback.Count ?? buffer.Length;
            LogFile.WriteLine(buffer, i, c);

            Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.WriteLine(buffer, i, c);
            Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

            if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
            {
                Chat.WriteChatMessage(string.Concat(buffer[i..(i + c)]), CSteamID.Nil, true, true);
            }
        }
        else
        {
            LogFile.WriteLine(callback.Value);

            Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.WriteLine(callback.Value);
            Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

            if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
            {
                Chat.WriteChatMessage(callback.Value.ToString(), CSteamID.Nil, true, true);
            }
        }
    }

    private static void LogLineFormatted(object sender, Jelly.IO.TextWriterFormattedEventArgs callback)
    {
        LogFile.WriteLine(callback.Format, callback.Arg);

        Beebo.Graphics.BeeboImGuiRenderer.ConsoleLines.WriteLine(callback.Format, callback.Arg);
        Beebo.Graphics.BeeboImGuiRenderer.scroll = 100000000;

        if(JellyBackend.DebugEnabled && Beebo.Main.Debug.LogToChat)
        {
            Chat.WriteChatMessage(string.Format(callback.Format, callback.Arg), CSteamID.Nil, true, true);
        }
    }
}
