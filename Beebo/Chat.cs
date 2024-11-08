using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Beebo.Commands;
using Beebo.Graphics;
using Beebo.Net;

using Jelly;
using Jelly.Graphics;

using Steamworks;

namespace Beebo;

public static partial class Chat
{
    private static float chatAlpha;

    private static int currentSuggestion;
    private static int suggestionScroll;
    private static int suggestionHeight = 10;

    private static readonly List<Keys> _arrowKeyInput = [];
    private const double RepeatDelay = 0.45;
    private const double RepeatRate = 0.01667;
    private static readonly double[] _arrowKeyDelays = new double[4];
    private static Keys[] _lastPressedKeys = [];

    private static int selectionStart = 0;

    private static Range SelectionRange => Math.Min(selectionStart, Cursor)..Math.Max(selectionStart, Cursor);
    private static bool IsSelecting => selectionStart != Cursor;

    public static bool WindowOpen { get; private set; } = false;
    public static string TextInput { get; private set; } = "";
    public static int Cursor { get; private set; } = 0;
    public static int Scroll { get; private set; } = 0;
    public static int WindowHeight { get; set; } = 6;

    public static List<TextComponent> History { get; } = [];

    private static int HistoryLineCount {
        get {
            int l = 0;
            foreach(var txt in History)
            {
                l += (int)(txt.CalculateHeight() / txt.LineSpacing);
            }
            return l;
        }
    }

    private static List<string> HistoryString {
        get => [
            ..from text in History
            select text.Text
        ];
    }

    private static int HistoryHeight {
        get {
            float l = 0;
            foreach(var txt in History)
            {
                l += txt.CalculateHeight();
            }
            return (int)l;
        }
    }

    public static void CancelTypingAndClose()
    {
        WindowOpen = false;
        TextInput = "";
        Cursor = 0;
        selectionStart = Cursor;
        Scroll = 0;
        currentSuggestion = 0;
        suggestionScroll = 0;
    }

