using System.Collections.Generic;
using Beebo.Mods;
using Jelly;

namespace Beebo;

public abstract class Mod
{
    internal Logger logger;

    public ModDependencyInfoList Dependencies { get; } = [];

    public Logger Logger => logger;

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
