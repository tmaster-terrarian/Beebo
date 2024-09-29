using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Beebo.Net;

using Jelly;
using Jelly.Graphics;

using Steamworks;
using Beebo.Graphics;
using System.Text.RegularExpressions;

namespace Beebo;

public static partial class Chat
{
    private static float chatAlpha;

    private static int currentSuggestion;

    public static bool WindowOpen { get; private set; } = false;
    public static string TextInput { get; private set; } = "";
    public static int Cursor { get; private set; } = 0;
    public static int Scroll { get; private set; } = 0;

    public static List<Tuple<string, Color>> History { get; } = [];

    public static void CancelTypingAndClose()
    {
        WindowOpen = false;
        TextInput = "";
        Cursor = 0;
        Scroll = 0;
    }

    public static void Update()
    {
        if(WindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            int c = TextInput.Length;

            TextInput =
                (Cursor > 0 ? TextInput[..Cursor] : "") +
                string.Join(null, input) +
                (Cursor < c ? TextInput[Cursor..] : "");

            TextInput = TextInput[..MathHelper.Min(TextInput.Length, 10240)];

            if(backspace && TextInput.Length > 0)
            {
                TextInput = TextInput[..^1];
            }

            Cursor += input.Count;

            if(Input.GetPressed(Keys.Left)) Cursor--;
            if(Input.GetPressed(Keys.Right)) Cursor++;

            Cursor = MathHelper.Clamp(Cursor, 0, TextInput.Length);

            if(TextInput.StartsWith('/'))
            {
                if(Input.GetPressed(Keys.Up)) currentSuggestion++;
                if(Input.GetPressed(Keys.Down)) currentSuggestion--;

                var text = TextInput.Length > 1 ? TextInput[1..] : "";
                if(TextInput.Length != c)
                {
                    currentSuggestion = 0;
                    Commands.GetSuggestions(text, EntityCommandSource.Default, Cursor - 1);
                }

                currentSuggestion = MathHelper.Clamp(currentSuggestion, 0, (Commands.Suggestions?.List.Count - 1) ?? 0);

                if((Commands.Suggestions?.List.Count ?? 0) > 0)
                {
                    var completion = Commands.Suggestions.List[currentSuggestion].Text;

                    if(Input.GetPressed(Keys.Tab) && !text[..(Cursor - 1)].EndsWith(completion))
                    {
                        TextInput =
                            (Cursor > 0 ? TextInput[..Cursor] : "") +
                            completion[Commands.Suggestions.Range.End..] +
                            (Cursor < c ? TextInput[Cursor..] : "");

                        Cursor = MathHelper.Clamp(Cursor + completion.Length, 0, TextInput.Length);

                        Commands.GetSuggestions(TextInput[1..], EntityCommandSource.Default, Cursor - 1);
                    }
                }
            }
            else
                currentSuggestion = 0;

            Scroll += Input.GetScrollDelta();
            Scroll = MathHelper.Clamp(Scroll, 0, MathHelper.Max(0, History.Count - 5));
        }

        if(Input.GetPressed(Keys.Enter) && !BeeboImGuiRenderer.Enabled)
        {
            WindowOpen = !WindowOpen;
            if(!WindowOpen && TextInput.Length > 0)
            {
                string message = TextInput[..MathHelper.Min(TextInput.Length, 10240)];

                WriteChatMessage(message, (!SteamManager.IsSteamRunning) ? CSteamID.Nil : P2PManager.MyID, false);

                if(TextInput.StartsWith('/'))
                {
                    Commands.ExecuteCommand(TextInput[1..], EntityCommandSource.Default);
                }

                TextInput = "";
                Scroll = 0;
            }

            Cursor = 0;
        }

        if(!WindowOpen)
        {
            if(Input.GetPressed(Keys.OemQuestion))
            {
                WindowOpen = true;
                TextInput += "/";
                Cursor++;
            }
        }
    }

