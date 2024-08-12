using System;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class ComponentDef : ContentDef
{
    public Type ComponentType { get; private set; }

    private ComponentDef()
    {
        
    }

    public static ComponentDef Create<T>() where T : Component
    {
        return Create(typeof(T));
    }

    public static ComponentDef Create(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if(!type.IsSubclassOf(typeof(Component))) throw new InvalidCastException($"{type.Name} is not a subclass of Component");

        var def = new ComponentDef
        {
            ComponentType = type
        };

        return def;
    }
}
