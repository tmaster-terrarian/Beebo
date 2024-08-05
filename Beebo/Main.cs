using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;
using System.Collections.Generic;
using Beebo.Net;
using Jelly.Coroutines;

namespace Beebo;

public class Main : Jelly.GameServer
{
    private static Main _instance = null;

    private static string chatInput = "";

    private static Callback<EquippedProfileItemsChanged_t> CallResult_equippedProfileItemsChanged;

    public static Logger Logger { get; } = new();

    public static CoroutineRunner CoroutineRunner { get; } = new();

    public static TextWriterTraceListener LogFile { get; } = new TextWriterTraceListener(File.CreateText(Path.Combine(ProgramPath, "latest.log")));

    public static Point MousePosition => new(
        Mouse.GetState().X / Renderer.PixelScale,
        Mouse.GetState().Y / Renderer.PixelScale
    );

    public static Point MousePositionClamped => new(
        MathHelper.Clamp(Mouse.GetState().X / Renderer.PixelScale, 0, Renderer.ScreenSize.X - 1),
        MathHelper.Clamp(Mouse.GetState().Y / Renderer.PixelScale, 0, Renderer.ScreenSize.Y - 1)
    );

    public static byte NetID => (byte)P2PManager.GetMemberIndex(P2PManager.MyID);
    public static bool IsHost => P2PManager.GetLobbyOwner() == P2PManager.MyID;

    public static bool ChatWindowOpen { get; private set; } = false;

    public static bool ControlsDisabled => ChatWindowOpen || _instance.Server || !_instance.IsActive;

    public static string SaveDataPath => new PathBuilder{AppendFinalSeparator = true}.Create(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    public static SpriteFont RegularFont { get; private set; }
    public static SpriteFont RegularFontBold { get; private set; }

    readonly GraphicsDeviceManager _graphics;
    Camera camera;

    readonly bool steamFailed;

    public ulong LobbyToJoin { get; set; } = 0;

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
            {
                Exiting += Game_Exiting;
                CallResult_equippedProfileItemsChanged = Callback<EquippedProfileItemsChanged_t>.Create(OnEquippedProfileItemsChanged);
            }
        }

