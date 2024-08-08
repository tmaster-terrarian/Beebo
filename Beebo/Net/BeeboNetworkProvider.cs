using Jelly.Net;

namespace Beebo.Net;

public class BeeboNetworkProvider : NetworkProvider
{
    public override bool IsHost => Main.IsHost;

    public override int GetNetID()
    {
        return Main.NetID;
    }

    public override int GetHostNetID()
    {
        return P2PManager.GetMemberIndex(P2PManager.GetLobbyOwner());
    }

    public override void SendSyncPacket(byte[] data, bool important)
    {
        
    }
}
