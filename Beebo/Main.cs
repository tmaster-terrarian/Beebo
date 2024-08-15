using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.GameContent;
using Beebo.Net;

using Jelly;
using Jelly.Coroutines;
using Jelly.GameContent;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;
using System.Collections;

namespace Beebo;

public class Main : Jelly.GameServer
{
    private static Scene scene;
    private static Scene nextScene;

    private static int _playersConfirmed = 0;

    private static Texture2D _missingProfile;

    internal static Main Instance { get; private set; } = null;

    public static Logger Logger { get; } = new("Main");

    public static ulong Ticks { get; private set; }

    public static CoroutineRunner GlobalCoroutineRunner { get; } = new();

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

    public static List<Tuple<string, Color>> ChatHistory { get; } = [];

    public static bool WaitingForAllReady { get; set; }

    public static Texture2D? DefaultSteamProfile { get; private set; }

    public static Entity MyPlayer { get; private set; }

    /// <summary>
    /// The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
    /// </summary>
    public static Scene Scene
    {
        get => scene;
        set
        {
            if(!ReferenceEquals(scene, value))
            {
                nextScene = value;
            }
        }
    }

    public static float FreezeTimer { get; set; }

    public static bool ChatWindowOpen { get; private set; } = false;
    public static string ChatInput { get; private set; } = "";

    public static bool PlayerControlsDisabled => ChatWindowOpen || Instance.Server || !Instance.IsActive;

    public static string SaveDataPath => Path.Combine(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    public static SpriteFont RegularFont { get; private set; }
    public static SpriteFont RegularFontBold { get; private set; }

    public static Dictionary<CSteamID, Texture2D> AlreadyLoadedAvatars { get; } = [];

    private readonly GraphicsDeviceManager _graphics;
    private Camera camera;

    private readonly bool steamFailed;

    public static class Debug
    {
        public static bool Enabled { get; set; }

        public static bool LogToChat { get; set; } = true;
    }

    public Main() : base()
    {
        if(Instance is not null) throw new Exception("You can't start the game more than once 4head");

        Instance = this;

        #if DEBUG
        Debug.Enabled = true;
        #endif

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

        RegistryManager.Init();

        Providers.Initialize(new BeeboNetworkProvider(), new BeeboContentProvider());

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

                _missingProfile = new Texture2D(GraphicsDevice, 64, 64, false, SurfaceFormat.Color);
                _missingProfile.SetData(buffer);
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
                else
                {
                    ChangeScene("Title");
                }
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        Providers.SetDeltaTime((float)gameTime.ElapsedGameTime.TotalSeconds);

        if(!Server)
        {
            Input.IgnoreInput = !IsActive || Server;

            Input.RefreshKeyboardState();
            Input.RefreshMouseState();
            Input.RefreshGamePadState();

            Input.UpdateTypingInput(gameTime);
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

        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();
            P2PManager.ReadAvailablePackets();
        }

        GlobalCoroutineRunner.Update(Providers.DeltaTime);

        if(!WaitingForAllReady)
        {
            if(FreezeTimer > 0)
                FreezeTimer = Math.Max(FreezeTimer - Providers.DeltaTime, 0);
            else
            {
                scene?.PreUpdate();
                scene?.Update();
                scene?.PostUpdate();
            }
        }

        if(ChatWindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            ChatInput += string.Join(null, input);
            ChatInput = ChatInput[..MathHelper.Min(ChatInput.Length, 4095)];

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
                string message = ChatInput[..MathHelper.Min(ChatInput.Length, 4095)];

                WriteChatMessage(message, (!SteamManager.IsSteamRunning) ? CSteamID.Nil : P2PManager.MyID, false);
                P2PManager.SendP2PPacketString(PacketType.ChatMessage, message, PacketSendMethod.Reliable);
                ChatInput = "";
            }
        }

        if(!ChatWindowOpen && !WaitingForAllReady)
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

            if(Input.GetPressed(Keys.F1))
            {
                SteamFriends.ActivateGameOverlayInviteDialog(P2PManager.CurrentLobby);
            }

            if(Input.GetPressed(Keys.F2) && P2PManager.InLobby && IsHost)
            {
                ChangeScene("Test", true);
            }

            if(Input.GetPressed(Keys.F3))
            {
                if(scene is not null)
                {
                    var json = scene.Serialize(false);
                    Logger.Info(json);
                    var newScene = SceneDef.Deserialize(json);
                    Logger.Info(newScene.Serialize(false));
                }
            }
        }

        if(!WaitingForAllReady)
        {
            camera.Update();

            //Changing scenes
            if(scene != nextScene && !P2PManager.InLobby)
            {
                var lastScene = scene;

                scene?.End();
                scene = nextScene;

                MyPlayer = null;

                OnSceneTransition(lastScene, nextScene);

                Logger.Info($"Loaded scene {scene.Name}");

                AddMyPlayer();

                scene?.Begin();
            }
        }

        base.Update(gameTime);

