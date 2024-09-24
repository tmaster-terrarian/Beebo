using Jelly;

namespace Beebo;

public enum EntityTags : uint
{
    None = 0,
    Player = 1
}

public static class TagExtensions
{
    public static void Add(this Tag tag, EntityTags entityTags) => tag.Add((uint)entityTags);

    public static void Remove(this Tag tag, EntityTags entityTags) => tag.Remove((uint)entityTags);
}
