using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly.Graphics;
using Jelly;

namespace Beebo;

public class Main : Game
{
    public static Point MousePosition => new(
        Mouse.GetState().X / Renderer.PixelScale,
        Mouse.GetState().Y / Renderer.PixelScale
    );

    public static Point MousePositionClamped => new(
        MathHelper.Clamp(Mouse.GetState().X / Renderer.PixelScale, 0, Renderer.ScreenSize.X - 1),
        MathHelper.Clamp(Mouse.GetState().Y / Renderer.PixelScale, 0, Renderer.ScreenSize.Y - 1)
    );

    readonly GraphicsDeviceManager _graphics;
    Camera camera;

    public Main()
    {
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
    }

    protected override void Initialize()
    {
        Renderer.Initialize(_graphics, GraphicsDevice, Window);

        camera = new Camera();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        Renderer.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
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

        // draw ui

        Renderer.EndDrawUI();
        Renderer.FinalizeDraw();

        base.Draw(gameTime);
    }
}
