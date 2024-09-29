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
    private static bool enabled = true;

    private static System.Numerics.Vector4 _colorV4 = IGui(Color.White);

    private static readonly List<float> frameTimes = [];

    public static ImGuiRenderer GuiRenderer { get; private set; }

    public static bool Enabled { get => enabled; set => enabled = value; }

    internal static StringWriter ConsoleLines { get; } = new();

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
        if(Input.GetPressed(Keys.OemTilde))
        {
            enabled = !enabled;
        }
    }

    public static void DrawUI()
    {
        if(!enabled) return;

        var v = new Vector4(_colorV4.X, _colorV4.Y, _colorV4.Z, _colorV4.W) * new Vector4(_colorV4.W, _colorV4.W, _colorV4.W, 1);

        Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(2, 2, 10, 10), new Color(v));
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
            ImGui.Begin("My First Tool", ref enabled, ImGuiWindowFlags.MenuBar);
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
                ImGui.EndMenuBar();
            }

            // Edit a color stored as 4 floats
            ImGui.ColorEdit4("Color", ref _colorV4);

            float[] samples = [..frameTimes];
            ImGui.PlotLines($"FPS: {MathF.Round(1f / System.Linq.Enumerable.Average(frameTimes))}", ref samples[0], 60);

            // Display contents in a scrolling region
            ImGui.TextColored(IGui(Color.Yellow), "Important Stuff");

            if(ImGui.BeginChild("Scrolling", default, ImGuiChildFlags.Border))
            {
                foreach(var str in ConsoleLines.ToString().Split("\n"))
                    ImGui.Text(str);

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
