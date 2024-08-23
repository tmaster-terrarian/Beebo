using Beebo.GameContent.Components;

using Jelly;
using Jelly.Components;
using Microsoft.Xna.Framework;

namespace Beebo.GameContent.Entities;

public class SimplePlayer : EntityDef
{
    // Jelly entity example

    // An 'EntityDef' is this project's way of creating entity templates that
    // can be easily serialized and deserialized to pass around between
    // multiplayer clients.

    // General usage tips:
    // - Use the JsonIgnore and JsonInclude member attributes to prevent
    // unwanted serialization and to get finer control over what component data
    // data gets synced between clients.
    // - To go along with the above practice, please do not use references to
    // any entities or components directly, as they will not serialize nicely
    // if you do, and will break your code!
    // - For all methods be sure to check CanUpdateLocally to ensure that any
    // entities / components don't get updated by every client in
    // multiplayer simultaneously. There are some catches within Jelly, but
    // they won't cover all cases so it's good to still be mindful of it.
    // - Don't use MarkForSync more than you need to, especially not too many
    // reliable syncs, as they will likely slow the connection for everyone
    // in the multiplayer lobby.

    public override void OnCreate(Entity entity)
    {
        // modify the entity here

        entity.Components.Add(new SpriteComponent {
            TexturePath = "Images/Entities/SimplePlayer/idle",
            Pivot = new(16, 32),
            Color = entity.NetID switch
            {
                0 => Color.Gold,
                1 => Color.AliceBlue,
                2 => Color.PaleGreen,
                3 => Color.HotPink,
                _ => Color.LightGray
            },
        });

        entity.Components.Add(new SimplePlayerBehavior());
    }
}
