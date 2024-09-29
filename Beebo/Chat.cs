using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Beebo.GameContent;
using Beebo.Net;

using Jelly;
using Jelly.Graphics;

using Steamworks;

namespace Beebo;

public static class Chat
{
    private static float chatAlpha;

    public static bool ChatWindowOpen { get; private set; } = false;
    public static string CurrentChatInput { get; private set; } = "";

    public static List<Tuple<string, Color>> ChatHistory { get; } = [];

    public static void CancelTypingAndClose()
    {
        ChatWindowOpen = false;
        CurrentChatInput = "";
    }

    public static void Update()
    {
        if(ChatWindowOpen)
        {
            List<char> input = [..Input.GetTextInput()];
            bool backspace = input.Remove('\x127');

            CurrentChatInput += string.Join(null, input);
            CurrentChatInput = CurrentChatInput[..MathHelper.Min(CurrentChatInput.Length, 4096)];

            if(backspace && CurrentChatInput.Length > 0)
            {
                CurrentChatInput = CurrentChatInput[..^1];
            }
        }

        if(Input.GetPressed(Keys.Enter))
        {
            ChatWindowOpen = !ChatWindowOpen;
            if(!ChatWindowOpen && CurrentChatInput.Length > 0)
            {
                string message = CurrentChatInput[..MathHelper.Min(CurrentChatInput.Length, 4096)];

                WriteChatMessage(message, (!SteamManager.IsSteamRunning) ? CSteamID.Nil : P2PManager.MyID, false);
                CurrentChatInput = "";
            }
        }

        if(!ChatWindowOpen)
        {
            if(Input.GetPressed(Keys.F3))
            {
                if(SceneManager.ActiveScene is not null)
                {
                    var json = SceneManager.ActiveScene.Serialize(false);
                    Main.Logger.LogInfo(json);
                    // var newScene = SceneDef.Deserialize(json);
                    // Logger.LogInfo(newScene.Serialize(false));
                }
            }
        }
    }

    public static void DrawUI()
    {
        if(ChatWindowOpen || chatAlpha > 0)
        {
            const int spaceWidth = 4;
            const int chatWidth = 256;
            Point chatPos = new(2, Renderer.ScreenSize.Y - 16);

            float alpha = ChatWindowOpen ? 1 : chatAlpha;

            if(ChatHistory.Count > 0)
            {
                Renderer.SpriteBatch.Draw(
                    Renderer.PixelTexture,
                    new Rectangle(
                        chatPos.X,
                        chatPos.Y - 12 * MathHelper.Min(5, ChatHistory.Count),
                        chatWidth,
                        12 * MathHelper.Min(5, ChatHistory.Count)
                    ),
                    Color.Black * 0.5f * alpha
                );
            }

            if(ChatWindowOpen)
            {
                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, new Rectangle(chatPos.X, chatPos.Y, chatWidth, 12), Color.Black * 0.67f);

                float x = chatWidth - 1 - MathHelper.Max(
                    chatWidth - 1,
                    Main.RegularFont.MeasureString(CurrentChatInput).X + (CurrentChatInput.Split(' ').Length - 1) * spaceWidth
                );

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    Main.RegularFont,
                    CurrentChatInput,
                    new Vector2(x + chatPos.X + 1, chatPos.Y - 1),
                    Color.White,
                    spaceWidth
                );
            }

            for(int i = 0; i < 5; i++)
            {
                int index = ChatHistory.Count - 1 - i;
                if(index < 0) continue;

                Renderer.SpriteBatch.DrawStringSpacesFix(
                    Main.RegularFont,
                    ChatHistory[index].Item1,
                    new Vector2(chatPos.X + 1, chatPos.Y - 13 - (i * 12)),
                    ChatHistory[index].Item2 * alpha,
                    spaceWidth
                );
            }
        }
    }

    public static void WriteChatMessage(string message, CSteamID origin, bool system = false, bool noLog = false)
    {
        if(system)
        {
            if(!noLog)
                Main.Logger.LogInfo("Server msg: " + message);

            ChatHistory.Add(new(message, Color.Yellow));
        }
        else
        {
            string name = "???";
            if(SteamManager.IsSteamRunning && origin != CSteamID.Nil)
            {
                name = SteamFriends.GetFriendPersonaName(origin);
            }

            if(!noLog)
                Main.Logger.LogInfo(name + " says: " + message);

            ChatHistory.Add(new($"{name}: {message}", Color.White));
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
    }
}