    public static void DrawUI()
    {
        if(WindowOpen || chatAlpha > 0)
        {
            const int spaceWidth = 6;
            const int chatWidth = 256;
            const int lineHeight = 12;
            Point chatPos = new(2, Renderer.ScreenSize.Y - 16);

            float alpha = WindowOpen ? 1 : chatAlpha;

            if(History.Count > 0)
            {
                Renderer.SpriteBatch.Draw(
                    Renderer.PixelTexture,
                    new Rectangle(
                        chatPos.X,
                        chatPos.Y - lineHeight * MathHelper.Min(5, History.Count) - 1,
                        chatWidth,
                        lineHeight * MathHelper.Min(5, History.Count)
                    ),
                    Color.Black * 0.5f * alpha
                );
            }

            if(WindowOpen)
            {
                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(chatPos.X, chatPos.Y, chatWidth, lineHeight), Color.Black * 0.67f);

                float x = chatWidth - 1 - MathHelper.Max(
                    chatWidth - 1,
                    Main.RegularFont.MeasureString(TextInput).X
                );

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    Main.RegularFont,
                    TextInput,
                    new Vector2(x + chatPos.X + 1, chatPos.Y - 1),
                    Color.White,
                    spaceWidth
                );

                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle((int)x + chatPos.X + (int)Main.RegularFont.MeasureString(TextInput[..Cursor]).X, chatPos.Y + 1, 1, 10), Color.LightGray);
            }

            for(int i = 0; i < 5; i++)
            {
                int index = History.Count - 1 - i - Scroll;
                if(index < 0) continue;

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    Main.RegularFont,
                    History[index].Item1,
                    new Vector2(chatPos.X + 1, chatPos.Y - (lineHeight + 2) - (i * lineHeight)),
                    History[index].Item2 * alpha,
                    spaceWidth
                );
            }

            if(WindowOpen && TextInput.StartsWith('/'))
            {
                if((Commands.Suggestions?.List.Count ?? 0) > 0)
                {
                    Renderer.SpriteBatch.Draw(
                        Renderer.PixelTexture,
                        new Rectangle(
                            chatPos.X + 5,
                            chatPos.Y - lineHeight * Commands.Suggestions.List.Count,
                            chatWidth - 5,
                            lineHeight * Commands.Suggestions.List.Count
                        ),
                        Color.Black * 0.75f
                    );

                    float x = chatWidth - 1 - MathHelper.Max(
                        chatWidth - 1,
                        Main.RegularFont.MeasureString(TextInput).X
                    );

                    var completion = Commands.Suggestions?.List[currentSuggestion].Text;
                    if(completion is not null)
                    {
                        Renderer.SpriteBatch.DrawStringSpacesFix(
                            Main.RegularFont,
                            completion[Commands.Suggestions.Range.End..],
                            new Vector2(x + chatPos.X + 1 + Main.RegularFont.MeasureString(TextInput).X, chatPos.Y - 1),
                            Color.DarkGray,
                            spaceWidth
                        );
                    }

                    for(int i = 0; i < Commands.Suggestions.List.Count; i++)
                    {
                        Renderer.SpriteBatch.DrawStringSpacesFix(
                            Main.RegularFont,
                            Commands.Suggestions.List[i].Text,
                            new Vector2(chatPos.X + 6, chatPos.Y - (lineHeight + 1) - (i * lineHeight)),
                            i == currentSuggestion ? Color.Yellow : Color.White,
                            spaceWidth
                        );

                        // if(i == currentSuggestion)
                        // {
                        //     Renderer.SpriteBatch.DrawStringSpacesFix(
                        //         Main.RegularFont,
                        //         Commands.Suggestions.List[i].Text[..Commands.Suggestions.Range.End],
                        //         new Vector2(chatPos.X + 6, chatPos.Y - (lineHeight + 1) - (i * lineHeight)),
                        //         Color.Yellow,
                        //         spaceWidth
                        //     );
                        // }
                    }
                }
            }
        }
    }

    public static void WriteChatMessage(string message, CSteamID origin, bool system = false, bool noLog = false)
    {
        if(system)
        {
            if(!noLog)
                Main.Logger.LogInfo("<Server>: " + message);

            History.Add(new(message, Color.Yellow));
        }
        else
        {
            string name = "???";
            if(SteamManager.IsSteamRunning && origin != CSteamID.Nil)
            {
                name = SteamFriends.GetFriendPersonaName(origin);
            }

            if(!noLog)
                Main.Logger.LogInfo(name + ": " + message);

            History.Add(new($"{name}: {message}", Color.White));
        }

        if(Main.GlobalCoroutineRunner.IsRunning(nameof(ChatDisappearDelay)))
            Main.GlobalCoroutineRunner.Stop(nameof(ChatDisappearDelay));
        Main.GlobalCoroutineRunner.Run(nameof(ChatDisappearDelay), ChatDisappearDelay());
    }

    private static IEnumerator ChatDisappearDelay(float holdTime = 5f, float fadeTime = 1f)
    {
        chatAlpha = 1;

        yield return holdTime;

        while(chatAlpha > 0)
        {
            float interval = (float)Main.Instance.TargetElapsedTime.TotalSeconds;
            chatAlpha -= interval / fadeTime;
            yield return null;
        }

        if(!WindowOpen)
        {
            Scroll = 0;
        }
    }
}
