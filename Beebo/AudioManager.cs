using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace Beebo;

public static class AudioManager
{
    static readonly List<SoundEffectInstance> instances = [];

    public static SoundEffectInstance Play(SoundEffect soundEffect, bool loopPlayback, float volume, float pitch, float pan)
    {
        var instance = soundEffect.CreateInstance();
        instance.IsLooped = loopPlayback;
        instance.Volume = volume;
        instance.Pitch = pitch;
        instance.Pan = pan;
        instance.Play();

        instances.Add(instance);

        return instance;
    }

    public static void PauseAll()
    {
        for(int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if(instance == null || instance.IsDisposed)
            {
                instances.RemoveAt(i);
                i--;
                continue;
            }

            if(instance.State == SoundState.Playing)
                instance.Pause();
        }
    }

    public static void ResumeAll()
    {
        for(int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if(instance == null || instance.IsDisposed)
            {
                instances.RemoveAt(i);
                i--;
                continue;
            }

            if(instance.State == SoundState.Paused)
                instance.Resume();
        }
    }

    public static void CleanDisposed()
    {
        for(int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if(instance == null || instance.IsDisposed)
            {
                instances.RemoveAt(i);
                i--;
                continue;
            }
        }
    }

    public static void ClearAll()
    {
        instances.Clear();
    }
}
