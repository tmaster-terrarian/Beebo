using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Jelly;

namespace Beebo.GameContent;

public class JsonCollisions
{
    public int[] Tiles { get; set; }

    public bool Visible { get; set; } = true;

    public List<Rectangle> JumpThroughs { get; set; }
    public List<Line> JumpThroughSlopes { get; set; }
    public List<Line> Slopes { get; set; }
}
