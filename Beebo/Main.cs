using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.GameContent;
using Beebo.Graphics;
using Beebo.Net;

using Jelly;
using Jelly.Coroutines;
using Jelly.GameContent;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;
using Jelly.Localization;

namespace Beebo;

public class Main : Jelly.GameServer
{
    internal static Main Instance { get; private set; } = null;

    public static Logger Logger { get; } = new("Main");

    public static ulong TotalFrames { get; private set; }

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

    public static Texture2D? DefaultSteamProfile { get; private set; }

    public static float FreezeTimer { get; set; }

    public static bool ChatWindowOpen { get; private set; } = false;
    public static string CurrentChatInput { get; private set; } = "";

    public static bool PlayerControlsDisabled => ChatWindowOpen || Instance.Server || !Instance.IsActive;

    public static string SaveDataPath => Path.Combine(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    public static SpriteFont RegularFont { get; private set; }
    public static SpriteFont RegularFontBold { get; private set; }

    public static Dictionary<CSteamID, Texture2D> AlreadyLoadedAvatars { get; } = [];

    private readonly GraphicsDeviceManager _graphics;
    private Camera camera;

    private readonly bool steamFailed;

    private Scene Scene => SceneManager.ActiveScene;

    public static class Debug
    {
        public static bool LogToChat { get; set; }
    }

    public Main() : base()
    {
        if(Instance is not null) throw new Exception("You can't start the game more than once 4head");

        Instance = this;

        #if DEBUG
        JellyBackend.DebugEnabled = true;
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
                    SteamManager.Logger.LogError("Game wasn't started by Steam-client! Restarting..");
                    Exit();
                }
            }
            catch(DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it.
                SteamManager.Logger.LogError("Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\nCaused by " + e);
                steamFailed = true;
            }
        }
    }

    protected override void Initialize()
    {
        Logger.LogInfo("Entering main loop");

        if(!Server) Renderer.Initialize(_graphics, GraphicsDevice, Window);

        camera = new Camera();

        if(Program.UseSteamworks)
        {
            if(!steamFailed && SteamManager.Init(Server))
            {
                Exiting += Game_Exiting;
            }
        }

        RegistryManager.Initialize();

        JellyBackend.Initialize(new BeeboContentProvider());

        LocalizationManager.CurrentLanguage = "jp-jp";

        Logger.LogInfo("test: " + LocalizationManager.GetLocalizedValue("test"));
        Logger.LogInfo("test2: " + LocalizationManager.GetLocalizedValue("test2"));

        BeeboImGuiRenderer.Initialize(this);

        SceneManager.ActiveSceneChanged += SceneChanged;

        if(!Server) base.Initialize();
        else LoadContent();
    }

    protected override void LoadContent()
    {
        // server + client resources

        if(Server) return;

        // client resources

        Renderer.LoadContent(Content);

        RegularFont = Content.Load<SpriteFont>("Fonts/default");
        RegularFontBold = Content.Load<SpriteFont>("Fonts/defaultBold");

        DefaultSteamProfile = Content.Load<Texture2D>("Images/UI/Multiplayer/DefaultProfile");

        BeeboImGuiRenderer.LoadContent(Content);
    }

    protected override void BeginRun()
    {
        ChangeScene("Title");

        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        JellyBackend.PreUpdate(delta);

        if(!Server)
        {
            Input.IgnoreInput = !IsActive;

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
                CurrentChatInput = "";
            }
            else
            {
                Exit();
                return;
            }
        }

        if(Input.GetPressed(Keys.F1))
        {
            JellyBackend.DebugEnabled = !JellyBackend.DebugEnabled;
        }

        if(SteamManager.IsSteamRunning)
        {
            SteamAPI.RunCallbacks();
            // P2PManager.ReadAvailablePackets();
        }

        GlobalCoroutineRunner.Update(delta);

        if(FreezeTimer > 0)
            FreezeTimer = Math.Max(FreezeTimer - Time.DeltaTime, 0);
        else
        {
            Scene?.PreUpdate();
            Scene?.Update();
            Scene?.PostUpdate();
        }

        if(ChatWindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            CurrentChatInput += string.Join(null, input);
            CurrentChatInput = CurrentChatInput[..MathHelper.Min(CurrentChatInput.Length, 4096)];

            if(backspace && CurrentChatInput.Length > 0)
            {
                CurrentChatInput = CurrentChatInput[..^1];
            }
        }

        if(Input.GetPressed(Keys.Enter))
        {
            ChatWindowOpen = !ChatWindowOpen;
            if(!ChatWindowOpen && CurrentChatInput.Length > 0)
            {
                string message = CurrentChatInput[..MathHelper.Min(CurrentChatInput.Length, 4096)];

                WriteChatMessage(message, (!SteamManager.IsSteamRunning) ? CSteamID.Nil : P2PManager.MyID, false);
                CurrentChatInput = "";
            }
        }

        if(!ChatWindowOpen)
        {
            if(Input.GetPressed(Keys.F3))
            {
                if(SceneManager.ActiveScene is not null)
                {
                    var json = SceneManager.ActiveScene.Serialize(false);
                    Logger.LogInfo(json);
                    // var newScene = SceneDef.Deserialize(json);
                    // Logger.LogInfo(newScene.Serialize(false));
                }
            }
        }

        camera.Update();

        JellyBackend.PostUpdate(delta);

        base.Update(gameTime);

        BeeboImGuiRenderer.Update(gameTime);

        TotalFrames++;
    }

    private void PreDraw(GameTime gameTime)
    {
        Scene?.PreDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        PreDraw(gameTime);

        Renderer.BeginDraw(SamplerState.PointWrap, camera.Transform);

        var rect = GraphicsDevice.ScissorRectangle;
        GraphicsDevice.ScissorRectangle = new(0, 0, Scene.Width, Scene.Height);

        Scene?.Draw();
        Scene?.PostDraw();

        GraphicsDevice.ScissorRectangle = rect;

        Renderer.EndDraw();
        Renderer.BeginDrawUI();

        Scene?.DrawUI();

        BeeboImGuiRenderer.DrawUI(gameTime);

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
                    RegularFont.MeasureString(CurrentChatInput).X + (CurrentChatInput.Split(' ').Length - 1) * spaceWidth
                );

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    RegularFont,
                    CurrentChatInput,
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

        BeeboImGuiRenderer.PostDraw(gameTime);
    }

    private void SceneChanged(Scene oldScene, Scene newScene)
    {
        Entity entity = new();
        EntityRegistry.SimplePlayer.OnCreate(entity);
        newScene.Entities.Add(entity);
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
                Logger.LogInfo("All players have finished loading, beginning scene");
                Instance.Scene?.Begin();
                break;
            }
        }
    }

    public static void WriteChatMessage(string message, CSteamID origin, bool system = false, bool noLog = false)
    {
        if(system)
        {
            if(!noLog)
                Logger.LogInfo("Server msg: " + message);

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
                Logger.LogInfo(name + " says: " + message);

            ChatHistory.Add(new($"{name}: {message}", Color.White));
        }

        if(GlobalCoroutineRunner.IsRunning(nameof(ChatDisappearDelay)))
            GlobalCoroutineRunner.Stop(nameof(ChatDisappearDelay));
        GlobalCoroutineRunner.Run(nameof(ChatDisappearDelay), ChatDisappearDelay());
    }

    private static void OnSceneTransition(Scene from, Scene to)
    {
        to.TimeScale = 1f;
    }

    public static void ChangeScene(string name)
    {
        if(SceneManager.ActiveScene?.Name == name)
            return;

        SceneManager.ActiveScene = Registries.Get<SceneRegistry>().GetDef(name).Build();

        // networking stuff
    }

    public static void HandleLeavingLobby()
    {
        ChangeScene("Title");
    }

    public static byte[] GetSyncPacket()
    {
        using var stream = new MemoryStream();
        var binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(Instance.Scene.Serialize());

        return stream.GetBuffer();
    }

    public static void ReadSyncPacket(byte[] data)
    {
        // using var stream = new MemoryStream(data);
        // var binaryReader = new BinaryReader(stream);

        // var json = binaryReader.ReadString();
        // Logger.Info(json);

        // var newScene = SceneDef.Deserialize(json);
        // ChangeLocalScene(newScene?.Build());
        // scene?.Subscribe();
    }

    protected override void OnActivated(object sender, EventArgs args)
    {
        base.OnActivated(sender, args);

        Scene?.GainFocus();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        base.OnDeactivated(sender, args);

        Scene?.LoseFocus();
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
