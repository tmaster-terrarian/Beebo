using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Beebo.Mods;

public class ModJson
{
    public string Guid { get; set; }

    public string Name { get; set; }

    public string Version { get; set; }

    public ModJson_Dependencies? Dependencies { get; set; }

    public class ModJson_Dependencies
    {
        public IList<ModDependency>? Required { get; set; }

        public IList<ModDependency>? Optional { get; set; }

        public IList<ModDependency>? Incompatible { get; set; }
    }
}
