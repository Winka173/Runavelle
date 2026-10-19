using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// Loads procedurally generated sprites from Resources/Art using the pivots/PPU in Art/manifest.json
    /// (written by Tools/Art/build_art.py). Missing keys fall back to a visible magenta debug square.
    /// </summary>
    public static class ArtLibrary
    {
        private struct Entry { public float Px, Py, Ppu; }

        private static Dictionary<string, Entry> _manifest;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite _missing;
        public static int MissingCount { get; private set; }

        private static void EnsureManifest()
        {
            if (_manifest != null) return;
            _manifest = new Dictionary<string, Entry>();
            var ta = Resources.Load<TextAsset>("Art/manifest");
            if (ta == null) { Debug.LogWarning("[Runavelle] Art/manifest.json missing - run Tools/Art/build_art.py"); return; }
            var root = MiniJson.Parse(ta.text) as Dictionary<string, object>;
            foreach (var kv in root)
                _manifest[kv.Key] = new Entry { Px = MiniJson.Float(kv.Value, "px", 0.5f), Py = MiniJson.Float(kv.Value, "py", 0.5f), Ppu = MiniJson.Float(kv.Value, "ppu", 64f) };
        }

        public static bool Has(string key)
        {
            EnsureManifest();
            return _manifest.ContainsKey(key);
        }

        public static Sprite Get(string key)
        {
            if (key == null) return Missing();
            if (Cache.TryGetValue(key, out var s)) return s;
            EnsureManifest();
            var tex = Resources.Load<Texture2D>("Art/" + key);
            if (tex == null || !_manifest.TryGetValue(key, out var e))
            {
                MissingCount++;
                Debug.LogWarning("[Runavelle] missing sprite " + key);
                Cache[key] = Missing();
                return Cache[key];
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(e.Px, e.Py), e.Ppu, 0, SpriteMeshType.FullRect);
            s.name = key;
            Cache[key] = s;
            return s;
        }

        /// <summary>First key that exists, else the last candidate (which reports missing).</summary>
        public static Sprite First(params string[] keys)
        {
            EnsureManifest();
            foreach (var k in keys) if (_manifest.ContainsKey(k)) return Get(k);
            return Get(keys[keys.Length - 1]);
        }

        public static Texture2D Tex(string key)
        {
            var s = Get(key);
            return s != null ? s.texture : null;
        }

        private static Sprite Missing()
        {
            if (_missing != null) return _missing;
            var t = new Texture2D(8, 8);
            var px = new Color[64];
            for (int i = 0; i < 64; i++) px[i] = new Color(1f, 0f, 1f, 1f);
            t.SetPixels(px);
            t.Apply();
            _missing = Sprite.Create(t, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
            return _missing;
        }

        // content -> sprite keys live in Core (shared with the headless snapshot renderer)
        public static string BuildingKey(Building b, int wallMask) => ArtKeys.Building(b, wallMask);
        public static string LevelPreviewKey(BuildingLevel l, ContentDatabase db) => ArtKeys.LevelPreview(l, db);
        public static string UnitKey(Unit u, bool attackFrame) => ArtKeys.Unit(u, attackFrame);

        public static string ElementIcon(ElementId e) => "icon/el_" + Elements.Info(e).Key;

        public static Color ElementColor(ElementId e, float a = 1f)
        {
            var i = Elements.Info(e);
            return new Color(i.R, i.G, i.B, a);
        }
    }
}
