using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public static class RegistryManager
{
    public static class AllRegistries
    {
        public static ComponentTypeRegistry ComponentRegistry { get; } = new();

        public static EntityRegistry EntityRegistry { get; } = new();

        public static SceneRegistry SceneRegistry { get; } = new();
    }

    public static JsonSerializerOptions SerializerOptions => new() {
        Converters = {
            new JsonStringEnumConverter(),
            new JsonPointConverter(),
            new JsonVector2Converter(),
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = ComponentTypeRegistry.ComponentTypeResolver,
    };

    public static void Initialize()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        if(assembly is not null)
        {
            ComponentTypeRegistry.ComponentTypeResolver.GetAllDerivedTypesFromAssembly(assembly);
        }

        Registries.Add(AllRegistries.ComponentRegistry);
        Registries.Add(AllRegistries.EntityRegistry);
        Registries.Add(AllRegistries.SceneRegistry);
    }
}
