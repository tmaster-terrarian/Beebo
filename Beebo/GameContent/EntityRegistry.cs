using Beebo.Components;

using Jelly.GameContent;

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

        // maybe could load json files here too..
    }
}
