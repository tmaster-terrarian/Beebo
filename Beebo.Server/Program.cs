using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

internal class Program
{
    private const uint steam_appid = 480;

    private static Process process;
    private static bool running;
    private static bool waitForInput;

    private static readonly List<string> commands = [
        "help",
        "exit"
    ];

    private static void Main(string[] args)
    {
        List<string> argumentList = new(args);

        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

        Console.InputEncoding = System.Text.Encoding.ASCII;
        Console.TreatControlCAsInput = true;

        Console.WriteLine("Welcome to the Beebo dedicated server!");
        Console.WriteLine();

        var cd = AppDomain.CurrentDomain.BaseDirectory;

        if(!argumentList.Contains("-noSteam")) // steamworks setup
        {
            string message = "The program was instructed to run with steamworks enabled, but the following steam libraries could not be found:";

            bool apiDll = FileExists("steam_api64.dll");
            bool steamworksDotNetDll = FileExists("Steamworks.NET.dll");

            if(!apiDll || !steamworksDotNetDll)
            {
                WriteError(new FileNotFoundException(message));

                if(!apiDll)
                {
                    Console.WriteLine("  steam_api64.dll");
                }

                if(!steamworksDotNetDll)
                {
                    Console.WriteLine("  Steamworks.NET.dll");
                }

                WaitForInput();
                return;
            }

            if(!FileExists("steam_appid.txt"))
            {
                using var writer = File.CreateText("steam_appid.txt");
                {
                    writer.Write(steam_appid);
                    writer.Close();
                }
            }
        }

        if(CheckMissing(Path.Combine(cd, "Beebo.dll"), "Beebo.dll"))
        {
            WaitForInput();
            return;
        }

        if(CheckMissing(Path.Combine(cd, "Beebo.exe"), "Beebo.exe"))
        {
            WaitForInput();
            return;
        }

        var others = Process.GetProcessesByName("Beebo");
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

            running = true;
        }
        catch(Exception e)
        {
            process?.Kill();
            WriteError(e);

            waitForInput = true;
        }

        System.Threading.Tasks.Task.Run(() => {
            while(running)
            {
                Console.Title = "Beebo Dedicated Server - " + GC.GetTotalMemory(false);
                Thread.Sleep(1000);
            }
        });

        System.Threading.Tasks.Task.Run(() => {
            while(running)
            {
                try
                {
                    var data = process.StandardOutput.Read();
                    if(data != -1)
                    {
                        Console.Write((char)data);
                    }
                }
                catch(Exception e)
                {
                    WriteError(e);
                }

                // Thread.Sleep(new TimeSpan(16666667));
            }
        });

        while(running)
        {
            if(process.HasExited)
            {
                break;
            }

            try
            {
                var input = Console.ReadLine();
                if(input is not null)
                {
                    while(input.StartsWith(' '))
                    {
                        input = input.Replace(" ", "");
                    }

                    if(input != "")
                        ReadCommand(input);
                }
            }
            catch(Exception e)
            {
                WriteError(e);
            }
        }

        if(waitForInput)
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
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }

    private static void AppDomain_ProcessExit(object sender, EventArgs e)
    {
        process?.Kill();
    }

    private static void ReadCommand(string value)
    {
        Console.ForegroundColor = ConsoleColor.White;

        try
        {
            switch(value)
            {
                case "exit":
                {
                    process?.Kill();
                    running = false;
                    return;
                }
                case "help":
                {
                    Console.WriteLine("List of available commands:");

                    foreach(var name in commands)
                    {
                        Console.WriteLine("  " + name);
                    }

                    Console.WriteLine();

                    return;
                }
            }
        }
        catch(Exception e)
        {
            WriteError(e);
        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static void WriteError(object e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.ToString());
        Console.ForegroundColor = ConsoleColor.Gray;
    }
}
