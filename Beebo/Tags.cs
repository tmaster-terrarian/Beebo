using System;

using Jelly;

namespace Beebo;

[Flags]
public enum EntityTags : uint
{
    None = 0,
    Player = 1
}

public static class TagExtensions
{
    public static void Add(this Tag tag, EntityTags entityTags) => tag.Add((uint)entityTags);

    public static void Remove(this Tag tag, EntityTags entityTags) => tag.Remove((uint)entityTags);

    public static void Add<T>(this Tag tag, T enumValue) where T : struct, Enum, IConvertible
        => tag.Add(enumValue.ToUInt32(null));

    public static void Remove<T>(this Tag tag, T enumValue) where T : struct, Enum, IConvertible
        => tag.Remove(enumValue.ToUInt32(null));
}
