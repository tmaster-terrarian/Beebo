using Jelly.GameContent;

namespace Beebo.GameContent;

public class SpriteRegistry : Registry<SpriteDef>
{
    public override void Init()
    {
        Register(new() {
            Name = "beebo-ledgegrab",
            TexturePath = "Images/Player/ledgegrab",
            Pivot = new(19, 12)
        });
    }
}
