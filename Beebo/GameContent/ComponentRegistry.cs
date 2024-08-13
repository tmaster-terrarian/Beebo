using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class ComponentRegistry : Registry<ComponentTypeDef>
{
    public static PolymorphicTypeResolver TypeResolver { get; } = new(typeof(Component));

    public override void Init()
    {
        // AddType(typeof(TestComponent));
        // AddType(typeof(TestComponent2));

        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        if(assembly is not null)
            TypeResolver.DerivedTypes = [..TypeResolver.DerivedTypes, ..TypeResolver.GetDerivedTypesFromAssembly(assembly)];

        foreach(var type in TypeResolver.DerivedTypes)
        {
            Register(new ComponentTypeDef {
                ComponentType = type,
                Name = type.Name
            });
        }

        Main.Logger.Info($"Registered Components:\n  - {string.Join("\n  - ", TypeResolver.DerivedTypes)}");
    }
}
