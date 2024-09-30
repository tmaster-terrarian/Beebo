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
                    .Then(a => 
                        a.Literal("hi")
                        .Executes(c => {
                            Log("hello :)");
                            return 1;
                        })
                    )
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
                            Log($"Name: {name}\n\nUsage:\n  {name} " + string.Join($"\n  {name} ", dispatcher.GetSmartUsage(dispatcher.GetRoot().GetChild(name), c.Source).Values));
                            return 1;
                        })
                )
                .Executes(c => {
                    var task = dispatcher.GetCompletionSuggestions(dispatcher.Parse("", c.Source));

                    List<string> strings = [];
                    foreach(var item in task.Result.List)
                        strings.Add(item.Text);

                    Log("Available commands:\n  - " + string.Join("\n  - ", strings));
                    return 1;
                })
        );

        static int AAA(Brigadier.NET.Context.CommandContext<EntityCommandSource> c)
        {
            return 1;
        }

        dispatcher.Register(l =>
            l.Literal("a")
                .Then(a => a.Literal("1").Executes(AAA))
                .Then(a => a.Literal("2").Executes(AAA))
                .Then(a => a.Literal("3").Executes(AAA))
                .Then(a => a.Literal("4").Executes(AAA))
                .Then(a => a.Literal("5").Executes(AAA))
                .Then(a => a.Literal("6").Executes(AAA))
                .Then(a => a.Literal("7").Executes(AAA))
                .Then(a => a.Literal("8").Executes(AAA))
                .Then(a => a.Literal("9").Executes(AAA))
                .Then(a => a.Literal("10").Executes(AAA))
                .Then(a => a.Literal("11").Executes(AAA))
                .Then(a => a.Literal("12").Executes(AAA))
                .Then(a => a.Literal("13").Executes(AAA))
                .Then(a => a.Literal("14").Executes(AAA))
        );
    }

    private static void Log(string value)
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
        }
        catch(Exception e)
        {
            Logger.LogError(e);
            Chat.WriteChatMessage(e.Message, default, true, true);
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
