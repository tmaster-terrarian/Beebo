namespace Beebo.Net;

public enum PacketType : byte
{
    FirstJoin,
    JellySync,
    ChatMessage,
    ChatMessage2,
    SceneChange,
    CallbackRequest,
    CallbackResponse,
}

public enum FirstJoinPacketType : byte
{
    SyncRequest,
    Sync
}

public enum CallbackPacketType : byte
{
    SceneChange
}
