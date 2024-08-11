using System;
using System.Text.Json.Serialization;

using Jelly.GameContent;

namespace Beebo.GameContent;

public class ComponentDef : ContentDef
{
    [JsonIgnore] public Type ComponentType { get; private set; }

    private ComponentDef()
    {
        
    }

    public static ComponentDef Create<T>() where T : Jelly.Component
    {
        var def = new ComponentDef
        {
            ComponentType = typeof(T)
        };

        // PropertyDescriptorCollection properties;

        // AssociatedMetadataTypeTypeDescriptionProvider typeDescriptionProvider;

        // properties = TypeDescriptor.GetProperties(typeof(Jelly.Component));
        // Console.WriteLine(properties[0].Attributes.Count);

        // typeDescriptionProvider = new AssociatedMetadataTypeTypeDescriptionProvider(
        //     typeof(Jelly.Component),
        //     typeof(T));

        // TypeDescriptor.AddProviderTransparent(typeDescriptionProvider, typeof(Jelly.Component));

        // properties = TypeDescriptor.GetProperties(typeof(Jelly.Component));
        // Console.WriteLine(properties[0].Attributes.Count);

        return def;
    }

    public static ComponentDef Create(Type type)
    {
        var def = new ComponentDef
        {
            ComponentType = type
        };

        // PropertyDescriptorCollection properties;

        // AssociatedMetadataTypeTypeDescriptionProvider typeDescriptionProvider;

        // properties = TypeDescriptor.GetProperties(typeof(Jelly.Component));
        // Console.WriteLine(properties[0].Attributes.Count);

        // typeDescriptionProvider = new AssociatedMetadataTypeTypeDescriptionProvider(
        //     typeof(Jelly.Component),
        //     typeof(T));

        // TypeDescriptor.AddProviderTransparent(typeDescriptionProvider, typeof(Jelly.Component));

        // properties = TypeDescriptor.GetProperties(typeof(Jelly.Component));
        // Console.WriteLine(properties[0].Attributes.Count);

        return def;
    }
}
