using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal class Program
{
    private const uint steam_appid = 480;

    private static Process process;
    private static bool running;

    private static readonly List<string> commands = [
        "help",
        "exit"
    ];

    private static void Main(string[] args)
    {
        try
        {
            HandleConsole(args);
        }
        catch(Exception e)
        {
            WriteError(e);
            WaitForInput();
        }
    }

    private static void HandleConsole(string[] args)
    {
        List<string> argumentList = new(args);

        var cd = AppDomain.CurrentDomain.BaseDirectory;

        Console.WriteLine("Welcome to the Beebo dedicated server\n");

        if(!argumentList.Contains("-noSteam")) // steamworks setup
        {
            bool apiDll = FileExists("steam_api64.dll");
            bool steamworksDotNetDll = FileExists("Steamworks.NET.dll");

            if(!apiDll || !steamworksDotNetDll)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The program was instructed to run with steamworks enabled, but the following required files could not be found:");

                if(!apiDll)
                {
                    Console.WriteLine("  steam_api64.dll");
                }

                if(!steamworksDotNetDll)
                {
                    Console.WriteLine("  Steamworks.NET.dll");
                }

                Cleanup();
                return;
            }

            if(!FileExists("steam_appid.txt"))
            {
                using var writer = File.CreateText(Path.Combine(cd, "steam_appid.txt"));
                writer.Write(steam_appid);
                writer.Close();
            }
        }

        if(CheckMissing(Path.Combine(cd, "Beebo.dll"), "Beebo.dll"))
        {
            Cleanup();
            return;
        }

        if(CheckMissing(Path.Combine(cd, "Beebo.exe"), "Beebo.exe"))
        {
            Cleanup();
            return;
        }

        var others = Process.GetProcessesByName("Beebo.exe");
        if(others.Length > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Other Beebo processes were detected, be sure to look into that if you didn't start them.");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        ProcessStartInfo info = new() {
            FileName = "Beebo.exe",
            Arguments = "-dedServer" + (args.Length > 0 ? ' ' + string.Join(' ', args) : ""),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            process = Process.Start(info);
            process.EnableRaisingEvents = true;

            process.Exited += Process_Exited;

            running = true;

            AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
        }
        catch(Exception e)
        {
            process?.Kill();
            WriteError(e);

            Cleanup();
            return;
        }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        WriteLoop();
        WriteErrorLoop();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        while (running)
        {
            try
            {
                var input = Console.In.ReadLine();
                if(input is not null && input != "")
                {
                    ReadCommand(input);
                }
            }
            catch(Exception e)
            {
                WriteError(e);
            }
        }

        Cleanup(false);
    }

    private static void Process_Exited(object sender, EventArgs e)
    {
        if(running)
        {
            running = false;
            WriteError("Beebo has closed unexpectedly, press any key to continue.");
            Console.ReadKey();
        }
    }

    private static async Task WriteLoop()
    {
        while (running)
        {
            try
            {
                string? data = await process?.StandardOutput.ReadLineAsync();
                if (data != null)
                {
                    Console.WriteLine(data);
                }
            }
            catch (Exception e)
            {
                WriteError(e);
            }
        }
    }

    private static async Task WriteErrorLoop()
    {
        while (running)
        {
            try
            {
                string? data = await process?.StandardError.ReadLineAsync();
                if (data != null)
                {
                    WriteError(data);
                }
            }
            catch (Exception e)
            {
                WriteError(e);
            }
        }
    }

    private static void Cleanup(bool wait = true)
    {
        if(wait)
        {
            WaitForInput();
        }
    }

    private static bool FileExists(string path)
    {
        return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
    }

    private static bool CheckMissing(string path, string? fileName = null)
    {
        if(!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path)))
        {
            WriteError(new FileNotFoundException($"{fileName ?? path} could not be located, ensure that the file and Beebo.Server.exe are in the same folder.", fileName));
            return true;
        }
        return false;
    }

    private static void WaitForInput()
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }

    private static void AppDomain_ProcessExit(object sender, EventArgs e)
    {
        running = false;
        process?.Kill();
    }

    private static void ReadCommand(string value)
    {
        Console.ForegroundColor = ConsoleColor.White;

        try
        {
            if(running) switch(value)
            {
                case "exit":
                    running = false;
                    process?.Kill();
                    return;
                case "help":
                    Console.WriteLine("List of available commands:");

                    foreach(var name in commands)
                    {
                        Console.WriteLine("  " + name);
                    }

                    Console.WriteLine();

                    return;
            }
        }
        catch(Exception e)
        {
            WriteError(e);
        }
    }

    private static void WriteError(object e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.ToString());
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
