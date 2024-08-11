using System;

using Jelly.GameContent;
using Jelly.Net;

namespace Jelly;

public static class Providers
{
    private static bool _initialized;

    private static NetworkProvider _networkProvider;

    public static NetworkProvider NetworkProvider
    {
        get {
            CheckInitialized();
            return _networkProvider;
        }
    }

    private static ContentProvider _contentProvider;

    public static ContentProvider ContentProvider
    {
        get {
            CheckInitialized();
            return _contentProvider;
        }
    }

    private static float deltaTime;

    public static float DeltaTime => deltaTime;

    /// <summary>
    /// This method should called before calling any other update methods related to <see cref="Jelly"/>
    /// </summary>
    public static void SetDeltaTime(float value)
    {
        deltaTime = value;
    }

    /// <summary>
    /// Sets up the main providers and engine backend utilities. Call once at program start.
    /// </summary>
    public static void Initialize(NetworkProvider networkProvider, ContentProvider contentProvider)
    {
        ArgumentNullException.ThrowIfNull(networkProvider, nameof(networkProvider));

        if(_initialized) throw new InvalidOperationException(nameof(Initialize) + " cannot be called more than once!");
        _initialized = true;

        _networkProvider = networkProvider;
        _contentProvider = contentProvider;

        Registries.Init();
    }

    private static void CheckInitialized()
    {
        if(!_initialized)
        {
            throw new InvalidOperationException(nameof(Initialize) + " has not been called yet!");
        }
    }
}
