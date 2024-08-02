using Microsoft.Xna.Framework.Graphics;

using Steamworks;

namespace Beebo.MultiplayerTest;

public class LobbyMember(CSteamID id, string name = "unknown")
{
    public CSteamID CSteamID { get; } = id;

    public string PersonaName { get; set; } = name;

    public Texture2D Avatar { get; set; } = Main.GetSteamUserAvatar(id);
}
