using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly;
using Jelly.Components;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class SceneRegistry : Registry<SceneDef>
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
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
    };

    public static Dictionary<string, SceneDef> DefsByName { get; } = [];

    public override void Init()
    {
        var title = Add("Title");

        SceneDef test = new()
        {
            Name = "Test",
            Entities = [
                new Entity(new(0, 0))
                {
                    Enabled = true,
                    Visible = true,
                }
            ]
        };

        test.Entities[0].Add(new SpriteComponent());

        Main.Logger.Info(JsonSerializer.Serialize(title, SerializerOptions));
        Main.Logger.Info(JsonSerializer.Serialize(test, SerializerOptions));
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
        return JsonSerializer.Deserialize<SceneDef>(File.ReadAllText(Path.Combine("Content", "Levels", path + ".json")), SerializerOptions);
    }
}
