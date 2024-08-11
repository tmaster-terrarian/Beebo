using Jelly;

namespace Beebo.GameContent.Components;

public class TestComponent : Component
{
    public string TestMessage { get; set; } = "Hello World!";
}

public class TestComponent2 : Component
{
    public int Num { get; set; }
}
