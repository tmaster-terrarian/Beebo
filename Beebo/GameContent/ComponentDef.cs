using Jelly;
using Jelly.GameContent;

namespace Beebo.GameContent;

public class ComponentDef<T> : ContentDef, IComponentDef where T : Component
{
    public delegate T CreateComponentDelegate();

    public CreateComponentDelegate CreateComponent { get; set; }
}

public interface IComponentDef;
