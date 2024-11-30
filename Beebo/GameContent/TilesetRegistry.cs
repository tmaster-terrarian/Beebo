using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class TilesetRegistry : Registry<TilesetDef>
{
    private static readonly List<string> ids = [];

    private static readonly List<string> _internalNames = [
        "default"
    ];

    public new bool Register(TilesetDef value)
    {
        if(_internalNames.Contains(value.Name)) return base.Register(value);

        if(ids.Contains(value.Name))
            throw new InvalidOperationException($"A tileset by the name {value.Name} is already registered!");

        ids.Add(value.Name);

        return base.Register(value);
    }

    public new bool Register(TilesetDef value, string name)
    {
        if(value.Name == name && _internalNames.Contains(value.Name)) return base.Register(value);

        if(ids.Contains(name))
            throw new InvalidOperationException($"A tileset by the name {name} is already registered!");

        ids.Add(name);

        value.Name = name;

        return base.Register(value, name);
    }

    public override void Init()
    {
        Register(new() {
            Name = "undefined",
        });

        foreach(var fullPath in Directory.EnumerateFiles(Path.Combine(FileLocations.DataPath, "tilesets"), "*.json", SearchOption.TopDirectoryOnly))
        {
            var def = JsonSerializer.Deserialize<TilesetDef>(File.OpenRead(fullPath), RegistryManager.SerializerOptions);
            Register(def);
        }

        Main.Logger.LogInfo($"Registered Tilesets:\n  - {string.Join("\n  - ", this.Keys)}");
    }

    public static TilesetDef GetDefStatic(int tileId)
    {
        return GetDefStatic(GetNameFromID(tileId));
    }

    public static int GetIDFromName(string name)
    {
        return ids.IndexOf(name);
    }

    public static string GetNameFromID(int id)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, ids.Count, nameof(id));
        return ids[id];
    }
}

public class TilesetRule : IEquatable<TilesetRule>
{
    public int U { get; set; }

    public int V { get; set; }

    public string[] Pattern { get; set; } = [];

    public bool Equals(TilesetRule other)
    {
        return ReferenceEquals(this, other) || other.Pattern == Pattern;
    }

    public static bool operator ==(TilesetRule left, TilesetRule right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TilesetRule left, TilesetRule right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj) => obj is TilesetRule tilesetRule && Equals(tilesetRule);
}

public class TilesetDef : RegistryEntry
{
    public IList<string>? Ignores { get; set; } = [];

    public string? CopyFrom { get; set; }

    public IList<TilesetRule>? Rules { get; set; } = [];
}