        Ticks++;
    }

    private void PreDraw(GameTime gameTime)
    {
        scene?.PreDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        PreDraw(gameTime);

        Renderer.BeginDraw(SamplerState.PointWrap, camera.Transform);

        scene?.Draw();
        scene?.PostDraw();

        Renderer.EndDraw();
        Renderer.BeginDrawUI();

        scene?.DrawUI();

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

                    var name = SteamFriends.GetFriendPersonaName(member);

                    Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, name, new Vector2(2 + (66 * i), 2) + Vector2.UnitY * 64, Color.White, 4);

                    if(P2PManager.GetLobbyOwner() == member)
                        Renderer.SpriteBatch.Draw(Content.Load<Texture2D>("Images/UI/Multiplayer/Crown"), new Vector2(2 + (66 * i), 2) + Vector2.UnitY * (64 + 3) + Vector2.UnitX * (1 + RegularFont.MeasureString(name).X), Color.White);
                }
            }

            Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, "InLobby: " + P2PManager.InLobby, new Vector2(12, Renderer.ScreenSize.Y - 34), Color.White, 4);
            Renderer.SpriteBatch.DrawStringSpacesFix(RegularFont, "CurrentLobby: " + P2PManager.CurrentLobby.m_SteamID, new Vector2(14, Renderer.ScreenSize.Y - 24), Color.White, 4);
        }

        if(ChatWindowOpen || chatAlpha > 0)
        {
            int spaceWidth = 4;
            int chatWidth = 256;
            Point chatPos = new(2, Renderer.ScreenSize.Y - 16);

            float alpha = ChatWindowOpen ? 1 : chatAlpha;

            if(ChatHistory.Count > 0)
            {
                Renderer.SpriteBatch.Draw(
                    Renderer.PixelTexture,
                    new Rectangle(
                        chatPos.X,
                        chatPos.Y - 12 * MathHelper.Min(5, ChatHistory.Count),
                        chatWidth,
                        12 * MathHelper.Min(5, ChatHistory.Count)
                    ),
                    Color.Black * 0.5f * alpha
                );
            }

            if(ChatWindowOpen)
            {
                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(chatPos.X, chatPos.Y, chatWidth, 12), Color.Black * 0.67f);

                float x = chatWidth - 1 - MathHelper.Max(
                    chatWidth - 1,
                    RegularFont.MeasureString(ChatInput).X + (ChatInput.Split(' ').Length - 1) * spaceWidth
                );

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    RegularFont,
                    ChatInput,
                    new Vector2(x + chatPos.X + 1, chatPos.Y - 1),
                    Color.White,
                    spaceWidth
                );
            }

            for(int i = 0; i < 5; i++)
            {
                int index = ChatHistory.Count - 1 - i;
                if(index < 0) continue;

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    RegularFont,
                    ChatHistory[index].Item1,
                    new Vector2(chatPos.X + 1, chatPos.Y - 13 - (i * 12)),
                    ChatHistory[index].Item2 * alpha,
                    spaceWidth
                );
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

    private static void OnPlayersReady(ReadinessReason readinessReason)
    {
        switch(readinessReason)
        {
            case ReadinessReason.WaitForSceneLoad:
            {
                Logger.Info("All players have finished loading, beginning scene");
                P2PManager.SendP2PPacket(PacketType.CallbackResponse, [(byte)CallbackPacketType.SceneChange], PacketSendMethod.Reliable);
                scene?.Begin();
                break;
            }
        }
    }

    public static void PlayerReady(CSteamID steamIDRemote)
    {
        if(WaitingForAllReady)
        {
            _playersConfirmed++;
        }
    }

    public static void WriteChatMessage(string message, CSteamID origin, bool system = false, bool noLog = false)
    {
        if(system)
        {
            if(!noLog)
                Logger.Info("Server msg: " + message);

            ChatHistory.Add(new(message, Color.Yellow));
        }
        else
        {
            string name = "???";
            if(SteamManager.IsSteamRunning && origin != CSteamID.Nil)
            {
                name = SteamFriends.GetFriendPersonaName(origin);
            }

            if(!noLog)
                Logger.Info(name + " says: " + message);

            ChatHistory.Add(new($"{name}: {message}", Color.White));
        }

        if(GlobalCoroutineRunner.IsRunning(nameof(ChatDisappearDelay)))
            GlobalCoroutineRunner.Stop(nameof(ChatDisappearDelay));
        GlobalCoroutineRunner.Run(nameof(ChatDisappearDelay), ChatDisappearDelay());
    }

    private static void OnSceneTransition(Scene from, Scene to)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        to.TimeScale = 1f;
    }

    internal static void ChangeScene(string name, bool updateLobby = false)
    {
        if(Scene?.Name == name)
            return;

        if(!ChangeLocalScene(Registries.Get<SceneRegistry>().GetDef(name).Build()))
            return;

        if(updateLobby)
        {
            P2PManager.SendP2PPacketString(PacketType.SceneChange, name, PacketSendMethod.Reliable);

            _playersConfirmed = 0;
            WaitingForAllReady = true;

            if(GlobalCoroutineRunner.IsRunning(nameof(WaitForConfirmation)))
                GlobalCoroutineRunner.Stop(nameof(WaitForConfirmation));
            GlobalCoroutineRunner.Run(nameof(WaitForConfirmation), WaitForConfirmation(ReadinessReason.WaitForSceneLoad));

            Logger.Info("Waiting for everyone to load...");
        }
        else
        {
            WaitingForAllReady = false;
            scene?.Begin();
        }
    }

    private static bool ChangeLocalScene(Scene newScene)
    {
        nextScene = newScene;
        if(scene != nextScene)
        {
            var lastScene = scene;

            scene?.End();
            scene = nextScene;

            MyPlayer = null;

            OnSceneTransition(lastScene, nextScene);

            Logger.Info($"Loaded scene {newScene.Name}");

            AddMyPlayer();
            return true;
        }
        return false;
    }

    public static void HandleLeavingLobby()
    {
        _playersConfirmed = 0;
        WaitingForAllReady = false;

        if(MyPlayer is not null)
        {
            if(scene?.Entities.Remove(MyPlayer) ?? true)
            {
                MyPlayer = null;
            }
        }

        ChangeScene("Title", false);
        AlreadyLoadedAvatars.Clear();
    }

    public static byte[] GetSyncPacket()
    {
        using var stream = new MemoryStream();
        var binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(scene.Serialize());

        return stream.GetBuffer();
    }

    public static void ReadSyncPacket(byte[] data)
    {
        using var stream = new MemoryStream(data);
        var binaryReader = new BinaryReader(stream);

        var json = binaryReader.ReadString();
        Logger.Info(json);

        var newScene = SceneDef.Deserialize(json);
        ChangeLocalScene(newScene?.Build());
        scene?.Subscribe();
    }

    public static void AddMyPlayer()
    {
        if(scene is null) return;

        if(MyPlayer is not null)
        {
            if(scene.Entities.Remove(MyPlayer))
            {
                MyPlayer = null;
            }
        }

        var jsonEntity = new JsonEntity {
            Name = "SimplePlayer",
            NetID = NetID,
            Enabled = true,
            Visible = true
        };

        MyPlayer = jsonEntity.Create(scene, false);
        Registries.Get<EntityRegistry>()?.GetDef(jsonEntity.Name)?.OnCreate(MyPlayer);
    }

    protected override void OnActivated(object sender, EventArgs args)
    {
        base.OnActivated(sender, args);

        scene?.GainFocus();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        base.OnDeactivated(sender, args);

        scene?.LoseFocus();
    }

    public static Texture2D GetMediumSteamAvatar(CSteamID cSteamID)
    {
        return GetMediumSteamAvatar(Renderer.GraphicsDevice, cSteamID);
    }

    private static Texture2D GetMediumSteamAvatar(GraphicsDevice device, CSteamID cSteamID)
    {
        if(Instance.Server)
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

                    if(Util.TexturesRoughlyMatch(texture, _missingProfile, 0.9f, 0.05f))
                    {
                        AlreadyLoadedAvatars.Remove(cSteamID);
                        AlreadyLoadedAvatars.Add(cSteamID, DefaultSteamProfile);
                        return DefaultSteamProfile;
                    }

                    AlreadyLoadedAvatars.Remove(cSteamID);
                    AlreadyLoadedAvatars.Add(cSteamID, texture);
                    return texture;
                }
            }
        }

        AlreadyLoadedAvatars.Remove(cSteamID);
        AlreadyLoadedAvatars.Add(cSteamID, DefaultSteamProfile);
        return DefaultSteamProfile;
    }

    static IEnumerator WaitForConfirmation(ReadinessReason readinessReason)
    {
        while(_playersConfirmed + 1 < P2PManager.PlayerCount)
        {
            WaitingForAllReady = true;
            yield return null;
        }

        WaitingForAllReady = false;
        OnPlayersReady(readinessReason);
        _playersConfirmed = 0;
    }

    static float chatAlpha;

    static IEnumerator ChatDisappearDelay(float holdTime = 5f, float fadeTime = 1f)
    {
        chatAlpha = 1;

        yield return holdTime;

        while(chatAlpha > 0)
        {
            float interval = (float)Instance.TargetElapsedTime.TotalSeconds;
            chatAlpha -= interval / fadeTime;
            yield return null;
        }
    }

    private static readonly List<string> missingAssets = [];

    public static T LoadContent<T>(string assetName)
    {
        if(missingAssets.Contains(assetName)) return default;

        try
        {
            return Instance.Content.Load<T>(assetName);
        }
        catch(Exception e)
        {
            Console.Error.WriteLine(e.GetType().FullName + $": The content file \"{assetName}\" was not found.");
            missingAssets.Add(assetName);
            return default;
        }
    }
}
