using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Jelly;
using Jelly.Components;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class SceneRegistry : Registry<SceneDef>
{
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
        TypeInfoResolver = ComponentRegistry.TypeResolver,
    };

    public static PolymorphicTypeResolver EntityDefTypeResolver { get; } = new(typeof(EntityDef));

    public static Dictionary<string, SceneDef> DefsByName { get; } = [];

    public override void Init()
    {
        var title = Add("Title");

        SceneDef test = new()
        {
            Name = "Test",
            Entities = [
                new EntityDef()
                {
                    Position = new(178, 182),
                    Enabled = true,
                    Visible = true,
                    Components = [
                        new SpriteComponent {
                            TexturePath = "Images/UI/Multiplayer/DefaultProfileOld"
                        },
                    ]
                }
            ]
        };

        Register(test);

        // Main.Logger.Info(title);
        // Main.Logger.Info(test);
    }

    private SceneDef Add(string name)
    {
        var def = LoadFromFile(name);
        if(def is not null)
        {
            DefsByName.Add(name, def);
            Register(def);
        }
        return def;
    }

    public static SceneDef? LoadFromFile(string path)
    {
        return SceneDef.Deserialize(File.ReadAllText(Path.Combine("Content", "Levels", path + ".json")));
    }
}

public static class SceneExtensions
{
    public static string Serialize(this Scene scene, bool pretty = false)
    {
        var options = SceneRegistry.SerializerOptions;

        options.WriteIndented = pretty;

        var ret = JsonSerializer.Serialize((SceneDef)scene, options);

        return ret;
    }
}
