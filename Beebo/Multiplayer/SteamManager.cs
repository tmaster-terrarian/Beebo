using System;

using Steamworks;

namespace Beebo.Multiplayer;

public class SteamManager
{
    public const uint steam_appid = 480;

    private static CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;
    private static CallResult<LeaderboardFindResult_t> m_callResultFindLeaderboard;
    private static Callback<PersonaStateChange_t> m_PersonaStateChange;
    private static Callback<UserStatsReceived_t> m_UserStatsReceived;

    public static bool IsSteamRunning { get; set; } = false;

    private SteamManager() {}

    public static bool Init()
    {
        try
        {
            if(!SteamAPI.Init())
            {
                Console.WriteLine("SteamAPI.Init() failed!");
                return false;
            }
        }
        catch(DllNotFoundException e) // We check this here as it will be the first instance of it.
        {
            Console.WriteLine(e);
            return false;
        }

        if(!Packsize.Test())
        {
            Console.WriteLine("You're using the wrong Steamworks.NET Assembly for this platform!");
            return false;
        }

        if(!DllCheck.Test())
        {
            Console.WriteLine("You're using the wrong dlls for this platform!");
            return false;
        }

        IsSteamRunning = true;

        InitializeCallbacks(); // We do this after SteamAPI.Init() has occured

        Console.WriteLine("Requesting Current Stats - " + SteamUserStats.RequestCurrentStats());

        Console.WriteLine("CurrentGameLanguage: " + SteamApps.GetCurrentGameLanguage());
        Console.WriteLine("PersonaName: " + SteamFriends.GetPersonaName());

        {
            uint length = SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out string folder, 260);
            Console.WriteLine("AppInstallDir: " + length + " " + folder);
        }

        m_NumberOfCurrentPlayers.Set(SteamUserStats.GetNumberOfCurrentPlayers());
        Console.WriteLine("Requesting Number of Current Players");

        {
            SteamAPICall_t hSteamAPICall = SteamUserStats.FindLeaderboard("Quickest Win");
            m_callResultFindLeaderboard.Set(hSteamAPICall);
            Console.WriteLine("Requesting Leaderboard");
        }

        return true;
    }

    private static void InitializeCallbacks()
    {
        m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
        m_callResultFindLeaderboard = CallResult<LeaderboardFindResult_t>.Create(OnFindLeaderboard);
        m_PersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
        m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(
            (pCallback) => {
                Console.WriteLine("[" + UserStatsReceived_t.k_iCallback + " - UserStatsReceived] - " + pCallback.m_eResult + " -- " + pCallback.m_nGameID + " -- " + pCallback.m_steamIDUser);
            });
    }

    private static void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure) {
        Console.WriteLine("[" + NumberOfCurrentPlayers_t.k_iCallback + " - NumberOfCurrentPlayers] - " + pCallback.m_bSuccess + " -- " + pCallback.m_cPlayers);
    }

    private static void OnFindLeaderboard(LeaderboardFindResult_t pCallback, bool bIOFailure) {
        Console.WriteLine("[" + LeaderboardFindResult_t.k_iCallback + " - LeaderboardFindResult] - " + pCallback.m_bLeaderboardFound + " -- " + pCallback.m_hSteamLeaderboard);
    }

    private static void OnPersonaStateChange(PersonaStateChange_t pCallback) {
        Console.WriteLine("[" + PersonaStateChange_t.k_iCallback + " - PersonaStateChange] - " + pCallback.m_ulSteamID + " -- " + pCallback.m_nChangeFlags);
    }
}
