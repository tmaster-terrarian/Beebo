using System;
using System.Collections.Generic;

using Beebo.Commands;

using Brigadier.NET;
using Brigadier.NET.Builder;

using Jelly;
using Jelly.GameContent;

using static Beebo.Commands.CommandManager;

namespace Beebo.GameContent;

public class CommandDef : RegistryEntry
{
    public LiteralArgumentBuilder<EntityCommandSource> Command { get; }

    public CommandDef(LiteralArgumentBuilder<EntityCommandSource> command)
    {
        Command = command;
        Name = command.Literal;
    }

    public CommandDef(Func<IArgumentContext<EntityCommandSource>, LiteralArgumentBuilder<EntityCommandSource>> command)
        : this(command(default(ArgumentContext<EntityCommandSource>))) {}

    public static implicit operator CommandDef(Func<IArgumentContext<EntityCommandSource>, LiteralArgumentBuilder<EntityCommandSource>> command)
        => new(command);

    public static implicit operator CommandDef(LiteralArgumentBuilder<EntityCommandSource> command)
        => new(command);
}

public class CommandRegistry : Registry<CommandDef>
{
    public override void Init()
    {
        Register(l => l.Literal("foo")
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

        Register(l => l.Literal("help")
            .Then(a =>
                a.Argument("command", Arguments.Word())
                    .Executes(c => {
                        var name = Arguments.GetString(c, "command");
                        Log($"Usage:\n  {name} " + string.Join($"\n  {name} ", Dispatcher.GetSmartUsage(Dispatcher.GetRoot().GetChild(name), c.Source).Values));
                        return 1;
                    })
            )
            .Executes(c => {
                var task = Dispatcher.GetCompletionSuggestions(Dispatcher.Parse("", c.Source));

                List<string> strings = [];
                foreach(var item in task.Result.List)
                    strings.Add(item.Text);

                Log("Available commands:\n  - " + string.Join("\n  - ", strings));
                return 1;
            })
        );

        Register(l => l.Literal("load")
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
                    var scene = RegistryManager.SceneRegistry.GetDef(sceneName)?.Build();

                    if(scene is not null)
                        SceneManager.ChangeSceneImmediately(scene);
                    else
                        throw new NullReferenceException("The specified scene does not exist or could not be found.");

                    return 1;
                })
            )
        );
    }

    // sometimes C# is so silly
    public bool Register(Func<IArgumentContext<EntityCommandSource>, LiteralArgumentBuilder<EntityCommandSource>> value)
        => base.Register(value);

    public bool Register(Func<IArgumentContext<EntityCommandSource>, LiteralArgumentBuilder<EntityCommandSource>> value, string key)
        => base.Register(value, key);
}
