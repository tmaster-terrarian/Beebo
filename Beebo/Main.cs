using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.Net;

using Jelly;
using Jelly.Coroutines;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;

namespace Beebo;

public class Main : Jelly.GameServer
{
    private static Main _instance = null;

    private static Texture2D _missingProfile;

    public static Logger Logger { get; } = new();

    public static CoroutineRunner CoroutineRunner { get; } = new();

    public static Point MousePosition => new(
        Mouse.GetState().X / Renderer.PixelScale,
        Mouse.GetState().Y / Renderer.PixelScale
    );

    public static Point MousePositionClamped => new(
        MathHelper.Clamp(Mouse.GetState().X / Renderer.PixelScale, 0, Renderer.ScreenSize.X - 1),
        MathHelper.Clamp(Mouse.GetState().Y / Renderer.PixelScale, 0, Renderer.ScreenSize.Y - 1)
    );

    public static int NetID => P2PManager.GetMemberIndex(P2PManager.MyID);
    public static bool IsHost => P2PManager.GetLobbyOwner() == P2PManager.MyID;

    public static Texture2D DefaultSteamProfile { get; private set; }

    public static bool ChatWindowOpen { get; private set; } = false;
    public static string ChatInput { get; private set; } = "";

    public static bool PlayerControlsDisabled => ChatWindowOpen || _instance.Server || !_instance.IsActive;

    public static string SaveDataPath => new PathBuilder{AppendFinalSeparator = true}.Create(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    public static SpriteFont RegularFont { get; private set; }
    public static SpriteFont RegularFontBold { get; private set; }

    public static Dictionary<CSteamID, Texture2D> AlreadyLoadedAvatars { get; } = [];

    private readonly GraphicsDeviceManager _graphics;
    private Camera camera;

    private readonly bool steamFailed;

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
            }
        }

        if(!Server) base.Initialize();
        else LoadContent();
    }

    protected override void LoadContent()
    {
        if(!Server)
        {
            Renderer.LoadContent(Content);

            RegularFont = Content.Load<SpriteFont>("Fonts/default");
            RegularFontBold = Content.Load<SpriteFont>("Fonts/defaultBold");

            DefaultSteamProfile = Content.Load<Texture2D>("Images/UI/Multiplayer/DefaultProfile");

            if(SteamManager.IsSteamRunning)
            {
                var binReader = File.OpenRead(Path.Combine(ProgramPath, "Content", "Images", "UI", "Multiplayer", "MissingProfile.bin"));
                byte[] buffer = new byte[64 * 64 * 4];

                binReader.Read(buffer, 0, buffer.Length);
                binReader.Dispose();

                (_missingProfile = new Texture2D(GraphicsDevice, 64, 64, false, SurfaceFormat.Color)).SetData(buffer);
            }
        }
    }

    protected override void BeginRun()
    {
        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();

            if(Server)
                P2PManager.CreateLobby(LobbyType.Public, 5);
            else
            {
                if(Program.LobbyToJoin != 0)
                {
                    P2PManager.JoinLobby((CSteamID)Program.LobbyToJoin);
                    Program.LobbyToJoin = 0;
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
                ChatInput = "";
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

            ChatInput += string.Join(null, input);
            ChatInput = ChatInput[..MathHelper.Min(ChatInput.Length, 254)];

            if(backspace && ChatInput.Length > 0)
            {
                ChatInput = ChatInput[..^1];
            }
        }

        if(Input.GetPressed(Keys.Enter))
        {
            ChatWindowOpen = !ChatWindowOpen;
            if(!ChatWindowOpen && ChatInput.Length > 0)
            {
                string name = SteamFriends.GetPersonaName();
                string message = ChatInput[..MathHelper.Min(ChatInput.Length, 254)];

                SteamManager.Logger.Info(name + " says: " + message);
                P2PManager.ChatHistory.Add($"{name}: {message}");

                P2PManager.SendP2PPacketString(PacketType.ChatMessage, message, PacketSendMethod.Reliable);
                ChatInput = "";
            }
        }

        if(!ChatWindowOpen)
        {
            if(Input.GetPressed(Keys.C))
            {
                P2PManager.CreateLobby(LobbyType.FriendsOnly);
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

            float x = 4 - (Renderer.ScreenSize.X - MathHelper.Max(Renderer.ScreenSize.X, RegularFont.MeasureString(ChatInput).X));
            Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, ChatInput, new Vector2(x, Renderer.ScreenSize.Y - 13), Color.White, 4);

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

                    var texture = GetMediumSteamAvatar(member);
                    Renderer.SpriteBatch.Draw(texture, new Vector2(2 + (66 * i), 2), Color.White);

                    Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, SteamFriends.GetFriendPersonaName(member), new Vector2(2 + (66 * i), 2) + Vector2.UnitY * 64, Color.White, 4);

                    if(P2PManager.GetLobbyOwner() == member)
                        Renderer.SpriteBatch.Draw(Content.Load<Texture2D>("Images/UI/Multiplayer/Crown"), new Vector2(2 + (66 * i), 2) + Vector2.UnitY * (64 + 12), Color.White);
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

    public static Texture2D GetMediumSteamAvatar(CSteamID cSteamID)
    {
        return GetMediumSteamAvatar(Renderer.GraphicsDevice, cSteamID);
    }

    private static Texture2D GetMediumSteamAvatar(GraphicsDevice device, CSteamID cSteamID)
    {
        if(_instance.Server)
            return null;

        if(AlreadyLoadedAvatars.TryGetValue(cSteamID, out Texture2D value))
            return value ?? DefaultSteamProfile;

        // Get the icon type as a integer.
        int icon = SteamFriends.GetMediumFriendAvatar(cSteamID);

        // Check if we got an icon type.
        if(icon != 0)
        {
            if(SteamUtils.GetImageSize(icon, out uint width, out uint height) && width > 0 && height > 0)
            {
                var rgba = new byte[width * height * 4];
                if(SteamUtils.GetImageRGBA(icon, rgba, rgba.Length))
                {
                    var texture = new Texture2D(device, (int)width, (int)height, false, SurfaceFormat.Color);
                    texture.SetData(rgba, 0, rgba.Length);

                    if(texture == _missingProfile)
                    {
                        AlreadyLoadedAvatars.Add(cSteamID, DefaultSteamProfile);
                        return DefaultSteamProfile;
                    }

                    AlreadyLoadedAvatars.Add(cSteamID, texture);
                    return texture;
                }
            }
        }

        AlreadyLoadedAvatars.Add(cSteamID, DefaultSteamProfile);
        return DefaultSteamProfile;
    }
}
