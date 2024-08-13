using System;

using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class ComponentTypeDef : ContentDef
{
    public Type ComponentType { get; set; }

    internal ComponentTypeDef()
    {
        
    }

    public static ComponentTypeDef Create<T>() where T : Component
    {
        return Create(typeof(T));
    }

    public static ComponentTypeDef Create(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if(!type.IsSubclassOf(typeof(Component))) throw new InvalidCastException($"{type.Name} is not a subclass of Component");

        var def = new ComponentTypeDef
        {
            ComponentType = type
        };

        return def;
    }
}
