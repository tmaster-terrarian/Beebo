namespace Beebo.Net;

public enum PacketType : byte
{
    FirstJoin,
    JellySync,
    ChatMessage,
    ChatMessage2,
    SceneChange,
}

public enum FirstJoinPacketType : byte
{
    SyncRequest,
    Sync
}
