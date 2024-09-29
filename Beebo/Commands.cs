using System;
using System.Collections.Generic;

using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Suggestion;

using Jelly;

namespace Beebo;

public static class Commands
{
    private static readonly CommandDispatcher<EntityCommandSource> dispatcher = new();

    private static readonly Dictionary<string, ParseResults<EntityCommandSource>> cachedCommands = [];

    internal static CommandDispatcher<EntityCommandSource> Dispatcher => dispatcher;

    public static Suggestions Suggestions { get; private set; }

    public static Logger Logger { get; } = new("Brigadier");

    public static void Initialize()
    {
        dispatcher.Register(l =>
            l.Literal("foo")
                .Then(a =>
                    a.Argument("bar", Arguments.Integer())
                        .Executes(c => {
                            Log("Bar is " + Arguments.GetInteger(c, "bar"));
                            return 1;
                        })
                )
                .Executes(c => {
                    Log("Called foo with no arguments");
                    return 1;
                })
        );

        dispatcher.Register(l =>
            l.Literal("help")
                .Then(a =>
                    a.Argument("command", Arguments.Word())
                        .Executes(c => {
                            var name = Arguments.GetString(c, "command");
                            Log($"Name: {name}\n\nUsage:\n  {name} " + string.Join($"\n  {name} ", dispatcher.GetAllUsage(dispatcher.GetRoot().GetChild(name), c.Source, false)));
                            return 1;
                        })
                )
                .Executes(c => {
                    Log("Available commands:\n  - " + string.Join("\n  - ", dispatcher.GetAllUsage(dispatcher.GetRoot(), c.Source, false)));
                    return 1;
                })
        );
    }

    private static void Log(string value)
    {
        Logger.LogInfo(value);
        Chat.WriteChatMessage(value, default, true, true);
    }

    public static void ExecuteCommand(string command, EntityCommandSource source)
    {
        Logger.LogInfo($"Executing command as [{source?.Entity?.EntityID.ToString() ?? "@"}] > \"{command}\"");

        var parseResults = dispatcher.Parse(command, source);
        cachedCommands.TryAdd(command, parseResults);

        try
        {
            dispatcher.Execute(parseResults);
        }
        catch(Exception e)
        {
            Logger.LogError(e);
        }
    }

    internal static async void GetSuggestions(string command, EntityCommandSource source, int cursor = -1)
    {
        Suggestions = await (cursor switch {
            > -1 => dispatcher.GetCompletionSuggestions(dispatcher.Parse(command, source), cursor),
            _ =>    dispatcher.GetCompletionSuggestions(dispatcher.Parse(command, source), command.Length),
        });
    }
}

public class EntityCommandSource(Entity? entity)
{
    public static EntityCommandSource Default => new(null);

    public Entity? Entity { get; } = entity;
}
