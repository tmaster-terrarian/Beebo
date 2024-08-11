using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;

using Beebo.GameContent.Components;

using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public class ComponentRegistry : Registry<ComponentDef>
{
    public static HashSet<Type> RegisteredTypes { get; } = [];

    public static PolymorphicTypeResolver TypeResolver { get; } = new PolymorphicTypeResolver(typeof(Component));

    public override void Init()
    {
        AddType(typeof(TestComponent));
    }

    private void AddType(Type type)
    {
        var def = ComponentDef.Create(type);
        def.Name = type.Name;
        if(Register(def))
        {
            if(RegisteredTypes.Add(def.ComponentType))
            {
                TypeResolver.DerivedTypes.Add(def.ComponentType);
            }
        }
    }
}