        if(!Server) base.Initialize();
        else LoadContent();
    }

    protected override void BeginRun()
    {
        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();

            if(Server)
                P2PManager.CreateLobby();
            else
            {
                if(LobbyToJoin != 0)
                {
                    P2PManager.JoinLobby(new CSteamID(LobbyToJoin));
                    LobbyToJoin = 0;
                }
            }
        }
    }

    protected override void LoadContent()
    {
        if(!Server)
        {
            Renderer.LoadContent(Content);

            RegularFont = Content.Load<SpriteFont>("Fonts/default");
            RegularFontBold = Content.Load<SpriteFont>("Fonts/defaultBold");

            if(SteamManager.IsSteamRunning)
            {
                // load content stuff for steam
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

        CoroutineRunner.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();
            P2PManager.ReadAvailablePackets();
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

        if(ChatWindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            chatInput += string.Join(null, input);
            chatInput = chatInput[..MathHelper.Min(chatInput.Length, 254)];

            if(backspace && chatInput.Length > 0)
            {
                chatInput = chatInput[..^1];
            }
        }

        if(Input.GetPressed(Keys.Enter))
        {
            ChatWindowOpen = !ChatWindowOpen;
            if(!ChatWindowOpen && chatInput.Length > 0)
            {
                string name = SteamFriends.GetPersonaName();
                string message = chatInput[..MathHelper.Min(chatInput.Length, 254)];

                SteamManager.Logger.Info(name + " says: " + message);
                P2PManager.ChatHistory.Add($"{name}: {message}");

                P2PManager.SendP2PPacketString(PacketType.ChatMessage, message, PacketDelivery.Reliable);
                chatInput = "";
            }
        }

        if(!ChatWindowOpen)
        {
            if(Input.GetPressed(Keys.C))
            {
                P2PManager.CreateLobby(ELobbyType.k_ELobbyTypePublic);
            }

            if(Input.GetPressed(Keys.L))
            {
                if(Input.GetDown(Keys.LeftControl))
                {
                    P2PManager.LeaveLobby();
                }
                else
                {
                    SteamManager.Logger.Info("Trying to get list of available lobbies ...");
                    SteamAPICall_t try_getList = SteamMatchmaking.RequestLobbyList();
                }
            }

            if(Input.GetPressed(Keys.J))
            {
                SteamManager.Logger.Info("Trying to join FIRST listed lobby ...");
                P2PManager.JoinLobby(SteamMatchmaking.GetLobbyByIndex(0));
            }

            if(Input.GetPressed(Keys.Q))
            {
                P2PManager.GetCurrentLobbyMembers(true);
            }

            if(Input.GetPressed(Keys.F1) && SteamFriends.GetPersonaName() == "bscit")
            {
                SteamFriends.ActivateGameOverlayInviteDialog(P2PManager.CurrentLobby);
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
            Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(2, Renderer.ScreenSize.Y - 14, Renderer.ScreenSize.X - 4, 10), Color.Black * 0.5f);

            float x = 4 - (Renderer.ScreenSize.X - MathHelper.Max(Renderer.ScreenSize.X, RegularFont.MeasureString(chatInput).X));
            Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, chatInput, new Vector2(x, Renderer.ScreenSize.Y - 13), Color.White, 4);

            for(int i = 0; i < 5; i++)
            {
                int index = P2PManager.ChatHistory.Count - 1 - i;
                if(index < 0) continue;

                Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, P2PManager.ChatHistory[index], new Vector2(20, Renderer.ScreenSize.Y - 24 - (i * 10)), Color.White, 4);
            }
        }

        if(SteamManager.IsSteamRunning)
        {
            var members = P2PManager.GetCurrentLobbyMembers();
            if(members.Count > 0)
            {
                for(int i = 0; i < members.Count; i++)
                {
                    CSteamID member = members[i];

                    var texture = GetSteamUserAvatar(member);
                    Renderer.SpriteBatch.Draw(texture, new Vector2(2 + (66 * i), 2), Color.White);

                    Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, SteamFriends.GetFriendPersonaName(member), new Vector2(2 + (66 * i), 2) + Vector2.UnitY * 64, Color.White, 4);
                }
            }

            if(!ChatWindowOpen)
            {
                Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, "InLobby: " + P2PManager.InLobby, new Vector2(12, Renderer.ScreenSize.Y - 34), Color.White, 4);
                Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, "CurrentLobby: " + P2PManager.CurrentLobby.m_SteamID, new Vector2(14, Renderer.ScreenSize.Y - 24), Color.White, 4);
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

    private void OnEquippedProfileItemsChanged(EquippedProfileItemsChanged_t param)
    {
        if(P2PManager.GetMemberIndex(param.m_steamID, out int index))
        {
            _alreadyLoadedAvatars.Remove(param.m_steamID);
        }
    }

    public static Texture2D GetSteamUserAvatar(CSteamID cSteamID)
    {
        return GetSteamUserAvatar(Renderer.GraphicsDevice, cSteamID);
    }

    private static Texture2D GetSteamUserAvatar(GraphicsDevice device, CSteamID cSteamID)
    {
        if(_alreadyLoadedAvatars.TryGetValue(cSteamID, out Texture2D value))
            return value;

        if(_instance.Server)
            return null;

        // Get the icon type as a integer.
        int icon = SteamFriends.GetMediumFriendAvatar(cSteamID);

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

        var tex = _instance.Content.Load<Texture2D>("Images/UI/Multiplayer/DefaultProfile");

        _alreadyLoadedAvatars.Add(cSteamID, tex);
        return tex;
    }
}
