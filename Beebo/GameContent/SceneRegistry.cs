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
    public override void Init()
    {
        string path = Path.Combine(Main.ProgramPath, "Content", "Levels");
        foreach(var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
        {
            string fileName = file[(path.Length + 1)..^5];
            Add(fileName);
        }

        SceneDef test = new() {
            Name = "Test",
            Entities = [
                new JsonEntity {
                    Position = new(178, 182),
                    Enabled = true,
                    Visible = true,
                    Components = [
                        new SpriteComponent {
                            TexturePath = "Images/UI/Multiplayer/DefaultProfile"
                        },
                    ]
                }
            ]
        };

        Main.Logger.LogInfo($"Registered Scenes:\n  - {string.Join("\n  - ", this.Keys)}");

        Register(test);
    }

    private SceneDef Add(string name)
    {
        var def = LoadFromFile(name);
        if(def is not null)
        {
            Register(def);
        }
        return def;
    }

    public static SceneDef? LoadFromFile(string path)
    {
        return SceneDef.Deserialize(File.ReadAllText(Path.Combine("Content", "Levels", path + ".json")));
    }
}
