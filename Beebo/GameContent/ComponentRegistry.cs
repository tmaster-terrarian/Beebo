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
        // AddType(typeof(TestComponent));
        // AddType(typeof(TestComponent2));

        var assembly = Assembly.GetAssembly(GetType());

        if(assembly is null) return;

        foreach(var type in assembly.DefinedTypes)
        {
            if(type.IsClass && type.IsSubclassOf(typeof(Component)))
            {
                AddType(type);
            }
        }

        Main.Logger.Info($"Registered Components: {TypesToString(RegisteredTypes)}");
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

    private static string TypesToString(IEnumerable<Type> types, bool fullNames = false)
    {
        ArgumentNullException.ThrowIfNull(types);

        List<Type> _types = [.. types];

        string str = "";
        for(int i = 0; i < _types.Count; i++)
        {
            Type type = _types[i];
            str += fullNames ? type.FullName : type.Name;

            if(i < _types.Count - 1)
            {
                str += ", ";
            }
        }

        return str;
    }
}
