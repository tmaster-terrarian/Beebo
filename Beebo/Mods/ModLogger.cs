using Jelly;

namespace Beebo.Mods;

// wrapper to avoid needing to reference jelly in a mod just to log things
public class ModLogger(string name)
{
    private readonly Logger logger = new(name);

    public string Name { get; } = name;

    public void Log(Logger.MessageType type, object? message) => logger.Log(type, message);

    public void LogInfo(object? message) => logger.Log(Logger.MessageType.INFO, message);

    public void LogError(object? message) => logger.Log(Logger.MessageType.ERROR, message);

    public void LogWarning(object? message) => logger.Log(Logger.MessageType.WARN, message);
}
