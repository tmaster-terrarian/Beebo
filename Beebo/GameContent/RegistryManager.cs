using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public static class RegistryManager
{
    public static ComponentRegistry ComponentRegistry { get; } = new ComponentRegistry();
    public static SceneRegistry SceneRegistry { get; } = new SceneRegistry();

    public static PolymorphicTypeResolver TypeResolver { get; } = new([typeof(Component), typeof(EntityDef)]);

    public static JsonSerializerOptions SerializerOptions => new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonPointConverter(),
            new JsonVector2Converter(),
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = TypeResolver,
    };

    public static void Init()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        if(assembly is not null)
        {
            TypeResolver.GetAllDerivedTypesFromAssembly(assembly);
        }

        Registries.Add(ComponentRegistry);
        Registries.Add(SceneRegistry);
    }
}