    public static void Update(GameTime gameTime)
    {
        if(WindowOpen)
        {
            UpdateArrowKeyInput(gameTime);

            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            int len = TextInput.Length;

            if(!IsSelecting)
            {
                TextInput =
                    (Cursor > 0 ? TextInput[..Cursor] : "") +
                    string.Join(null, input) +
                    (Cursor < len ? TextInput[Cursor..] : "");
                Cursor += input.Count;
                selectionStart = Cursor;
            }
            else if(input.Count > 0)
            {
                TextInput =
                    TextInput[..SelectionRange.Start] +
                    string.Join(null, input) +
                    TextInput[SelectionRange.End..];
                Cursor = MathHelper.Clamp(SelectionRange.Start.Value + 1, 0, TextInput.Length);
                selectionStart = Cursor;
            }

            TextInput = TextInput[..MathHelper.Min(TextInput.Length, 10240)];

            if(backspace && TextInput.Length > 0 && (Cursor > 0 || selectionStart > 0))
            {
                if(IsSelecting)
                {
                    TextInput = TextInput[..SelectionRange.Start] + TextInput[SelectionRange.End..];
                    Cursor = SelectionRange.Start.Value;
                    selectionStart = Cursor;
                }
                else
                {
                    TextInput = TextInput[..(Cursor - 1)] + TextInput[Cursor..];
                    Cursor--;
                    selectionStart = Cursor;
                }
            }

            if(_arrowKeyInput.Contains(Keys.Left))
            {
                bool s = IsSelecting;
                if(!(s && !Input.GetDown(Keys.LeftShift)))
                    Cursor--;

                if(s && !Input.GetDown(Keys.LeftShift))
                    Cursor = MathHelper.Clamp(SelectionRange.Start.Value, 0, TextInput.Length);

                if(!Input.GetDown(Keys.LeftShift))
                    selectionStart = MathHelper.Clamp(Cursor, 0, TextInput.Length);
            }
            if(_arrowKeyInput.Contains(Keys.Right))
            {
                bool s = IsSelecting;
                if(!(s && !Input.GetDown(Keys.LeftShift)))
                    Cursor++;
                if(s && !Input.GetDown(Keys.LeftShift))
                    Cursor = MathHelper.Clamp(SelectionRange.End.Value, 0, TextInput.Length);

                if(!Input.GetDown(Keys.LeftShift))
                    selectionStart = MathHelper.Clamp(Cursor, 0, TextInput.Length);
            }

            Cursor = MathHelper.Clamp(Cursor, 0, TextInput.Length);
            selectionStart = MathHelper.Clamp(selectionStart, 0, TextInput.Length);

            if(TextInput.StartsWith('/') && Cursor > 0)
            {
                string text = TextInput.Length > 1 ? TextInput[1..] : "";
                if(TextInput.Length != len)
                {
                    currentSuggestion = 0;
                    CommandManager.GetSuggestions(text, EntityCommandSource.Default, Cursor - 1);
                }

                if((CommandManager.Suggestions?.List.Count ?? 0) > 0)
                {
                    if(_arrowKeyInput.Contains(Keys.Up)) currentSuggestion++;
                    if(_arrowKeyInput.Contains(Keys.Down)) currentSuggestion--;

                    currentSuggestion = MathHelper.Clamp(currentSuggestion, 0, CommandManager.Suggestions.List.Count - 1);

                    if(currentSuggestion - suggestionScroll >= suggestionHeight) suggestionScroll++;
                    if(currentSuggestion - suggestionScroll < 0) suggestionScroll--;

                    string completion = CommandManager.Suggestions.List[currentSuggestion].Text;

                    if(Input.GetPressed(Keys.Tab) && !text[..(Cursor - 1)].EndsWith(completion))
                    {
                        int min = MathHelper.Min(text.Length, Cursor + CommandManager.Suggestions.Range.Length);

                        TextInput = "/" +
                            (min > 0 ? text[..min] : "") +
                            completion[MathHelper.Min(completion.Length - 1, CommandManager.Suggestions.Range.Length)..] +
                            (min < len ? text[min..] : "");

                        Cursor = MathHelper.Clamp(Cursor + completion.Length, 0, TextInput.Length);
                        selectionStart = Cursor;

                        currentSuggestion = 0;

                        CommandManager.GetSuggestions(TextInput[1..], EntityCommandSource.Default, Cursor - 1);
                    }
                }
            }
            else
            {
                currentSuggestion = 0;
                suggestionScroll = 0;
            }

            if(TextInput.Length != len && !Input.GetDown(Keys.LeftShift))
                selectionStart = Cursor;

            Scroll += Input.GetScrollDelta();
            Scroll = MathHelper.Clamp(Scroll, 0, MathHelper.Max(0, HistoryLineCount - WindowHeight));
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
                    CommandManager.ExecuteCommand(TextInput[1..], EntityCommandSource.Default);
                }

                TextInput = "";
                Scroll = 0;
            }

