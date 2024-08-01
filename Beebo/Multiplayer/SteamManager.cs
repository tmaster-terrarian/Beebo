using System;
using System.Collections.Generic;
using Jelly;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Beebo.Multiplayer;

public static class SteamManager
{
    public const uint steam_appid = 480;

    public static AppId_t AppID => (AppId_t)steam_appid;

    public static Logger Logger { get; } = new("Steamworks.NET");

    private static readonly List<LobbyMember> Players = [];

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

        Logger.Info("Requesting Current Stats - " + SteamUserStats.RequestCurrentStats());

        Logger.Info("CurrentGameLanguage: " + SteamApps.GetCurrentGameLanguage());
        Logger.Info("PersonaName: " + SteamFriends.GetPersonaName());

        {
            uint length = SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out string folder, 260);
            Logger.Info("Steam AppInstallDir: " + length + " " + folder);
        }

        Logger.Info("AppDir: " + Main.ProgramPath.Length + " " + Main.ProgramPath);

        SteamCallbacks.m_NumberOfCurrentPlayers.Set(SteamUserStats.GetNumberOfCurrentPlayers());
        Logger.Info("Requesting Number of Current Players");

        SteamCallbacks.m_CallResultFindLeaderboard.Set(SteamUserStats.FindLeaderboard("Quickest Win"));
        Logger.Info("Requesting Leaderboard");

        return true;
    }

    public static void Cleanup()
    {
        Logger.Info("Shutting down Steam...");
        SteamAPI.Shutdown();
    }

    public static void Update()
    {
        if(Main.IsOnline && Input.GetDown(Keys.F1))
        {
            Main.IsClient = false;
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4);
        }
    }

    private static void InitializeCallbacks()
    {
        SteamCallbacks.Initialize();
    }

    // private static void OnSteamNetConnectionStatusChangedCallback(SteamNetConnectionStatusChangedCallback_t pCallback)
    // {
    //     if(!Main.IsClient)
    //     {
    //         if(pCallback.m_info.m_hListenSocket == ListenSocket)
    //         {
    //             if(pCallback.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None
    //             && pCallback.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
    //             {
    //                 var id = pCallback.m_info.m_identityRemote.GetSteamID();

    //                 Logger.Info("Receiving connection attempt from user " + id + " (" + SteamFriends.GetFriendPersonaName(id) + ")");

    //                 if(Players.Count >= 4)
    //                 {
    //                     if(SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, (int)ConnectionLostReason.TooManyPlayers, "Max player count reached.", false))
    //                     {
    //                         Logger.Info("Booted " + id + " from server: TooManyPlayers");
    //                     }
    //                     return;
    //                 }

    //                 var result = SteamNetworkingSockets.AcceptConnection(pCallback.m_hConn);

    //                 Players.Add(new(id));

    //                 Logger.Info($"[{SteamNetConnectionStatusChangedCallback_t.k_iCallback} - SteamNetConnectionStatusChangedCallback] - {id} -- {result}");
    //             }
    //         }
    //     }
    // }
}

public enum ConnectionLostReason
{
    TooManyPlayers = 1000,
    CancelledByClient = 1001
}
