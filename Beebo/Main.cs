using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.MultiplayerTest;

using Jelly;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;
using System.Collections.Generic;

namespace Beebo;

public class Main : Jelly.GameServer
{
    private static Main _instance = null;

    private static string chatInput = "";

    public static Logger Logger { get; } = new();

    public static TextWriterTraceListener LogFile { get; } = new TextWriterTraceListener(File.CreateText(Path.Combine(ProgramPath, "latest.log")));

    public static Point MousePosition => new(
        Mouse.GetState().X / Renderer.PixelScale,
        Mouse.GetState().Y / Renderer.PixelScale
    );

    public static Point MousePositionClamped => new(
        MathHelper.Clamp(Mouse.GetState().X / Renderer.PixelScale, 0, Renderer.ScreenSize.X - 1),
        MathHelper.Clamp(Mouse.GetState().Y / Renderer.PixelScale, 0, Renderer.ScreenSize.Y - 1)
    );

    public static bool IsGameOwner { get; set; }
    public static bool ChatWindowOpen { get; private set; } = false;

    public static bool ControlsDisabled => ChatWindowOpen || _instance.Server || !_instance.IsActive;

    public static string SaveDataPath => new PathBuilder{AppendFinalSeparator = true}.Create(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    readonly GraphicsDeviceManager _graphics;
    Camera camera;

    readonly bool steamFailed;
    Texture2D? pfp;
    string username;

    public Main() : base()
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
            IsGameOwner = true;
        }
        else IsGameOwner = false;

        Trace.AutoFlush = true;
        Trace.Listeners.Clear();

        if(Console.IsOutputRedirected)
        {
            TextWriterTraceListener tr1 = new ConsoleTraceListener();
            Trace.Listeners.Add(tr1);
        }

        Trace.Listeners.Add(LogFile);

        Logger.Error(new Exception("hello"));

        if(Program.UseSteamworks)
        {
            try
            {
                if(SteamAPI.RestartAppIfNecessary(SteamManager.AppID))
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

        if(Program.UseSteamworks)
        {
            if(!steamFailed && SteamManager.Init(Server))
                Exiting += Game_Exiting;
        }

        if(!Server) base.Initialize();
        else LoadContent();
    }

    protected override void LoadContent()
    {
        if(!Server)
        {
            Renderer.LoadContent(Content);

            if(Program.UseSteamworks)
            {
                if(SteamManager.IsSteamRunning)
                {
                    pfp = GetSteamUserAvatar(GraphicsDevice, SteamUser.GetSteamID());
                    username = SteamFriends.GetPersonaName();
                }
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if(!Server)
        {
            Input.IgnoreInput = !IsActive || Server;

            Input.RefreshKeyboardState();
            Input.RefreshMouseState();
            Input.RefreshGamePadState();

            Input.UpdateTypingInput(gameTime);
        }

        if(Program.UseSteamworks)
        {
            if(SteamManager.IsSteamRunning)
            {
                SteamAPI.RunCallbacks();
                // SteamManager.Update();
                // LobbyServer.Update();
                // LobbyManager.Update();
            }
        }

        if(Input.GetPressed(Buttons.Back) || Input.GetPressed(Keys.Escape))
        {
            if(ChatWindowOpen)
            {
                ChatWindowOpen = false;
                chatInput = "";
            }
            else
            {
                Exit();
                return;
            }
        }

        if(Input.GetPressed(Keys.Enter))
        {
            ChatWindowOpen = !ChatWindowOpen;
            if(!ChatWindowOpen && chatInput.Length > 0)
            {
                // LobbyManager.NetBroadcast(PacketType.ChatMessage, chatInput);
                chatInput = "";
            }
        }

        if(ChatWindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            chatInput += string.Join(null, input);

            if(backspace && chatInput.Length > 0)
            {
                chatInput = chatInput[..^1];
            }
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

        if(ChatWindowOpen)
        {
            Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(2, Renderer.ScreenSize.Y - 12, Renderer.ScreenSize.X - 4, 10), Color.Black * 0.5f);
            Renderer.SpriteBatch.DrawString(Renderer.RegularFont, chatInput, new Vector2(4, Renderer.ScreenSize.Y - 11), Color.White);
        }

        if(Program.UseSteamworks)
        {
            if(SteamManager.IsSteamRunning)
            {
                // var members = LobbyServer.GetCurrentLobbyMembers(false);
                // if(members.Count > 0)
                // {
                //     var fallbackTexture = Content.Load<Texture2D>("Images/UI/Multiplayer/DefaultProfile");

                //     for(int i = 0; i < members.Count; i++)
                //     {
                //         CSteamID member = members[i];

                //         Renderer.SpriteBatch.Draw(GetSteamUserAvatar(member), new Vector2(2 + (66 * i), 2), Color.White);

                //         Renderer.SpriteBatch.DrawString(Renderer.RegularFont, SteamFriends.GetFriendPersonaName(member), new Vector2(2 + (66 * i), 2) + Vector2.UnitY * pfp.Height, Color.White);
                //     }
                // }
            }
        }

        Renderer.EndDrawUI();
        Renderer.FinalizeDraw();

        base.Draw(gameTime);
    }

    private void Game_Exiting(object sender, EventArgs e)
    {
        if(SteamManager.IsSteamRunning)
        {
            SteamManager.Cleanup();
        }
    }

    private static readonly Dictionary<CSteamID, Texture2D> _alreadyLoadedAvatars = [];

    public static Texture2D GetSteamUserAvatar(CSteamID cSteamID)
    {
        return GetSteamUserAvatar(Renderer.GraphicsDevice, cSteamID);
    }

    private static Texture2D GetSteamUserAvatar(GraphicsDevice device, CSteamID cSteamID)
    {
        if(_alreadyLoadedAvatars.ContainsKey(cSteamID))
        {
            return _alreadyLoadedAvatars[cSteamID];
        }

        // Get the icon type as a integer.
        var icon = SteamFriends.GetMediumFriendAvatar(cSteamID);

        // Check if we got an icon type.
        if(icon != 0)
        {
            var ret = SteamUtils.GetImageSize(icon, out uint width, out uint height);

            if(ret && width > 0 && height > 0)
            {
                var rgba = new byte[width * height * 4];
                ret = SteamUtils.GetImageRGBA(icon, rgba, rgba.Length);
                if(ret)
                {
                    var texture = new Texture2D(device, (int)width, (int)height, false, SurfaceFormat.Color);
                    texture.SetData(rgba, 0, rgba.Length);

                    _alreadyLoadedAvatars.Add(cSteamID, texture);
                    return texture;
                }
            }
        }

        if(!_instance.Server)
        {
            var tex = _instance.Content.Load<Texture2D>("Images/UI/Multiplayer/DefaultProfile");

            _alreadyLoadedAvatars.Add(cSteamID, tex);
            return tex;
        }

        return null;
    }
}
