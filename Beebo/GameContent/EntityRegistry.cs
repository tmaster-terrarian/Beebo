using Beebo.GameContent.Components;
using Jelly.GameContent;
using Microsoft.Xna.Framework;

namespace Beebo.GameContent;

public class EntityRegistry : Registry<EntityDef>
{
    public override void Init()
    {
        Register(new() {
            Name = "PlayerBeebo",
            Entity = new() {
                Depth = 50,
                Tag = (uint)EntityTags.Player,
                Components = {
                    new Player {
                        State = PlayerState.IgnoreState
                    },
                    new Unit {
                        Team = Team.Player
                    }
                }
            }
        });
    }
}
