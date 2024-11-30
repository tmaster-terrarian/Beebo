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
    public static SceneRegistry SceneRegistry { get; } = new();
    public static AudioRegistry AudioRegistry { get; } = new();
    public static CommandRegistry CommandRegistry { get; } = new();
    public static TilesetRegistry TilesetRegistry { get; } = new();

    public static void Initialize()
    {
        if(Assembly.GetExecutingAssembly() is Assembly assembly)
            PolymorphicTypeResolver.GetAllDerivedTypesFromAssembly(assembly);

        foreach(var mod in ModLoader.loadedMods)
            PolymorphicTypeResolver.GetAllDerivedTypesFromAssembly(mod.Assembly);

        var set1 = PolymorphicTypeResolver.GetTypeSet(typeof(Component)).DerivedTypes;
        if(set1.Count > 0)
            Main.Logger.LogInfo($"Registered Component Types:\n  - {string.Join("\n  - ", set1)}");

        var set2 = PolymorphicTypeResolver.GetTypeSet(typeof(JsonEntity)).DerivedTypes;
        if(set2.Count > 0)
            Main.Logger.LogInfo($"Registered JsonEntity Types:\n  - {string.Join("\n  - ", set2)}");

        Registries.Add(EntityRegistry);
        Registries.Add(SceneRegistry);
        Registries.Add(AudioRegistry);
        Registries.Add(CommandRegistry);
        Registries.Add(TilesetRegistry);

        ModLoader.DoRegistriesInit();
    }
}
