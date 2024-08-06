using Steamworks;

namespace Beebo.Net;

// god these names are too long
public enum ChatMemberStateChange
{
    Entered = EChatMemberStateChange.k_EChatMemberStateChangeEntered,
    Left = EChatMemberStateChange.k_EChatMemberStateChangeLeft,
    Disconnected = EChatMemberStateChange.k_EChatMemberStateChangeDisconnected,
    Kicked = EChatMemberStateChange.k_EChatMemberStateChangeKicked,
    Banned = EChatMemberStateChange.k_EChatMemberStateChangeBanned,
}

public enum LobbyType
{
    Private = ELobbyType.k_ELobbyTypePrivate,
    FriendsOnly = ELobbyType.k_ELobbyTypeFriendsOnly,
    Public = ELobbyType.k_ELobbyTypePublic,
    Invisible = ELobbyType.k_ELobbyTypeInvisible,
    PrivateUnique = ELobbyType.k_ELobbyTypePrivateUnique,
}

public enum PacketSendMethod
{
    Unreliable = EP2PSend.k_EP2PSendUnreliable,
    UnreliableNoDelay = EP2PSend.k_EP2PSendUnreliableNoDelay,
    Reliable = EP2PSend.k_EP2PSendReliable,
    ReliableWithBuffering = EP2PSend.k_EP2PSendReliableWithBuffering,
}
