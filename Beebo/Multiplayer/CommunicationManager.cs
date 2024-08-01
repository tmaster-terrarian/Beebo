using System;
using System.Collections.Generic;
using Steamworks;

namespace Beebo.Multiplayer;

public static class CommunicationManager
{
    private static readonly Callback<P2PSessionRequest_t> Callback_NewConnection = Callback<P2PSessionRequest_t>.Create(OnNewConnection);
    private static readonly Callback<LobbyChatUpdate_t> Callback_ConnectionChanged = Callback<LobbyChatUpdate_t>.Create(OnConnectionChanged);

    private static readonly List<LobbyMember> _lobbyMembers = [];

    public static void InitializeCallbacks()
    {
        SteamCallbacks.PersonaStateChange += OnPersonaStateChange;
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
                        SteamManager.Logger.Info(_lobbyMembers[GetMemberIndex(steamIDRemote)].PersonaName + " says: " + msg);
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
        for(int i = 0; i < _lobbyMembers.Count; i++)
        {
            if(_lobbyMembers[i].CSteamID == id) return i;
        }
        return -1;
    }

    public static void NetBroadcast(PacketType TYPE, string message)
    {
        for(int i = 0; i < _lobbyMembers.Count; i++)
        {
            if(_lobbyMembers[i].CSteamID == SteamUser.GetSteamID())
                continue;

            byte[] b_message = System.Text.Encoding.UTF8.GetBytes(message);
            byte[] sendBytes = new byte[b_message.Length + 1];

            sendBytes[0] = (byte)TYPE;
            b_message.CopyTo(sendBytes, 1);

            SteamNetworking.SendP2PPacket(_lobbyMembers[i].CSteamID, sendBytes, (uint)sendBytes.Length, EP2PSend.k_EP2PSendReliable);
        }
    }

    private static void OnNewConnection(P2PSessionRequest_t result)
    {
        foreach(var member in _lobbyMembers)
        {
            if(member.CSteamID == result.m_steamIDRemote)
            {
                SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
                return;
            }
        }
    }

    private static void OnConnectionChanged(LobbyChatUpdate_t result)
    {
        var mask = (EChatMemberStateChange)result.m_rgfChatMemberStateChange;

        var UserChanged = (CSteamID)result.m_ulSteamIDUserChanged;
        var MakingChange = (CSteamID)result.m_ulSteamIDMakingChange;

        if((mask & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
        {
            _lobbyMembers.Add(new(UserChanged, SteamFriends.GetPlayerNickname(UserChanged)));
        }
        else if((mask & (EChatMemberStateChange.k_EChatMemberStateChangeDisconnected | EChatMemberStateChange.k_EChatMemberStateChangeLeft)) != 0)
        {
            _lobbyMembers.RemoveAt(GetMemberIndex(UserChanged));
        }
    }

    private static void OnPersonaStateChange(object? sender, PersonaStateChange_t pCallback)
    {
        
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
    ChatMessage
}
