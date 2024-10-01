using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beebo.GameContent;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Suggestion;

using Jelly;
using Jelly.GameContent;

namespace Beebo.Commands;

public static class CommandManager
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
                            Log($"Usage:\n  {name} " + string.Join($"\n  {name} ", dispatcher.GetSmartUsage(dispatcher.GetRoot().GetChild(name), c.Source).Values));
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

        dispatcher.Register(l =>
            l.Literal("load")
                .Then(a =>
                    a.Argument("sceneId", Arguments.String())
                    // .Suggests((c, builder) => {
                    //     foreach(var def in Registries.Get<SceneRegistry>())
                    //     {
                    //         builder.Suggest(def.Key);
                    //     }
                    //     return builder.BuildFuture();
                    // })
                    .Executes(c => {
                        var sceneName = c.GetArgument<string>("sceneId");
                        var scene = Registries.Get<SceneRegistry>().GetDef(sceneName)?.Build();

                        if(scene is not null)
                            SceneManager.ChangeSceneImmediately(scene);
                        else
                            throw new NullReferenceException("The specified scene does not exist or could not be found.");

                        return 1;
                    })
                )
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
