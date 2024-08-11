using System.Collections.Generic;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class SceneDef : ContentDef
{
    public IList<Entity>? Entities { get; set; }

    public Scene Build()
    {
        var scene = new Scene(Name.GetHashCode())
        {
            Name = Name,
        };

        foreach(var e in Entities ?? [])
        {
            scene.Entities.Add(e);
        }

        return scene;
    }
}
