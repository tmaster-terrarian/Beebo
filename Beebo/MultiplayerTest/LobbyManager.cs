using System;
using System.Collections.Generic;
using Steamworks;

namespace Beebo.MultiplayerTest;

public static class LobbyManager
{
    private static Callback<P2PSessionRequest_t> Callback_NewConnection;

    public static List<CSteamID> LobbyMembers { get; } = [];

    public static void InitializeCallbacks()
    {
        Callback_NewConnection = Callback<P2PSessionRequest_t>.Create(OnNewConnection);
    }

    public static void Update()
    {
        GetNetworkData();
    }

    private static void GetNetworkData()
    {
        while(SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
        {
            byte[] packet = new byte[msgSize];
            if(SteamNetworking.ReadP2PPacket(packet, msgSize, out uint bytesRead, out CSteamID steamIDRemote))
            {
                PacketType TYPE = (PacketType)packet[0];
                string msg = System.Text.Encoding.UTF8.GetString(SubArray(packet, 1, packet.Length - 1));

                switch(TYPE)
                {
                    case PacketType.ChatMessage:
                        SteamManager.Logger.Info(SteamFriends.GetFriendPersonaName(steamIDRemote) + " says: " + msg);
                        break;
                    default:
                        SteamManager.Logger.Warn("Received an invalid packet with unknown type: " + (int)TYPE + ", value: '" + msg + "'");
                        break;
                }
            }
        }
    }

    static int GetMemberIndex(CSteamID id)
    {
        for(int i = 0; i < LobbyMembers.Count; i++)
        {
            if(LobbyMembers[i] == id) return i;
        }
        return -1;
    }

    public static void NetBroadcast(PacketType TYPE, string message)
    {
        for(int i = 0; i < LobbyMembers.Count; i++)
        {
            if(LobbyMembers[i] == SteamUser.GetSteamID())
                continue;

            byte[] b_message = System.Text.Encoding.UTF8.GetBytes(message);
            byte[] sendBytes = new byte[b_message.Length + 1];

            sendBytes[0] = (byte)TYPE;
            b_message.CopyTo(sendBytes, 1);

            SteamNetworking.SendP2PPacket(LobbyMembers[i], sendBytes, (uint)sendBytes.Length, EP2PSend.k_EP2PSendReliable);
        }
    }

    private static void OnNewConnection(P2PSessionRequest_t result)
    {
        foreach(var member in LobbyMembers)
        {
            if(member == result.m_steamIDRemote)
            {
                SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
                return;
            }
        }
    }

    public static T[] SubArray<T>(T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}

public enum PacketType
{
    ChatMessage = 1
}
