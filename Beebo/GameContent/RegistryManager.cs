using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly.GameContent;
using Jelly.Serialization;

namespace Beebo.GameContent;

public static class RegistryManager
{
    public static JsonSerializerOptions SerializerOptions => new() {
        Converters = {
            new JsonStringEnumConverter(),
            new JsonPointConverter(),
            new JsonVector2Converter(),
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = ComponentTypeRegistry.ComponentTypeResolver,
    };

    public static void Initialize()
    {
        if(Assembly.GetExecutingAssembly() is Assembly assembly)
            ComponentTypeRegistry.ComponentTypeResolver.GetAllDerivedTypesFromAssembly(assembly);

        Registries.Add(new ComponentTypeRegistry());
        Registries.Add(new EntityRegistry());
        Registries.Add(new SceneRegistry());
        Registries.Add(new AudioRegistry());

        RegistriesInit();
    }

    static void RegistriesInit()
    {
        
    }
}
