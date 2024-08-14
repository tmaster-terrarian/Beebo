using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class ComponentRegistry : Registry<ComponentTypeDef>
{
    public static TypeSet ComponentTypes => RegistryManager.TypeResolver.GetTypeSet(typeof(Component));

    public override void Init()
    {
        // AddType(typeof(TestComponent));
        // AddType(typeof(TestComponent2));

        foreach (var type in ComponentTypes.DerivedTypes)
        {
            Register(new ComponentTypeDef {
                ComponentType = type,
                Name = type.Name
            });
        }

        Main.Logger.Info($"Registered Components:\n  - {string.Join("\n  - ", ComponentTypes.DerivedTypes)}");
    }
}
