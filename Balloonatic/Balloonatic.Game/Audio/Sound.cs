using System.Collections.Generic;
using System.IO;
using Balloonatic.Core;
using Microsoft.Xna.Framework.Audio;

namespace Balloonatic.Audio
{
    /// <summary>
    /// Every sound the game makes (I11), built on three promises.
    ///
    /// <b>Mute first.</b> Nothing loads until the player has actually touched something (browsers
    /// suspend audio before a gesture anyway, and a headless test bed that never gestures never
    /// touches the audio stack at all). From then on every call is volume-gated and try/caught —
    /// a missing clip, a headless head, or a browser that simply will not play must never throw,
    /// and any failure retires the whole kit rather than spamming a broken call every frame.
    ///
    /// <b>Old-tech instruments.</b> The clips are the square/triangle/noise kit from
    /// <c>audio/.audio/generate.js</c>, embedded like the sprite sheet so both heads read the same
    /// bytes from the same stream.
    ///
    /// <b>Volumes live in settings.</b> <see cref="ApplyVolumes"/> re-echoes the menu sliders into
    /// the playing instances; pausing the game pauses the music, as the sim clock already does.
    /// </summary>
    static class Sound
    {
        static readonly string[] ClipNames = { "burner", "creak", "chime", "giggle", "jingle", "music", "thump" };

        static readonly Dictionary<string, SoundEffect> Clips = new();
        static readonly Queue<(string Name, int FramesLeft)> Queued = new();

        static SoundEffectInstance? _music;
        static SoundEffectInstance? _burner;
        static bool _unlocked;      // a real gesture happened — audio may exist
        static bool _broken;        // any audio failure retires the kit for the session
        static bool _musicWanted;   // remembered across pause (screen) and volume (settings) changes
        static int _musicLevel = -1, _soundLevel = -1;

        public static bool Unlocked => _unlocked;

        /// <summary>
        /// Called by the game loop the first moment a pointer press or a key answers — the browser's
        /// definition of "the player meant to be here". Loading happens here, not at boot: the boot
        /// path must stay clean for a test bed that only scripts the harness.
        /// </summary>
        public static void Unlock()
        {
            if (_unlocked || _broken) return;
            _unlocked = true;
            try
            {
                foreach (string name in ClipNames)
                {
                    using Stream stream = typeof(Sound).Assembly.GetManifestResourceStream(ResourceName(name))!;
                    Clips[name] = SoundEffect.FromStream(stream);
                }
                _music = Clips["music"].CreateInstance();
                _music.IsLooped = true;
                _burner = Clips["burner"].CreateInstance();
                _burner.IsLooped = true;
                ApplyVolumes();
                if (_musicWanted) _music.Play();
            }
            catch (System.Exception)
            {
                _broken = true;
            }
        }

        static string ResourceName(string clip) => "Balloonatic.content.audio." + clip + ".wav";

        /// <summary>Independent 0..10 buses, normalized once for every audio backend.</summary>
        public static void ApplyVolumes()
        {
            if (_broken || !_unlocked) return;
            if (_musicLevel == AppSettings.MusicVolume && _soundLevel == AppSettings.SoundVolume) return;
            float music = AppSettings.MusicVolume / (float)AppSettings.VolumeMax;
            float effects = AppSettings.SoundVolume / (float)AppSettings.VolumeMax;
            try
            {
                if (_music != null) _music.Volume = music;
                if (_burner != null) _burner.Volume = effects * 0.6f;
                _musicLevel = AppSettings.MusicVolume;
                _soundLevel = AppSettings.SoundVolume;
            }
            catch (System.Exception) { _broken = true; }
        }

        /// <summary>Music plays everywhere except behind the pause veil, which holds the world still.</summary>
        public static void SetMusic(bool on)
        {
            _musicWanted = on;
            if (_broken || !_unlocked || _music == null) return;
            try
            {
                if (on && AppSettings.MusicVolume > 0) { if (_music.State != SoundState.Playing) _music.Play(); }
                else _music.Stop();
            }
            catch (System.Exception) { _broken = true; }
        }

        /// <summary>The burner breathes while the sim says heat is on — the only sound that is a state, not an event.</summary>
        public static void SetBurner(bool on)
        {
            on = on && AppSettings.SoundVolume > 0;
            if (_broken || !_unlocked || _burner == null) return;
            try
            {
                if (on) { if (_burner.State != SoundState.Playing) _burner.Play(); }
                else _burner.Stop();
            }
            catch (System.Exception) { _broken = true; }
        }

        /// <summary>One-shot, sfx-gated. Silent before unlock or with the sfx slider at rest.</summary>
        public static void Play(string clip)
        {
            if (_broken || !_unlocked || AppSettings.SoundVolume == 0) return;
            try
            {
                if (Clips.TryGetValue(clip, out SoundEffect? fx))
                    fx.Play(AppSettings.SoundVolume / (float)AppSettings.VolumeMax, 0f, 0f);
            }
            catch (System.Exception) { _broken = true; }
        }

        /// <summary>A one-shot some frames from now — the star chimes ring one after the jingle lands.</summary>
        public static void PlaySoon(string clip, int frames) => Queued.Enqueue((clip, frames));

        /// <summary>One line the audio smoke test reads: what woke, what broke, what plays.</summary>
        public static string Status()
        {
            string music = _music?.State.ToString().ToLowerInvariant() ?? "none";
            string burner = _burner?.State.ToString().ToLowerInvariant() ?? "none";
            return "unlocked=" + (_unlocked ? 1 : 0)
                + ";broken=" + (_broken ? 1 : 0)
                + ";music=" + music
                + ";burner=" + burner;
        }

        public static string MixStatus() =>
            "music=" + (_music?.Volume ?? 0f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            + ";sfx=" + (AppSettings.SoundVolume / (float)AppSettings.VolumeMax).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            + ";burner=" + (_burner?.Volume ?? 0f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Advance the small delayed queue; called once per Update.</summary>
        public static void Tick()
        {
            ApplyVolumes();
            for (int i = Queued.Count - 1; i >= 0; i--)
            {
                (string name, int left) = Queued.Dequeue();
                if (left <= 1) Play(name);
                else Queued.Enqueue((name, left - 1));
            }
        }
    }
}
