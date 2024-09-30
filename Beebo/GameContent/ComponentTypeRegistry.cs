using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class ComponentTypeRegistry : Registry<ComponentTypeDef>
{
    public static TypeSet ComponentTypes => ComponentTypeResolver.GetTypeSet(typeof(Component));

    public static PolymorphicTypeResolver ComponentTypeResolver { get; } = new([typeof(Component), typeof(JsonEntity)]);

    public override void Init()
    {
        foreach (var type in ComponentTypes.DerivedTypes)
        {
            Register(new ComponentTypeDef {
                ComponentType = type,
                Name = type.Name
            });
        }

        Main.Logger.LogInfo($"Registered Components:\n  - {string.Join("\n  - ", ComponentTypes.DerivedTypes)}");
    }
}
