using System.Collections.Generic;
using Jelly;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Beebo.MultiplayerTest;

[System.Obsolete("Deprecated, use Beebo.Net.P2PManager")]
public static class LobbyServer
{
    public static Callback<LobbyCreated_t> Callback_lobbyCreated { get; private set; }
    public static Callback<LobbyMatchList_t> Callback_lobbyList { get; private set; }
    public static Callback<LobbyEnter_t> Callback_lobbyEnter { get; private set; }
    public static Callback<LobbyDataUpdate_t> Callback_lobbyInfo { get; private set; }

    public static ulong Current_lobbyID { get; private set; }

    public static void InitializeCallbacks()
    {
        Callback_lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        Callback_lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
        Callback_lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        Callback_lobbyInfo = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyInfo);
    }

    static readonly List<CSteamID> lobbyIDS = [];

    public static void Update()
    {
        // Command - Create new lobby
        if(Input.GetPressed(Keys.C))
        {
            SteamManager.Logger.Info("Trying to create lobby ...");
            SteamAPICall_t try_toHost = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4);
        }

        // Command - List lobbies
        if(Input.GetPressed(Keys.L))
        {
            SteamManager.Logger.Info("Trying to get list of available lobbies ...");
            SteamAPICall_t try_getList = SteamMatchmaking.RequestLobbyList();
        }

        // Command - Join lobby at index 0 (testing purposes)
        if(Input.GetPressed(Keys.J))
        {
            SteamManager.Logger.Info("Trying to join FIRST listed lobby ...");
            SteamAPICall_t try_joinLobby = SteamMatchmaking.JoinLobby(SteamMatchmaking.GetLobbyByIndex(0));
        }

        // Command - List lobby members
        if(Input.GetPressed(Keys.Q))
        {
            GetCurrentLobbyMembers();
        }
    }

    public static List<CSteamID> GetCurrentLobbyMembers(bool log = true)
    {
        int numPlayers = SteamMatchmaking.GetNumLobbyMembers((CSteamID)Current_lobbyID);

        if(log)
            SteamManager.Logger.Info("\t Number of players currently in lobby : " + numPlayers);

        List<CSteamID> ids = [];

        for (int i = 0; i < numPlayers; i++)
        {
            var id = SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)Current_lobbyID, i);

            if(log)
                SteamManager.Logger.Info("\t Player(" + i + ") == " + SteamFriends.GetFriendPersonaName(id));

            ids.Add(id);
        }

        return ids;
    }

    static void OnLobbyCreated(LobbyCreated_t result)
    {
        if(result.m_eResult == EResult.k_EResultOK)
            SteamManager.Logger.Info("Lobby created -- SUCCESS!");
        else
            SteamManager.Logger.Info("Lobby created -- failure ...");

        string personalName = SteamFriends.GetPersonaName();
        var lobbyId = (CSteamID)result.m_ulSteamIDLobby;

        SteamMatchmaking.SetLobbyData(lobbyId, "name", personalName + "'s Lobby");
    }

    static void OnGetLobbiesList(LobbyMatchList_t result)
    {
        SteamManager.Logger.Info("Found " + result.m_nLobbiesMatching + " lobbies!");

        for(int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDS.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }

    static void OnGetLobbyInfo(LobbyDataUpdate_t result)
    {
        for(int i = 0; i < lobbyIDS.Count; i++)
        {
            if(lobbyIDS[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                SteamManager.Logger.Info("Lobby " + i + " :: " + SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDS[i].m_SteamID, "name"));
                return;
            }
        }
    }

    static void OnLobbyEntered(LobbyEnter_t result)
    {
        Current_lobbyID = result.m_ulSteamIDLobby;

        var members = GetCurrentLobbyMembers();

        if (result.m_EChatRoomEnterResponse == 1)
        {
            SteamManager.Logger.Info("Lobby joined!");
        }
        else
            SteamManager.Logger.Info("Failed to join lobby.");
    }
}