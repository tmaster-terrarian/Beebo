using System.IO;

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Jelly.GameContent;

namespace Beebo.GameContent;

public sealed class AudioDef : RegistryEntry
{
    public string FilePath { get; set; }

    public bool IsLooped { get; set; } = false;

    public float Pan { get; set; } = 0;

    public float Pitch { get; set; } = 0;

    public float Volume { get; set; } = 1;

    public SoundEffect SoundEffect { get; set; }

    public SoundEffectInstance Play(bool loopPlayback = false, float? volume = null, float? pitch = null, float? pan = null)
    {
        // var instance = SoundEffect.CreateInstance();
        // instance.IsLooped = loopPlayback || IsLooped;
        // instance.Volume = (volume ?? 1) * Volume;
        // instance.Pitch = pitch ?? Pitch;
        // instance.Pan = pan ?? Pan;
        // instance.Play();

        // return instance;

        return AudioManager.Play(SoundEffect, loopPlayback || IsLooped, (volume ?? 1) * Volume, pitch ?? Pitch, pan ?? Pan);
    }
}

public class AudioRegistry : Registry<AudioDef>
{
    public override void Init()
    {
        #region player

        Register(new() {
            Name = "player_jump",
            Volume = 0.3f
        });
        Register(new() {
            Name = "player_wall_jump",
            Volume = 0.7f
        });
        Register(new() {
            Name = "player_land",
            Volume = 0.25f
        });
        Register(new() {
            Name = "player_shoot",
            Volume = 0.75f
        });
        Register(new() {
            Name = "player_throw_bomb",
            Volume = 0.4f
        });

        #endregion

        #region bomb

        Register(new() {
            Name = "bomb_bounce",
            Volume = 0.7f
        });
        Register(new() {
            Name = "bomb_explosion",
            Volume = 0.5f
        });
        Register(new() {
            Name = "bomb_throw",
            Volume = 0.5f
        });

        #endregion
    }

    public static void LoadContent(ContentManager content)
    {
        foreach(var def in Registries.Get<AudioRegistry>())
        {
            def.Value.SoundEffect = content.Load<SoundEffect>(def.Value.FilePath ?? $"Audio/{def.Key}");
        }
    }
}
