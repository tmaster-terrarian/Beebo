using System.Diagnostics;

internal class Program
{
    private static Process process;
    private static bool running;
    private static bool waitForInput;

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;

        Console.WriteLine("Welcome to the Beebo dedicated server!");

        ProcessStartInfo info = new() {
            FileName = "Beebo.exe",
            Arguments = "-dedServer",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            process = Process.Start(info);
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += Process_OutputDataRecieved;
            process.ErrorDataReceived += Process_ErrorDataRecieved;

            running = true;
        }
        catch(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.ToString());
            Console.ForegroundColor = ConsoleColor.Gray;
            waitForInput = true;
        }

        while(running)
        {
            if(process.HasExited)
            {
                break;
            }

            var input = Console.ReadLine();
            if(input is not null && input != "")
            {
                while(input.StartsWith(' '))
                {
                    input = input.Replace(" ", "");
                }

                try
                {
                    ReadCommand(input);
                }
                catch(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.ToString());
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        if(waitForInput)
        {
            Console.ReadKey();
        }
    }

    private static void AppDomain_ProcessExit(object sender, EventArgs e)
    {
        process?.Kill();
    }

    private static void ReadCommand(string value)
    {
        Console.ForegroundColor = ConsoleColor.White;

        switch(value)
        {
            case "exit":
            {
                process?.Kill();
                running = false;
                return;
            }
        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static void Process_OutputDataRecieved(object sender, DataReceivedEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(e.Data ?? "");
    }

    private static void Process_ErrorDataRecieved(object sender, DataReceivedEventArgs e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Data ?? "");
    }
}
