using Jelly.GameContent;

namespace Beebo.GameContent;

public class SceneRegistry : Registry<SceneDef>
{
    public SceneDef Lobby { get; } = new SceneDef()
    {
        
    };

    public void Init()
    {
        Register(Lobby);
    }
}
