using Microsoft.Xna.Framework.Graphics;
using Steamworks;

namespace Beebo.Multiplayer;

public readonly struct LobbyMember(CSteamID id, string name)
{
    public CSteamID CSteamID { get; } = id;

    public string PersonaName { get; } = name;

    public Texture2D Avatar { get; } = Main.GetSteamUserAvatar(id);
}
