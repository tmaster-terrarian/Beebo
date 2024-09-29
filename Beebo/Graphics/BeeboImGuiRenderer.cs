using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

using ImGuiNET;

using Jelly;

using MonoGame.ImGuiNet;
using System;
using Jelly.Graphics;
using System.Collections.Generic;
using System.IO;

namespace Beebo.Graphics;

public static class BeeboImGuiRenderer
{
    private static bool enabled = false;

    private static readonly List<float> frameTimes = [];

    public static ImGuiRenderer GuiRenderer { get; private set; }

    public static bool Enabled { get => enabled; set => enabled = value; }

    internal static StringWriter ConsoleLines { get; } = new();

    internal static float scroll = 0;

    public static void Initialize(Game game)
    {
        GuiRenderer = new(game);

        ConsoleLines.NewLine = "\n";
    }

    public static void LoadContent(ContentManager content)
    {
        GuiRenderer.RebuildFontAtlas();
    }

    public static void Update()
    {
        if(Input.GetPressed(Keys.OemTilde) && !Chat.WindowOpen)
        {
            enabled = !enabled;
        }
    }

    public static void DrawUI()
    {
        if(!enabled) return;
    }

    public static void PostDraw()
    {
        while(frameTimes.Count < 59) frameTimes.Add(1/60f);
        frameTimes.Add((float)Time.GameTime.ElapsedGameTime.TotalSeconds);
        if(frameTimes.Count > 60)
            frameTimes.RemoveAt(0);

        GuiRenderer.BeginLayout(Time.GameTime);

        if(enabled)
        {
            ImGui.Begin("Debug Panel", ref enabled, ImGuiWindowFlags.MenuBar);

            if(ImGui.BeginMenuBar())
            {
                if(ImGui.BeginMenu("File"))
                {
                    if(ImGui.MenuItem("Open..", "Ctrl+O"))
                    {
                        
                    }

                    if(ImGui.MenuItem("Save", "Ctrl+S"))
                    {
                        
                    }

                    if(ImGui.MenuItem("Close", "Ctrl+W"))
                    {
                        enabled = false;
                    }

                    ImGui.EndMenu();
                }

                if(ImGui.BeginMenu("File2"))
                {
                    if(ImGui.MenuItem("Open.."))
                    {
                        
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            float[] samples = [..frameTimes];
            ImGui.PlotLines($"FPS: {MathF.Round(1f / System.Linq.Enumerable.Average(frameTimes))}", ref samples[0], 60);

            ImGui.TextColored(IGui(Color.Yellow), "Debug Console");

            // if(ImGui.BeginChild("Console Input", default, ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.ResizeX))
            // {
            //     if(ImGui.InputTextWithHint(string.Empty, LocalizationManager.GetLocalizedValue("debug.gui.commandsHint"), ref commandInput, 10000, ImGuiInputTextFlags.EnterReturnsTrue))
            //     {
            //         Commands.ExecuteCommand(commandInput, EntityCommandSource.Default);

            //         commandInput = "";
            //     }

            //     if(Input.GetAnyDown(InputType.Keyboard))
            //     {
            //         List<char> input = [..Input.GetTextInput()];
            //         input.Remove('`');
            //         input.Remove('\x127');
            //         input.Remove('\0');

            //         commandInput += string.Join(null, input);

            //         Commands.GetSuggestions(commandInput, EntityCommandSource.Default);
            //     }

            //     var context = Commands.Dispatcher.Parse(commandInput, EntityCommandSource.Default).Context.FindSuggestionContext(commandInput.Length);

            //     string[] _sR = Commands.Dispatcher.GetAllUsage(context.Parent, EntityCommandSource.Default, true);
            //     int cur = 0;

            //     ImGui.SameLine();
            //     if(ImGui.ListBox(string.Empty, ref cur, _sR, _sR.Length))
            //     {
            //         string[] strings = commandInput.Split(' ');
            //         if(strings.Length == 1)
            //             commandInput = commandInput[..MathHelper.Min(commandInput.Length, context.StartPos)] + strings[0];
            //     }

            //     ImGui.EndChild();
            // }

            if(ImGui.BeginChild("Scrolling", default, ImGuiChildFlags.Border))
            {
                foreach(var str in ConsoleLines.ToString().Split("\n"))
                    ImGui.Text(str);

                if(ImGui.GetScrollY() != scroll && scroll != -1)
                    ImGui.SetScrollY(scroll);

                scroll = -1;

                ImGui.EndChild();
            }

            ImGui.End();
        }

        GuiRenderer.EndLayout();
    }

    private static System.Numerics.Vector4 IGui(Color color) => color.ToVector4().ToNumerics();
    private static System.Numerics.Vector2 IGui(Vector2 vector2) => vector2.ToNumerics();
    private static System.Numerics.Vector2 IGui(Point point) => point.ToVector2().ToNumerics();
}
