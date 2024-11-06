using System.Collections.Generic;
using Beebo.Mods;
using Jelly;

namespace Beebo;

public abstract class Mod
{
    internal ModLogger logger;

    public ModDependencyInfoList Dependencies { get; } = [];

    public ModLogger Logger => logger;

    public virtual void OnBeforeRun()
    {
        
    }

    public virtual void OnInitialize()
    {
        
    }

    public virtual void OnRegistriesInit()
    {
        
    }

    public virtual void OnLoadContent()
    {
        
    }

    public virtual void OnEndRun()
    {
        
    }
}
