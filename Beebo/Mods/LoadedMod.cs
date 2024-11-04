using System;
using System.Reflection;

namespace Beebo.Mods;

public class LoadedMod(Assembly assembly, string path, Type entry)
{
    public Assembly Assembly => assembly;

    public string BasePath => path;

    public Type Entry => entry;

    public Mod Instance { get; internal set; }
}
