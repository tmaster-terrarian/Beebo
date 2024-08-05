using System;
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

        Initialize();

        {
            Logger.Info("Steam AppInstallDir: " + SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out string folder, 260) + " " + folder);
            Logger.Info("ProgramPath: " + Main.ProgramPath.Length + " " + Main.ProgramPath);
        }

        return true;
    }

    public static void Cleanup()
    {
        P2PManager.LeaveLobby();
        Logger.Info("Shutting down Steam...");
        SteamAPI.Shutdown();
    }

    private static void Initialize()
    {
        P2PManager.InitializeCallbacks();
    }
}
