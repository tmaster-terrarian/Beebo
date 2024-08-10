using Jelly.Net;

namespace Beebo.Net;

public class BeeboNetworkProvider : NetworkProvider
{
    public override bool NetworkingEnabled => Program.UseSteamworks && SteamManager.IsSteamRunning;

    public override bool IsHost => Main.IsHost;

    public override int GetNetID()
    {
        return Main.NetID;
    }

    public override int GetHostNetID()
    {
        return P2PManager.GetMemberIndex(P2PManager.GetLobbyOwner());
    }

    public override void SendSyncPacket(SyncPacketType syncPacketType, byte[] data, bool important)
    {
        P2PManager.SendP2PPacket(PacketType.JellySync, [(byte)syncPacketType, ..data], important ? PacketSendMethod.Reliable : PacketSendMethod.Unreliable);
    }
}
