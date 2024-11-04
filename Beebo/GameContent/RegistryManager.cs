using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beebo.Mods;
using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public static class RegistryManager
{
    public static PolymorphicTypeResolver PolymorphicTypeResolver { get; } = new([typeof(Component), typeof(JsonEntity)]);

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
        TypeInfoResolver = PolymorphicTypeResolver,
    };

    public static EntityRegistry EntityRegistry { get; } = new();
    public static AudioRegistry AudioRegistry { get; } = new();

    public static void Initialize()
    {
        if(Assembly.GetExecutingAssembly() is Assembly assembly)
            PolymorphicTypeResolver.GetAllDerivedTypesFromAssembly(assembly);

        foreach(var mod in ModLoader.loadedMods)
            PolymorphicTypeResolver.GetAllDerivedTypesFromAssembly(mod.Assembly);

        Main.Logger.LogInfo($"Registered Components:\n  - {string.Join("\n  - ", PolymorphicTypeResolver.GetTypeSet(typeof(Component)).DerivedTypes)}");

        Registries.Add(EntityRegistry);
        Registries.Add(new SceneRegistry());
        Registries.Add(AudioRegistry);

        ModLoader.DoRegistriesInit();
    }
}
