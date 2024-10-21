using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

// kinda like a prefab
public class EntityDef : RegistryEntry
{
    public JsonEntity Entity { get; set; }

    public Entity Instantiate()
    {
        return Entity.Build();
    }
}
