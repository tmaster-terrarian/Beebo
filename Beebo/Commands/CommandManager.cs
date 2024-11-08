using System;
using System.Collections.Generic;

using Beebo.GameContent;

using Brigadier.NET;
using Brigadier.NET.Suggestion;

using Jelly;

namespace Beebo.Commands;

public static class CommandManager
{
    private static readonly CommandDispatcher<EntityCommandSource> dispatcher = new();

    private static readonly Dictionary<string, ParseResults<EntityCommandSource>> cachedCommands = [];

    public static CommandDispatcher<EntityCommandSource> Dispatcher => dispatcher;

    public static Suggestions Suggestions { get; private set; }

    public static Logger Logger { get; } = new("Brigadier");

    public static void Initialize()
    {
        foreach(var item in RegistryManager.CommandRegistry)
        {
            Dispatcher.Register(item.Value.Command);
        }
    }

    internal static void Log(string value)
    {
        Logger.LogInfo(value);

        foreach(var line in value.Split('\n'))
            Chat.WriteChatMessage(line, default, true, true);
    }

    public static void ExecuteCommand(string command, EntityCommandSource source)
    {
        Logger.LogInfo($"Executing command as [{source?.Entity?.EntityID.ToString() ?? "@"}] > \"{command}\"");

        var parseResults = dispatcher.Parse(command, source);
        cachedCommands.TryAdd(command, parseResults);

        try
        {
            dispatcher.Execute(parseResults);

            Suggestions = null;
        }
        catch(Exception e)
        {
            Logger.LogError(e);
            Chat.WriteChatMessage(e.Message, default, true, true);
        }
    }

    internal static async void GetSuggestions(string command, EntityCommandSource source, int cursor = -1)
    {
        Suggestions = cursor switch {
            > -1 => await dispatcher.GetCompletionSuggestions(dispatcher.Parse(command, source), cursor),
            _ =>    await dispatcher.GetCompletionSuggestions(dispatcher.Parse(command, source), command.Length),
        };
    }
}

public class EntityCommandSource(Entity? entity)
{
    public static EntityCommandSource Default => new(null);

    private long? id = entity?.EntityID;

    public Entity? Entity => id != null
        ? SceneManager.ActiveScene.Entities.FindByID(id!.Value)
        : null;
}
