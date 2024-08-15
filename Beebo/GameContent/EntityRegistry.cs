using Beebo.GameContent.Entities;

using Jelly.GameContent;

namespace Beebo.GameContent;

public class EntityRegistry : Registry<EntityDef>
{
    public static EntityDef SimplePlayer { get; } = new SimplePlayer();

    public override void Init()
    {
        Register(SimplePlayer);
    }
}
