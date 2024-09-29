using System;
using System.Collections.Generic;
using Beebo.GameContent;
using Beebo.GameContent.Entities;
using Jelly.GameContent;
using Jelly.Net;
using Microsoft.Xna.Framework;
using Steamworks;

namespace Beebo.Net;

public static class P2PManager
{
    private static Callback<P2PSessionRequest_t> Callback_P2PSessionRequest;
    private static Callback<LobbyCreated_t> Callback_lobbyCreated;
    private static Callback<LobbyMatchList_t> Callback_lobbyList;
    private static Callback<LobbyEnter_t> Callback_lobbyEnter;
    private static Callback<LobbyDataUpdate_t> Callback_lobbyDataUpdate;
    private static Callback<LobbyChatUpdate_t> Callback_lobbyChatUpdate;
    private static Callback<PersonaStateChange_t> Callback_personaStateChange;
    private static Callback<GameLobbyJoinRequested_t> Callback_lobbyJoinRequested;

    public static bool InLobby { get; private set; }
    public static CSteamID CurrentLobby { get; private set; }
    public static CSteamID MyID => SteamUser.GetSteamID();

    public static int PlayerCount => InLobby ? SteamMatchmaking.GetNumLobbyMembers(CurrentLobby) : 1;

    public static List<CSteamID> PublicLobbyList { get; } = [];

