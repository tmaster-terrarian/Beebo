using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

internal class Program
{
    private static Process process;
    private static bool running;
    private static bool waitForInput;

    private static readonly List<string> commands = [
        "help",
        "exit"
    ];

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

        Console.InputEncoding = System.Text.Encoding.ASCII;
        Console.TreatControlCAsInput = true;

        Console.WriteLine("Welcome to the Beebo dedicated server!");
        Console.WriteLine();

        ProcessStartInfo info = new() {
            FileName = "Beebo.exe",
            Arguments = "-dedServer" + (args.Length > 0 ? ' ' + string.Join<string>(' ', args) : ""),
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
                    var line = process.StandardOutput.ReadLine();
                    if(line is not null)
                    {
                        Console.WriteLine(line);
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
            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }

    private static async void StartLineRead()
    {
        while(running)
        {
            try
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if(line is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(line);
                }
            }
            catch(Exception e)
            {
                WriteError(e);
            }
        }
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