            Cursor = 0;
            selectionStart = 0;
        }

        if(!WindowOpen && !BeeboImGuiRenderer.Enabled)
        {
            if(Input.GetPressed(Keys.OemQuestion))
            {
                WindowOpen = true;
                TextInput += "/";
                Cursor++;
                selectionStart++;
            }
        }
    }

    public static void DrawUI()
    {
        if(WindowOpen || chatAlpha > 0)
        {
            const int spaceWidth = 6;
            const int lineHeight = 12;

            int chatWidth = (Renderer.ScreenSize.X / 2) - 1;
            Point chatPos = new(2, Renderer.ScreenSize.Y - 16);

            float alpha = WindowOpen ? 1 : chatAlpha;

            if(History.Count > 0)
            {
                Renderer.SpriteBatch.Draw(
                    Renderer.PixelTexture,
                    new Rectangle(
                        chatPos.X,
                        chatPos.Y - MathHelper.Min(lineHeight * WindowHeight, HistoryHeight) - 1,
                        chatWidth,
                        MathHelper.Min(lineHeight * WindowHeight, HistoryHeight)
                    ),
                    Color.Black * 0.5f * alpha
                );
            }

            var font = Fonts.RegularFont;

            if(WindowOpen)
            {
                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(chatPos.X, chatPos.Y, chatWidth, lineHeight), Color.Black * 0.67f);

                float x = chatWidth - 2 - MathHelper.Max(
                    chatWidth - 2,
                    font.MeasureString(TextInput).X
                );

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    font,
                    TextInput,
                    new Vector2(x + chatPos.X + 2, chatPos.Y - 1),
                    Color.White,
                    spaceWidth
                );

                if(IsSelecting)
                    Renderer.SpriteBatch.Draw(
                        Renderer.PixelTexture,
                        new Rectangle(
                            (int)x + chatPos.X + (int)font.MeasureString(TextInput[..SelectionRange.Start]).X + 1,
                            chatPos.Y + 1,
                            (int)font.MeasureString(TextInput[SelectionRange]).X + 1,
                            10
                        ),
                        Color.White * 0.25f
                    );
                else
                    Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle((int)x + chatPos.X + (int)font.MeasureString(TextInput[..Cursor]).X + 1, chatPos.Y + 1, 1, 10), Color.LightGray);

                if(HistoryHeight > WindowHeight * lineHeight)
                {
                    int barHeight = (int)((float)(WindowHeight * lineHeight) / HistoryHeight * (WindowHeight * lineHeight - 2));
                    Renderer.SpriteBatch.Draw(
                        Renderer.PixelTexture,
                        new Rectangle(
                            chatPos.X + chatWidth - 2,
                            chatPos.Y - 2 - (Scroll * lineHeight / (HistoryHeight - barHeight)),
                            1,
                            barHeight
                        ),
                        Color.LightGray
                    );
                }
            }

            for(int i = 0; i < WindowHeight; i++)
            {
                int index = History.Count - 1 - i - Scroll;
                if(index < 0) continue;

                var txt = History[index];

                txt.Draw(Renderer.SpriteBatch, new Vector2(chatPos.X + 2, chatPos.Y - 2 - lineHeight - i * lineHeight), txt.Color * alpha);
            }

            if(WindowOpen && TextInput.StartsWith('/') && Cursor > 0)
            {
                int offsetSuggestions = 0;

                // full hint
                var cmd = TextInput[1..];
                var cmdSplit = cmd.Split(' ');
                if(cmd.Length > 0)
                {
                    var node = CommandManager.Dispatcher.FindNode(cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    var hintText = node is not null ? CommandManager.Dispatcher.GetSmartUsage(node, EntityCommandSource.Default) : null;

                    if(hintText is not null && hintText.Count > 0)
                    {
                        float x = 0;
                        List<string> l = [
                            ..from entity in CommandManager.Suggestions?.List ?? []
                            select entity.Text
                        ];

                        if(!hintText.Values.All(l.Contains))
                        {
                            offsetSuggestions = lineHeight;

                            foreach(var token in hintText)
                            {
                                string str = token.Value;
                                if(!token.Value.Contains(cmdSplit[0]) && cmdSplit.Length == 1) continue;

                                Renderer.SpriteBatch.Draw(
                                    Renderer.PixelTexture,
                                    new Rectangle(
                                        (int)x + chatPos.X + 6,
                                        chatPos.Y - lineHeight,
                                        (int)font.MeasureString(str).X + 1,
                                        lineHeight
                                    ),
                                    Color.Black * 0.75f
                                );

                                Renderer.SpriteBatch.DrawStringSpacesFix(
                                    font,
                                    str,
                                    new Vector2(x + chatPos.X + 7, chatPos.Y - 1 - lineHeight),
                                    Color.DarkGray,
                                    spaceWidth
                                );

                                x += font.MeasureString(str).X + spaceWidth;
                            }
                        }
                    }
                }

                if((CommandManager.Suggestions?.List.Count ?? 0) > 0)
                {
                    // inline hint
                    var completion = CommandManager.Suggestions.List[currentSuggestion].Text;

                    if(completion is not null && completion.Length > 0 && !TextInput[1..Cursor].EndsWith(completion))
                    {
                        float x = chatWidth - 2 - MathHelper.Max(chatWidth - 2, font.MeasureString(TextInput).X);

                        string v = completion;
                        if(CommandManager.Suggestions.Range.Length < completion.Length)
                            v = completion[MathHelper.Min(completion.Length - 1, CommandManager.Suggestions.Range.End - CommandManager.Suggestions.Range.Start)..];

                        Renderer.SpriteBatch.DrawStringSpacesFix(
                            font,
                            v,
                            new Vector2(x + chatPos.X + 2 + font.MeasureString(TextInput).X, chatPos.Y - 1),
                            Color.DarkGray,
                            spaceWidth
                        );
                    }

                    // suggestion list
                    Renderer.SpriteBatch.Draw(
                        Renderer.PixelTexture,
                        new Rectangle(
                            chatPos.X + 6,
                            chatPos.Y - offsetSuggestions - MathHelper.Min(suggestionHeight, CommandManager.Suggestions.List.Count) * lineHeight,
                            chatWidth - 6,
                            MathHelper.Min(suggestionHeight, CommandManager.Suggestions.List.Count) * lineHeight
                        ),
                        Color.Black * 0.75f
                    );

                    for(int i = 0; i < CommandManager.Suggestions.List.Count; i++)
                    {
                        if(i < suggestionScroll) continue;
                        if(i - suggestionScroll >= suggestionHeight) break;

                        Renderer.SpriteBatch.DrawStringSpacesFix(
                            font,
                            CommandManager.Suggestions.List[i].Text,
                            new Vector2(chatPos.X + 7, chatPos.Y - offsetSuggestions - lineHeight - 1 - ((i - suggestionScroll) * lineHeight)),
                            i == currentSuggestion ? Color.Yellow : Color.White,
                            spaceWidth
                        );

                        // if(i == currentSuggestion)
                        // {
                        //     Renderer.SpriteBatch.DrawStringSpacesFix(
                        //         font,
                        //         Commands.Suggestions.List[i].Text[..Commands.Suggestions.Range.End],
                        //         new Vector2(chatPos.X + 7, chatPos.Y - lineHeight - 1 - (i * lineHeight)),
                        //         Color.Yellow,
                        //         spaceWidth
                        //     );
                        // }
                    }
                }
            }
        }
    }

    private static void UpdateArrowKeyInput(GameTime gameTime)
    {
        var keysPressed = Input.KeyboardState.GetPressedKeys();

        HashSet<Keys> _lastKeys = new(_lastPressedKeys);
        _arrowKeyInput.Clear();
        var currSeconds = gameTime.TotalGameTime.TotalSeconds;

        foreach(var key in keysPressed)
        {
            int keyNum = -1;
            if(key == Keys.Left)
                keyNum = 0;
            if(key == Keys.Right)
                keyNum = 1;
            if(key == Keys.Up)
                keyNum = 2;
            if(key == Keys.Down)
                keyNum = 3;

            if(keyNum != -1)
            {
                if ((currSeconds > _arrowKeyDelays[keyNum]) || (!_lastKeys.Contains(key)))
                {
                    _arrowKeyInput.Add(key);
                    _arrowKeyDelays[keyNum] = currSeconds + (_lastKeys.Contains(key) ? RepeatRate : RepeatDelay);
                }
            }
        }
        _lastPressedKeys = keysPressed;
    }

    public static void WriteChatMessage(string message, CSteamID origin, bool system = false, bool noLog = false)
    {
        if(system)
        {
            if(!noLog)
                Main.Logger.LogInfo("<Server>: " + message);

            var txt = TextComponent.WordWrap(message, (int)(((Renderer.ScreenSize.X / 2) - 1) / Fonts.RegularFont.MeasureString("0").X));

            foreach(var line in txt.Split('\n'))
            {
                History.Add(new TextComponent {
                    Text = $"{line}",
                    Color = Color.Yellow,
                    Font = Fonts.RegularFont,
                    SpaceWidth = 6,
                    TextAlignment = TextAlignmentPresets.TopLeft,
                    RenderNewlines = false,
                    LineSpacing = 12,
                });
            }
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

            var txt = TextComponent.WordWrap(message, (int)(((Renderer.ScreenSize.X / 2) - 1) / Fonts.RegularFont.MeasureString("0").X));

            foreach(var line in $"{name}: {txt}".Split('\n'))
            {
                History.Add(new TextComponent {
                    Text = $"{line}",
                    Color = Color.White,
                    Font = Fonts.RegularFont,
                    SpaceWidth = 6,
                    TextAlignment = TextAlignmentPresets.TopLeft,
                    RenderNewlines = false,
                    LineSpacing = 12,
                });
            }
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