    public static void InitializeCallbacks()
    {
        Callback_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnNewConnection);
        Callback_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        Callback_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
        Callback_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        Callback_lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);
        Callback_lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        Callback_personaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
        Callback_lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
    }

    public static void ReadAvailablePackets()
    {
        while(SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
        {
            byte[] packet = new byte[msgSize];
            if(SteamNetworking.ReadP2PPacket(packet, msgSize, out uint bytesRead, out CSteamID steamIDRemote))
            {
                if(msgSize <= 0) return;

                PacketType TYPE = (PacketType)packet[0];
                byte[] data = packet.Length > 1 ? packet[1..] : [];

                string dataString = data.Length > MathHelper.Max(1, data[0])
                    ? data[1..MathHelper.Min(data[0] + 1, data.Length)].ToStringUTF8()
                    : "";

                switch(TYPE)
                {
                    case PacketType.FirstJoin:
                    {
                        switch((FirstJoinPacketType)data[0])
                        {
                            case FirstJoinPacketType.SyncRequest:
                            {
                                if(!Main.IsHost)
                                    return;

                                // send current game state to steamIDRemote
                                SendP2PPacket(
                                    steamIDRemote,
                                    PacketType.FirstJoin,
                                    [
                                        (byte)FirstJoinPacketType.Sync,
                                        .. Main.GetSyncPacket()
                                    ],
                                    PacketSendMethod.Reliable
                                );

                                break;
                            }
                            case FirstJoinPacketType.Sync:
                            {
                                Main.ReadSyncPacket(data[1..]);
                                break;
                            }
                        }

                        break;
                    }
                    // case PacketType.JellySync:
                    // {
                    //     // set the part of the game state from data owned by the sender
                    //     // first byte is a Jelly.Net.SyncPacketType value

                    //     switch((SyncPacketType)data[0])
                    //     {
                    //         case SyncPacketType.EntityAdded:
                    //         case SyncPacketType.ComponentAdded:
                    //         case SyncPacketType.ComponentUpdate:
                    //             BeeboNetworkProvider.PacketInterceptRead(data, GetMemberIndex(steamIDRemote));
                    //             break;
                    //         default:
                    //             Jelly.Providers.NetworkProvider.RaisePacketReceivedEvent(data, GetMemberIndex(steamIDRemote));
                    //             break;
                    //     }

                    //     break;
                    // }
                    case PacketType.ChatMessage:
                    {
                        Chat.WriteChatMessage(dataString, steamIDRemote);
                        break;
                    }
                    case PacketType.ChatMessage2:
                    {
                        Chat.WriteChatMessage(dataString, steamIDRemote, true);
                        break;
                    }
                    case PacketType.SceneChange:
                    {
                        Main.ReadSyncPacket(data[1..]);
                        SendP2PPacket(steamIDRemote, PacketType.CallbackRequest, [(byte)CallbackPacketType.SceneChange], PacketSendMethod.Reliable);
                        break;
                    }
                    case PacketType.CallbackRequest:
                    {
                        switch((CallbackPacketType)data[0])
                        {
                            case CallbackPacketType.SceneChange:
                            {
                                Main.Logger.LogInfo($"{SteamFriends.GetFriendPersonaName(steamIDRemote)} ({steamIDRemote}) has loaded");
                                break;
                            }
                        }
                        break;
                    }
                    case PacketType.CallbackResponse:
                    {
                        switch((CallbackPacketType)data[0])
                        {
                            case CallbackPacketType.SceneChange:
                            {
                                Main.Logger.LogInfo("All players have finished loading, beginning scene");
                                // Main.Scene?.Begin();
                                break;
                            }
                        }
                        break;
                    }
                    default:
                    {
                        SteamManager.Logger.LogWarning("Received an invalid packet with unknown type: " + packet[0] + ", value: '" + string.Join("", data) + "'");
                        break;
                    }
                }
            }
        }
    }

    public static void SendP2PPacket(PacketType type, byte[] message, PacketSendMethod packetSendMethod = PacketSendMethod.Reliable)
    {
        for(int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(CurrentLobby); i++)
        {
            SendP2PPacket(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i), type, message, packetSendMethod);
        }
    }

    public static void SendP2PPacketString(PacketType type, string message, PacketSendMethod packetSendMethod = PacketSendMethod.Reliable)
    {
        SendP2PPacket(type, message.GetBytes(), packetSendMethod);
    }

    public static void SendP2PPacket(CSteamID target, PacketType type, byte[] message, PacketSendMethod packetSendMethod = PacketSendMethod.Reliable)
    {
        if(target == MyID)
            return;

        byte[] sendBytes = [(byte)type, ..message];

        SteamNetworking.SendP2PPacket(target, sendBytes, (uint)sendBytes.Length, (EP2PSend)packetSendMethod);
    }

    public static void SendP2PPacketString(CSteamID target, PacketType type, string message, PacketSendMethod packetSendMethod = PacketSendMethod.Reliable)
    {
        SendP2PPacket(target, type, message.GetBytes(), packetSendMethod);
    }

    public static void CreateLobby(LobbyType lobbyType = LobbyType.Public, int maxPlayers = 4)
    {
        LeaveLobby();
        SteamAPICall_t try_toHost = SteamMatchmaking.CreateLobby((ELobbyType)lobbyType, maxPlayers);
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
            SteamManager.Logger.LogInfo($"Leaving current lobby ({CurrentLobby.m_SteamID}) ...");
            HandleLobbyOwnerLeft();

            Main.HandleLeavingLobby();

            foreach (var id in GetCurrentLobbyMembers())
            {
                if (id != MyID)
                    SteamNetworking.CloseP2PSessionWithUser(id);
            }

            SteamMatchmaking.LeaveLobby(CurrentLobby);
            CurrentLobby = CSteamID.Nil;
            InLobby = false;

            Chat.ChatHistory.Clear();

            SteamManager.Logger.LogInfo($"Lobby left!");
        }
    }

    private static void HandleLobbyOwnerLeft()
    {
        if (Main.IsHost)
        {
            var players = GetCurrentLobbyMembers();
            if (players.Count > 1)
            {
                players.Remove(MyID);
                var newOwner = players[0];

                SteamManager.Logger.LogInfo($"Transferring lobby ownership to {SteamFriends.GetFriendPersonaName(newOwner)} ({newOwner})");

                SetLobbyOwner(MyID, newOwner);
            }
        }
    }

    public static void SetLobbyOwner(CSteamID oldOwner, CSteamID newOwner)
    {
        string message = $"{SteamFriends.GetFriendPersonaName(newOwner)} is now the lobby owner.";

        // SendP2PPacketString(PacketType.ChatMessage2, $"{SteamFriends.GetFriendPersonaName(oldOwner)} has transferred ownership of the lobby to {SteamFriends.GetFriendPersonaName(newOwner)}.", PacketSendMethod.Reliable);

        Chat.WriteChatMessage(message, MyID, true);
        SendP2PPacketString(PacketType.ChatMessage2, message, PacketSendMethod.Reliable);

        SteamMatchmaking.SetLobbyOwner(CurrentLobby, newOwner);
    }

    public static List<CSteamID> GetCurrentLobbyMembers(bool log = false)
    {
        if(!InLobby)
            return [];

        int numPlayers = PlayerCount;

        if(log)
            SteamManager.Logger.LogInfo("\t Number of players currently in lobby: " + numPlayers);

        List<CSteamID> ids = [];

        for(int i = 0; i < numPlayers; i++)
        {
            var id = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);

            if(log)
                SteamManager.Logger.LogInfo("\t Player " + (i + 1) + ": " + SteamFriends.GetFriendPersonaName(id) + (GetLobbyOwner() == id ? " (owner)" : ""));

            ids.Add(id);
        }

        return ids;
    }

    private static void OnLobbyCreated(LobbyCreated_t result)
    {
        if(result.m_eResult == EResult.k_EResultOK)
            SteamManager.Logger.LogInfo("Lobby created -- SUCCESS!");
        else
        {
            SteamManager.Logger.LogInfo("Lobby created -- failure ...");
            return;
        }

        string personalName = SteamFriends.GetPersonaName();
        var lobbyId = (CSteamID)result.m_ulSteamIDLobby;

        SteamMatchmaking.SetLobbyData(lobbyId, "name", personalName + "'s Lobby");
    }

    private static void OnGetLobbiesList(LobbyMatchList_t result)
    {
        PublicLobbyList.Clear();

        SteamManager.Logger.LogInfo("Found " + result.m_nLobbiesMatching + " lobbies!");

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
                var name = SteamMatchmaking.GetLobbyData((CSteamID)PublicLobbyList[i].m_SteamID, "name");
                if(name != "")
                    SteamManager.Logger.LogInfo($"Lobby {i} :: {name}");
                return;
            }
        }
    }

    private static void OnLobbyChatUpdate(LobbyChatUpdate_t result)
    {
        if(result.m_ulSteamIDLobby == CurrentLobby.m_SteamID)
        {
            var state = (ChatMemberStateChange)result.m_rgfChatMemberStateChange;
            var idChanged = (CSteamID)result.m_ulSteamIDUserChanged;
            var idMakingChange = (CSteamID)result.m_ulSteamIDUserChanged;

            string message = null;

            if(Main.IsHost)
            {
                if((state & ChatMemberStateChange.Entered) == ChatMemberStateChange.Entered)
                {
                    message = $"{SteamFriends.GetFriendPersonaName(idChanged)} joined.";
                }
                if((state & ChatMemberStateChange.Left) == ChatMemberStateChange.Left)
                {
                    message = $"{SteamFriends.GetFriendPersonaName(idChanged)} left.";
                }
                if((state & ChatMemberStateChange.Disconnected) == ChatMemberStateChange.Disconnected)
                {
                    message = $"{SteamFriends.GetFriendPersonaName(idChanged)} lost connection.";
                }
                if((state & ChatMemberStateChange.Kicked) == ChatMemberStateChange.Kicked)
                {
                    message = $"{SteamFriends.GetFriendPersonaName(idChanged)} was kicked from the lobby by {SteamFriends.GetFriendPersonaName(idMakingChange)}.";
                }
                if((state & ChatMemberStateChange.Banned) == ChatMemberStateChange.Banned)
                {
                    message = $"{SteamFriends.GetFriendPersonaName(idChanged)} was banned from the lobby by {SteamFriends.GetFriendPersonaName(idMakingChange)}.";
                }
            }

            if(state - ChatMemberStateChange.Entered != 0)
            {
                if(idChanged == MyID)
                {
                    Main.HandleLeavingLobby();
                }
            }

            if(message is not null)
            {
                Chat.WriteChatMessage(message, idChanged, true);
                SendP2PPacketString(PacketType.ChatMessage2, message, PacketSendMethod.Reliable);
            }
        }
    }

    private static void OnPersonaStateChange(PersonaStateChange_t result)
    {
        var id = (CSteamID)result.m_ulSteamID;

        if(GetMemberIndex(id) == -1)
            return;

        if((result.m_nChangeFlags & EPersonaChange.k_EPersonaChangeAvatar) != 0)
        {
            Main.AlreadyLoadedAvatars.Remove(id);
        }
    }

    private static void OnLobbyEntered(LobbyEnter_t result)
    {
        if(result.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            CurrentLobby = new CSteamID(result.m_ulSteamIDLobby);
            InLobby = true;

            foreach(var user in GetCurrentLobbyMembers())
            {
                if(SteamFriends.RequestUserInformation(user, false))
                {
                    Main.AlreadyLoadedAvatars.Remove(user);
                    Main.AlreadyLoadedAvatars.Add(user, Main.DefaultSteamProfile);
                }
            }

            SendP2PPacket(GetLobbyOwner(), PacketType.FirstJoin, [(byte)FirstJoinPacketType.SyncRequest, ], PacketSendMethod.Reliable);

            if(Main.IsHost)
            {
                Main.ChangeScene("Test");
            }

            SteamManager.Logger.LogInfo("Lobby joined!");
        }
        else
        {
            SteamManager.Logger.LogInfo("Failed to join lobby.");
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
