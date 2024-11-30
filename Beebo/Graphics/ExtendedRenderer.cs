using System;

using Microsoft.Xna.Framework;

using Jelly;
using Jelly.Graphics;

namespace Beebo.Graphics;

public abstract class ExtendedRenderer : AbstractRenderer, IDrawable
{
    internal bool _visible = true;
    internal int _drawOrder;

    public Tag Tag { get; set; }

    public int DrawOrder {
        get => _drawOrder;
        internal set {
            if(_drawOrder != value)
            {
                _drawOrder = value;
                DrawOrderChanged?.Invoke(this, new());
            }
        }
    }

    public bool Visible {
        get => _visible;
        internal set {
            if(_visible != value)
            {
                _visible = value;
                VisibleChanged?.Invoke(this, new());
            }
        }
    }

    public event EventHandler<EventArgs> DrawOrderChanged;
    public event EventHandler<EventArgs> VisibleChanged;

    public virtual void BeginDraw(GameTime gameTime) {}

    public virtual void PostDraw(GameTime gameTime) {}

    public virtual void DrawDebug(GameTime gameTime) {}

    public virtual void DrawDebugUI(GameTime gameTime) {}

    public virtual void SceneBegin(Scene scene) {}

    public virtual void PreDraw() {}

    private new void PreDraw(GameTime gameTime) {}
}
