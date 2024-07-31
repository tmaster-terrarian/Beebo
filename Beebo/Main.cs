using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.Multiplayer;

using Jelly;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;

namespace Beebo;

public class Main : Jelly.GameServer
{
    static Main _instance = null;

    public static class AppMetadata
    {
        public const string Name = "Beebo";
        public const string Version = "1.0.0.0";
        public const int Build = 1;
    }

    public static Logger Logger { get; } = new();

    public static Point MousePosition => new(
        Mouse.GetState().X / Renderer.PixelScale,
        Mouse.GetState().Y / Renderer.PixelScale
    );

    public static Point MousePositionClamped => new(
        MathHelper.Clamp(Mouse.GetState().X / Renderer.PixelScale, 0, Renderer.ScreenSize.X - 1),
        MathHelper.Clamp(Mouse.GetState().Y / Renderer.PixelScale, 0, Renderer.ScreenSize.Y - 1)
    );

    public static bool IsClient { get; private set; }

    public static string SaveDataPath => new PathBuilder{AppendFinalSeparator = true}.Create(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    readonly GraphicsDeviceManager _graphics;
    Camera camera;

    readonly bool steamFailed;
    Texture2D? pfp;
    string username;

    public Main()
    {
        if(_instance is not null) throw new Exception("You can't start the game more than once 4head");

        _instance = this;

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferMultiSampling = false,
            SynchronizeWithVerticalRetrace = true,
            PreferredBackBufferWidth = Renderer.ScreenSize.X * Renderer.PixelScale,
            PreferredBackBufferHeight = Renderer.ScreenSize.Y * Renderer.PixelScale,
            GraphicsProfile = GraphicsProfile.HiDef,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        if(Server)
        {
            IsClient = false;
        }

        Trace.Listeners.Clear();

        if(Console.IsOutputRedirected)
        {
            TextWriterTraceListener tr1 = new TextWriterTraceListener(Console.Out);
            Trace.Listeners.Add(tr1);
        }

        TextWriterTraceListener tr2 = new TextWriterTraceListener(File.CreateText(Path.Combine(ProgramPath, "latest.log")));
        Trace.Listeners.Add(tr2);

        if(Program.UseSteamworks)
        {
            try
            {
                if(SteamAPI.RestartAppIfNecessary(SteamManager.AppId))
                {
                    Logger.Error("Steamworks.NET", "Game wasn't started by Steam-client! Restarting..");
                    Exit();
                }
            }
            catch(DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it.
                Logger.Error("Steamworks.NET", "Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\nCaused by " + e);
                steamFailed = true;
            }
        }
    }

    protected override void Initialize()
    {
        Logger.Info("Entering main loop");

        if(!Server) Renderer.Initialize(_graphics, GraphicsDevice, Window);

        camera = new Camera();

        if(Program.UseSteamworks && !steamFailed && SteamManager.Init())
            Exiting += Game_Exiting;

        if(!Server) base.Initialize();
        else LoadContent();
    }

    protected override void LoadContent()
    {
        if(!Server)
        {
            Renderer.LoadContent(Content);
        }

        if(Program.UseSteamworks)
        {
            if(SteamManager.IsSteamRunning)
            {
                pfp = GetSteamUserAvatar(GraphicsDevice);
                username = SteamFriends.GetPersonaName();
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if(Program.UseSteamworks)
        {
            if(SteamManager.IsSteamRunning)
                SteamAPI.RunCallbacks();
        }

        Input.RefreshKeyboardState();
        Input.RefreshMouseState();
        Input.RefreshGamePadState();

        if(Input.GetDown(Buttons.Start) || Input.GetDown(Keys.Escape))
        {
            Exit();
            return;
        }

        camera.Update();

        base.Update(gameTime);
    }

    private void PreDraw(GameTime gameTime)
    {
        // draw stuff
    }

    protected override void Draw(GameTime gameTime)
    {
        PreDraw(gameTime);

        Renderer.BeginDraw(SamplerState.PointWrap, camera.Transform);

        // draw stuff

        Renderer.EndDraw();
        Renderer.BeginDrawUI();

        if(Program.UseSteamworks)
        {
            if(SteamManager.IsSteamRunning)
            {
                if(pfp != null)
                {
                    Renderer.SpriteBatch.Draw(pfp, new Vector2(2, 2), Color.White);
                    Renderer.SpriteBatch.DrawString(Renderer.RegularFont, username, new Vector2(2, 2) + Vector2.UnitY * pfp.Height, Color.White);
                }
            }
        }

        Renderer.EndDrawUI();
        Renderer.FinalizeDraw();

        base.Draw(gameTime);
    }

    private void Game_Exiting(object sender, EventArgs e)
    {
        if(SteamManager.IsSteamRunning)
            SteamAPI.Shutdown();
    }

    private static Texture2D GetSteamUserAvatar(GraphicsDevice device)
    {
        // Get the icon type as a integer.
        var icon = SteamFriends.GetMediumFriendAvatar(SteamUser.GetSteamID());

        // Check if we got an icon type.
        if (icon != 0)
        {
            var ret = SteamUtils.GetImageSize(icon, out uint width, out uint height);

            if (ret && width > 0 && height > 0)
            {
                var rgba = new byte[width * height * 4];
                ret = SteamUtils.GetImageRGBA(icon, rgba, rgba.Length);
                if (ret)
                {
                    var texture = new Texture2D(device, (int)width, (int)height, false, SurfaceFormat.Color);
                    texture.SetData(rgba, 0, rgba.Length);
                    return texture;
                }
            }
        }
        return null;
    }
}
