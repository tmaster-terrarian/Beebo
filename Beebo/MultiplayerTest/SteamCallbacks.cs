using System;

using Jelly;

using Steamworks;

namespace Beebo.MultiplayerTest;

public static class SteamCallbacks
{
    public static Logger Logger => SteamManager.Logger;

    public static CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;
    public static CallResult<LeaderboardFindResult_t> m_CallResultFindLeaderboard;
    public static Callback<PersonaStateChange_t> m_PersonaStateChange;
    public static Callback<UserStatsReceived_t> m_UserStatsReceived;

    public static void Initialize()
    {
        m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
        m_CallResultFindLeaderboard = CallResult<LeaderboardFindResult_t>.Create(OnFindLeaderboard);
        m_PersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
        m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(
            (pCallback) => {
                Logger.Info("[" + UserStatsReceived_t.k_iCallback + " - UserStatsReceived] - " + pCallback.m_eResult + " -- " + pCallback.m_nGameID + " -- " + pCallback.m_steamIDUser);
            }
        );

        LobbyManager.InitializeCallbacks();
        LobbyServer.InitializeCallbacks();
    }

    private static void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
    {
        Logger.Info("[" + NumberOfCurrentPlayers_t.k_iCallback + " - NumberOfCurrentPlayers] - " + pCallback.m_bSuccess + " -- " + pCallback.m_cPlayers);
    }

    private static void OnFindLeaderboard(LeaderboardFindResult_t pCallback, bool bIOFailure)
    {
        Logger.Info("[" + LeaderboardFindResult_t.k_iCallback + " - LeaderboardFindResult] - " + pCallback.m_bLeaderboardFound + " -- " + pCallback.m_hSteamLeaderboard);
    }

    private static void OnPersonaStateChange(PersonaStateChange_t pCallback)
    {
        Logger.Info("[" + PersonaStateChange_t.k_iCallback + " - PersonaStateChange] - " + pCallback.m_ulSteamID + " -- " + pCallback.m_nChangeFlags);
        PersonaStateChange?.Invoke(null, pCallback);
    }

    public static event EventHandler<PersonaStateChange_t> PersonaStateChange;
}
