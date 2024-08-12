using Jelly;

namespace Beebo.GameContent.Components;

public class TestComponent : Component
{
    public string TestMessage { get; set; } = "Hello World!";
}

public class TestComponent2 : TestComponent
{
    public int Num { get; set; }
}
