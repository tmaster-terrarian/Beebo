using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Beebo.Mods;

public sealed class ModDependencyInfo(string guid, bool available)
{
    public string Guid => guid;
    public bool Available => available;
}

public class ModDependencyInfoList : ICollection<ModDependencyInfo>, IEnumerable<ModDependencyInfo>, IEnumerable
{
    int ICollection<ModDependencyInfo>.Count => dict.Count;
    bool ICollection<ModDependencyInfo>.IsReadOnly => true;

    internal readonly Dictionary<string, ModDependencyInfo> dict = [];

    public bool Contains(string guid)
    {
        return dict.ContainsKey(guid);
    }

    public bool IsAvailable(string guid)
    {
        return dict.TryGetValue(guid, out var value) && value.Available;
    }

    public void CopyTo(ModDependencyInfo[] array, int arrayIndex)
    {
        dict.Values.CopyTo(array, arrayIndex);
    }

    internal void Add(ModDependencyInfo item)
    {
        dict.Add(item.Guid, item);
    }

    bool ICollection<ModDependencyInfo>.Contains(ModDependencyInfo item)
    {
        return dict.ContainsValue(item);
    }

    void ICollection<ModDependencyInfo>.Clear()
    {
        dict.Clear();
    }

    void ICollection<ModDependencyInfo>.Add(ModDependencyInfo item)
    {
        Add(item);
    }

    bool ICollection<ModDependencyInfo>.Remove(ModDependencyInfo item)
    {
        return dict.Remove(item.Guid);
    }

    public IEnumerator<ModDependencyInfo> GetEnumerator()
    {
        return dict.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
