using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly.GameContent;

namespace Beebo.GameContent;

// [JsonSerializable(type: typeof(SceneDef))]
public class SceneDef : ContentDef
{
    // public static JsonSerializerOptions SerializeOptions { get; } = new()
    // {
    //     Converters =
    //     {
    //         new JsonStringEnumConverter()
    //     },
    //     DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    //     ReadCommentHandling = JsonCommentHandling.Skip,
    //     AllowTrailingCommas = true,
    // };

    // public static SceneDef? Load(string path)
    // {
    //     return JsonSerializer.Deserialize<SceneDef>(Path.Combine(Main.ProgramPath, "Content", "Scenes", path), SerializeOptions);
    // }
}
