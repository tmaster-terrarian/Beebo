using System;
using Beebo.MultiplayerTest;
using Beebo.Net;
using Jelly;

using Steamworks;

namespace Beebo;

public static class SteamManager
{
    public const uint steam_appid = 480;

    public static AppId_t AppID => (AppId_t)steam_appid;

    public static Logger Logger { get; } = new("Steamworks.NET");

    public static bool IsSteamRunning { get; set; } = false;

    public static bool Init(bool server = false)
    {
        try
        {
            if(!SteamAPI.Init())
            {
                Logger.Error("SteamAPI.Init() failed!");
                return false;
            }
        }
        catch(DllNotFoundException e) // We check this here as it will be the first instance of it.
        {
            Logger.Error(e);
            return false;
        }

        if(!Packsize.Test())
        {
            Logger.Error("You're using the wrong Steamworks.NET Assembly for this platform!");
            return false;
        }

        if(!DllCheck.Test())
        {
            Logger.Error("You're using the wrong dlls for this platform!");
            return false;
        }

        IsSteamRunning = true;

        InitializeCallbacks(); // We do this after SteamAPI.Init() has occured

        // Logger.Info("Requesting Current Stats - " + SteamUserStats.RequestCurrentStats());

        // Logger.Info("CurrentGameLanguage: " + SteamApps.GetCurrentGameLanguage());
        // Logger.Info("PersonaName: " + SteamFriends.GetPersonaName());

        {
            uint length = SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out string folder, 260);
            Logger.Info("Steam AppInstallDir: " + length + " " + folder);
        }

        // Logger.Info("AppDir: " + Main.ProgramPath.Length + " " + Main.ProgramPath);

        // SteamCallbacks.m_NumberOfCurrentPlayers.Set(SteamUserStats.GetNumberOfCurrentPlayers());
        // Logger.Info("Requesting Number of Current Players");

        // SteamCallbacks.m_CallResultFindLeaderboard.Set(SteamUserStats.FindLeaderboard("Quickest Win"));
        // Logger.Info("Requesting Leaderboard");

        return true;
    }

    public static void Cleanup()
    {
        Logger.Info("Shutting down Steam...");
        SteamAPI.Shutdown();
    }

    public static void Update()
    {
        
    }

    private static void InitializeCallbacks()
    {
        // SteamCallbacks.Initialize();
        P2PManager.InitializeCallbacks();
    }
}

public enum ConnectionLostReason
{
    TooManyPlayers = 1000,
    CancelledByClient = 1001
}
