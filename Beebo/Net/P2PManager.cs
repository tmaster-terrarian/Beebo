using System.Collections.Generic;

using Steamworks;

namespace Beebo.Net;

public static class P2PManager
{
    private static Callback<P2PSessionRequest_t> Callback_P2PSessionRequest;
    private static Callback<LobbyCreated_t> Callback_lobbyCreated;
    private static Callback<LobbyMatchList_t> Callback_lobbyList;
    private static Callback<LobbyEnter_t> Callback_lobbyEnter;
    private static Callback<LobbyDataUpdate_t> Callback_lobbyInfo;
    private static Callback<GameLobbyJoinRequested_t> Callback_lobbyJoinRequested;

    public static bool InLobby { get; private set; }
    public static CSteamID CurrentLobby { get; private set; }
    public static CSteamID MyID => SteamUser.GetSteamID();

    public static List<CSteamID> PublicLobbyList { get; } = [];

    public static void InitializeCallbacks()
    {
        Callback_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnNewConnection);
        Callback_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        Callback_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
        Callback_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        Callback_lobbyInfo = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);
        Callback_lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
    }

    public static void ReadAvailablePackets()
    {
        while(SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
        {
            byte[] packet = new byte[msgSize];
            if(SteamNetworking.ReadP2PPacket(packet, msgSize, out uint bytesRead, out CSteamID steamIDRemote))
            {
                PacketType TYPE = (PacketType)packet[0];
                byte[] data = packet[1..];

                string dataString = System.Text.Encoding.UTF8.GetString(data);

                switch(TYPE)
                {
                    case PacketType.FirstJoinSyncRequest:
                    {
                        if(Main.IsHost)
                        {
                            // send current game state to steamIDRemote
                        }
                        break;
                    }
                    case PacketType.FirstJoinSync:
                    {
                        // set game state from data
                        break;
                    }
                    case PacketType.Sync:
                    {
                        // set the part of the game state from data owned by the sender
                        break;
                    }
                    case PacketType.ChatMessage:
                    {
                        SteamManager.Logger.Info(SteamFriends.GetFriendPersonaName(steamIDRemote) + " says: " + dataString);
                        break;
                    }
                    default:
                    {
                        SteamManager.Logger.Warn("Received an invalid packet with unknown type: " + (int)TYPE + ", value: '" + string.Join("", data) + "'");
                        break;
                    }
                }
            }
        }
    }

    public static void SendP2PPacket(PacketType type, byte[] message, PacketDelivery packetSendMethod = PacketDelivery.Reliable)
    {
        for(int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(CurrentLobby); i++)
        {
            SendP2PPacket(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i), type, message, packetSendMethod);
        }
    }

    public static void SendP2PPacketString(PacketType type, string message, PacketDelivery packetSendMethod = PacketDelivery.Reliable)
    {
        SendP2PPacket(type, System.Text.Encoding.UTF8.GetBytes(message), packetSendMethod);
    }

    public static void SendP2PPacket(CSteamID target, PacketType type, byte[] message, PacketDelivery packetSendMethod = PacketDelivery.Reliable)
    {
        if(target == MyID)
            return;

        byte[] sendBytes = [(byte)type, ..message];

        // var w = new BinaryWriter(new BinaryStream());

        SteamNetworking.SendP2PPacket(target, sendBytes, (uint)sendBytes.Length, (EP2PSend)packetSendMethod);
    }

    public static void SendP2PPacketString(CSteamID target, PacketType type, string message, PacketDelivery packetSendMethod = PacketDelivery.Reliable)
    {
        SendP2PPacket(target, type, System.Text.Encoding.UTF8.GetBytes(message), packetSendMethod);
    }

    public static void CreateLobby(ELobbyType eLobbyType = ELobbyType.k_ELobbyTypePublic)
    {
        LeaveLobby();
        SteamAPICall_t try_toHost = SteamMatchmaking.CreateLobby(eLobbyType, 4);
    }

    public static void JoinLobby(CSteamID steamIDLobby)
    {
        LeaveLobby();
        SteamAPICall_t try_toJoin = SteamMatchmaking.JoinLobby(steamIDLobby);
    }

    public static void LeaveLobby()
    {
        if(InLobby)
        {
            if(Main.IsHost)
            {
                var players = GetCurrentLobbyMembers();
                if(players.Count > 1)
                {
                    players.Remove(MyID);
                    SteamMatchmaking.SetLobbyOwner(CurrentLobby, players[0]);
                }
            }

            foreach(var id in GetCurrentLobbyMembers())
            {
                if(id != MyID)
                    SteamNetworking.CloseP2PSessionWithUser(id);
            }

            SteamMatchmaking.LeaveLobby(CurrentLobby);
            CurrentLobby = CSteamID.Nil;
            InLobby = false;
        }
    }

    public static List<CSteamID> GetCurrentLobbyMembers(bool log = false)
    {
        if(!InLobby)
            return [];

        int numPlayers = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);

        if(log)
            SteamManager.Logger.Info("\t Number of players currently in lobby: " + numPlayers);

        List<CSteamID> ids = [];

        for(int i = 0; i < numPlayers; i++)
        {
            var id = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);

            if(log)
                SteamManager.Logger.Info("\t Player " + (i + 1) + ": " + SteamFriends.GetFriendPersonaName(id) + (GetLobbyOwner() == id ? " (owner)" : ""));

            ids.Add(id);
        }

        return ids;
    }

    private static void OnLobbyCreated(LobbyCreated_t result)
    {
        if(result.m_eResult == EResult.k_EResultOK)
            SteamManager.Logger.Info("Lobby created -- SUCCESS!");
        else
            SteamManager.Logger.Info("Lobby created -- failure ...");

        string personalName = SteamFriends.GetPersonaName();
        var lobbyId = (CSteamID)result.m_ulSteamIDLobby;

        SteamMatchmaking.SetLobbyData(lobbyId, "name", personalName + "'s Lobby");
    }

    private static void OnGetLobbiesList(LobbyMatchList_t result)
    {
        PublicLobbyList.Clear();

        SteamManager.Logger.Info("Found " + result.m_nLobbiesMatching + " lobbies!");

        for(int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            if(SteamMatchmaking.RequestLobbyData(lobbyID))
            {
                PublicLobbyList.Add(lobbyID);
            }
        }
    }

    private static void OnGetLobbyInfo(LobbyDataUpdate_t result)
    {
        for(int i = 0; i < PublicLobbyList.Count; i++)
        {
            if(PublicLobbyList[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                SteamManager.Logger.Info("Lobby " + i + " :: " + SteamMatchmaking.GetLobbyData((CSteamID)PublicLobbyList[i].m_SteamID, "name"));
                return;
            }
        }
    }

    private static void OnLobbyEntered(LobbyEnter_t result)
    {
        if(result.m_EChatRoomEnterResponse == 1)
        {
            CurrentLobby = new CSteamID(result.m_ulSteamIDLobby);
            InLobby = true;

            if(GetLobbyOwner(out CSteamID owner) && owner != MyID)
            {
                SendP2PPacket(owner, PacketType.FirstJoinSyncRequest, []);
            }

            SteamManager.Logger.Info("Lobby joined!");
        }
        else
        {
            SteamManager.Logger.Info("Failed to join lobby.");
        }
    }

    private static void OnNewConnection(P2PSessionRequest_t result)
    {
        if(InLobby)
        {
            int count = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            for(int i = 0; i < count; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);

                if(member == result.m_steamIDRemote)
                {
                    SteamNetworking.AcceptP2PSessionWithUser(result.m_steamIDRemote);
                    return;
                }
            }
        }
    }

    public static CSteamID GetLobbyOwner()
    {
        return CurrentLobby == CSteamID.Nil ? CSteamID.Nil : SteamMatchmaking.GetLobbyOwner(CurrentLobby);
    }

    public static bool GetLobbyOwner(out CSteamID cSteamID)
    {
        return (cSteamID = GetLobbyOwner()) != CSteamID.Nil;
    }

    public static int GetMemberIndex(CSteamID id)
    {
        if(!InLobby)
            return -1;

        for(int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(CurrentLobby); i++)
        {
            if(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i) == id) return i;
        }
        return -1;
    }

    public static bool GetMemberIndex(CSteamID id, out int index)
    {
        return (index = GetMemberIndex(id)) != -1;
    }

    private static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
    {
        JoinLobby(param.m_steamIDLobby);
    }
}
