using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>Player settings persisted in the offline profile. Presentation-only: never changes simulation rules.</summary>
    public sealed class GameSettings
    {
        public int VfxLevel = 2;            // 0 low, 1 medium, 2 high
        public bool DamageNumbers = true;
        public bool ScreenShake = true;
        public bool ReducedFlashes;
        public bool ElementGlyphs = true;   // colour-blind safe shape icons on statuses
        public float Master = 0.9f, Music = 0.6f, Sfx = 0.8f;
        public float UiScale = 1f;
        public int Quality = 2;
        public bool TouchControls;          // forced on for mobile platforms
        public bool FastDay;

        public void Load(Profile p)
        {
            string Get(string k, string d) => p.Settings.TryGetValue(k, out var v) && v != null ? v : d;
            VfxLevel = int.Parse(Get("vfx", "2"));
            DamageNumbers = Get("dmg", "1") == "1";
            ScreenShake = Get("shake", "1") == "1";
            ReducedFlashes = Get("flash", "0") == "1";
            ElementGlyphs = Get("glyph", "1") == "1";
            Master = float.Parse(Get("master", "0.9"), System.Globalization.CultureInfo.InvariantCulture);
            Music = float.Parse(Get("music", "0.6"), System.Globalization.CultureInfo.InvariantCulture);
            Sfx = float.Parse(Get("sfx", "0.8"), System.Globalization.CultureInfo.InvariantCulture);
            UiScale = float.Parse(Get("ui", "1"), System.Globalization.CultureInfo.InvariantCulture);
            Quality = int.Parse(Get("quality", Application.isMobilePlatform ? "1" : "2"));
            TouchControls = Get("touch", Application.isMobilePlatform ? "1" : "0") == "1";
            FastDay = Get("fastday", "0") == "1";
        }

        public void Save(Profile p)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            p.Settings["vfx"] = VfxLevel.ToString();
            p.Settings["dmg"] = DamageNumbers ? "1" : "0";
            p.Settings["shake"] = ScreenShake ? "1" : "0";
            p.Settings["flash"] = ReducedFlashes ? "1" : "0";
            p.Settings["glyph"] = ElementGlyphs ? "1" : "0";
            p.Settings["master"] = Master.ToString(ci);
            p.Settings["music"] = Music.ToString(ci);
            p.Settings["sfx"] = Sfx.ToString(ci);
            p.Settings["ui"] = UiScale.ToString(ci);
            p.Settings["quality"] = Quality.ToString();
            p.Settings["touch"] = TouchControls ? "1" : "0";
            p.Settings["fastday"] = FastDay ? "1" : "0";
        }

        public void Apply()
        {
            Gui.UiScaleSetting = UiScale;
            AudioListener.volume = Master;
            Application.targetFrameRate = Quality == 0 ? 30 : 60;
            QualitySettings.vSyncCount = 0;
        }
    }

    /// <summary>
    /// Pooled one-shot audio with per-clip voice limiting (never 50 identical arrow sounds at once) and day/night music.
    /// Clips are synthesized offline by Tools/Audio/build_audio.py into Resources/Audio.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private static AudioService _i;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _active = new Dictionary<string, int>();
        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private AudioSource _musicA, _musicB;
        private string _musicKey;
        private float _musicFade = 1f;
        public GameSettings Settings;

        public static void Init(GameObject host, GameSettings s)
        {
            _i = host.AddComponent<AudioService>();
            _i.Settings = s;
            for (int k = 0; k < 16; k++) _i._pool.Add(_i.gameObject.AddComponent<AudioSource>());
            _i._musicA = _i.gameObject.AddComponent<AudioSource>();
            _i._musicB = _i.gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { _i._musicA, _i._musicB }) { m.loop = true; m.volume = 0f; }
        }

        private AudioClip Clip(string key)
        {
            if (_clips.TryGetValue(key, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + key);
            _clips[key] = c;
            return c;
        }

        public static void Ui() => Play("ui_click", 0.7f, 2, 0.04f);

        /// <summary>Play with a cap on simultaneous voices and a minimum re-trigger interval per clip.</summary>
        public static void Play(string key, float volume = 1f, int maxVoices = 3, float minInterval = 0.06f, float pitchJitter = 0.06f)
        {
            if (_i == null) return;
            var clip = _i.Clip(key);
            if (clip == null) return;
            float now = Time.unscaledTime;
            if (_i._lastPlayed.TryGetValue(key, out var last) && now - last < minInterval) return;
            int active = 0;
            foreach (var s in _i._pool) if (s.isPlaying && s.clip == clip) active++;
            if (active >= maxVoices) return;
            foreach (var s in _i._pool)
            {
                if (s.isPlaying) continue;
                s.clip = clip;
                s.volume = volume * _i.Settings.Sfx;
                s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
                s.Play();
                _i._lastPlayed[key] = now;
                return;
            }
        }

        public static void Music(string key)
        {
            if (_i == null || _i._musicKey == key) return;
            _i._musicKey = key;
            var clip = _i.Clip(key);
            var tmp = _i._musicA; _i._musicA = _i._musicB; _i._musicB = tmp; // A = new
            _i._musicA.clip = clip;
            _i._musicA.volume = 0f;
            if (clip != null) _i._musicA.Play();
            _i._musicFade = 0f;
        }

        private void Update()
        {
            _musicFade = Mathf.MoveTowards(_musicFade, 1f, Time.unscaledDeltaTime / 1.5f);
            float target = Settings.Music * 0.55f;
            _musicA.volume = target * _musicFade;
            _musicB.volume = target * (1f - _musicFade);
            if (_musicFade >= 1f && _musicB.isPlaying) _musicB.Stop();
        }
    }
}
