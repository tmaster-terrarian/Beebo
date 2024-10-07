using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Beebo.Commands;
using Beebo.GameContent;
using Beebo.Graphics;
using Beebo.Net;

using Jelly;
using Jelly.Coroutines;
using Jelly.GameContent;
using Jelly.Graphics;
using Jelly.IO;

using Steamworks;

namespace Beebo;

public class Main : Game
{
    internal static Main Instance { get; private set; } = null;

    private readonly GraphicsDeviceManager _graphics;

    private static bool steamFailed;
    private static Camera camera;
    private static Scene Scene => SceneManager.ActiveScene;

    public static Logger Logger { get; } = new("Main");

    public static Camera Camera => camera;

    public static ulong TotalFrames { get; private set; }
    public static float FreezeTimer { get; set; }
    public static CoroutineRunner GlobalCoroutineRunner { get; } = new();

    public static bool PlayerControlsDisabled => Chat.WindowOpen || Input.InputDisabled || BeeboImGuiRenderer.Enabled || FreezeTimer > 0;

    public static int NetID => P2PManager.GetMemberIndex(P2PManager.MyID);
    public static bool IsHost => P2PManager.GetLobbyOwner() == P2PManager.MyID;
    public static Texture2D? DefaultSteamProfile { get; private set; }

    public static string SaveDataPath => Path.Combine(PathBuilder.LocalAppdataPath, AppMetadata.Name);
    public static string ProgramPath => AppDomain.CurrentDomain.BaseDirectory;

    public static Dictionary<CSteamID, Texture2D> AlreadyLoadedAvatars { get; } = [];

    public static class Debug
    {
        public static bool LogToChat { get; set; }
    }

    public Main() : base()
    {
        if(Instance is not null)
            throw new InvalidOperationException("You can't start the game more than once 4head");
        Instance = this;

        #if DEBUG
        JellyBackend.DebugEnabled = true;
        #endif

        _graphics = Renderer.GetDefaultGraphicsDeviceManager(this);

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

        Renderer.Initialize(_graphics, GraphicsDevice, Window);

        camera = new Camera();

        if(Program.UseSteamworks)
        {
            if(!steamFailed && SteamManager.Init(false))
            {
                Exiting += Game_Exiting;
            }
        }

        RegistryManager.Initialize();

        JellyBackend.Initialize(new BeeboContentProvider());

        CommandManager.Initialize();

        LocalizationManager.CurrentLanguage = "en-us";

        BeeboImGuiRenderer.Initialize(this);

        SceneManager.ActiveSceneChanged += SceneChanged;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        Renderer.LoadContent(Content);

        MasterRenderer.LoadContent(Content);

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
        JellyBackend.PreUpdate(gameTime);

        Input.InputDisabled = !IsActive;

        Input.RefreshKeyboardState();
        Input.RefreshMouseState();
        Input.RefreshGamePadState();

        Input.UpdateTypingInput(gameTime);

        if(Input.GetPressed(Buttons.Back) || Input.GetPressed(Keys.Escape))
        {
            if(Chat.WindowOpen)
            {
                Chat.CancelTypingAndClose();
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

        GlobalCoroutineRunner.Update(Time.UnscaledDeltaTime);

        if(FreezeTimer > 0)
            FreezeTimer = Math.Max(FreezeTimer - Time.UnscaledDeltaTime, 0);
        else
        {
            Scene?.PreUpdate();
            Scene?.Update();
            Scene?.PostUpdate();
        }

        Chat.Update(gameTime);

        camera.Update();

        JellyBackend.PostUpdate();

        base.Update(gameTime);

        BeeboImGuiRenderer.Update();

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

        BeeboImGuiRenderer.DrawUI();

        Chat.DrawUI();

        Renderer.EndDrawUI();
        Renderer.FinalizeDraw();

        base.Draw(gameTime);

        BeeboImGuiRenderer.PostDraw();
    }

    private void SceneChanged(Scene oldScene, Scene newScene)
    {
        
    }

    private void Game_Exiting(object sender, EventArgs e)
    {
        if(SteamManager.IsSteamRunning)
        {
            SteamManager.Cleanup();
        }
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

        binaryWriter.Write(Scene.Serialize());

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
