using System;
using System.Collections.Generic;
using System.Linq;

namespace Runavelle.Core
{
    public enum InputAction { MoveUp, MoveDown, MoveLeft, MoveRight, Ultimate, Dash, Pause, Speed, StartNight }

    /// <summary>
    /// P9 keyboard remapping, engine-free: action → key names (Unity KeyCode names). Gamepad buttons and touch controls
    /// stay fixed. Stored in Profile.Settings["keys"]. Rebinding a key that another action uses moves it (never two
    /// actions on one key), and an action can never be left without a key.
    /// </summary>
    public sealed class InputBindings
    {
        public static readonly Dictionary<InputAction, string[]> Defaults = new Dictionary<InputAction, string[]>
        {
            [InputAction.MoveUp] = new[] { "W", "UpArrow" },
            [InputAction.MoveDown] = new[] { "S", "DownArrow" },
            [InputAction.MoveLeft] = new[] { "A", "LeftArrow" },
            [InputAction.MoveRight] = new[] { "D", "RightArrow" },
            [InputAction.Ultimate] = new[] { "Space", "Q" },
            [InputAction.Dash] = new[] { "LeftShift", "E" },
            [InputAction.Pause] = new[] { "Escape", "P" },
            [InputAction.Speed] = new[] { "F", "Tab" },
            [InputAction.StartNight] = new[] { "Return", "N" },
        };

        /// <summary>Keys that keep a fixed meaning elsewhere (menus / undo chord) and cannot be bound.</summary>
        public static readonly HashSet<string> Reserved = new HashSet<string> { "Mouse0", "Mouse1", "Mouse2", "LeftControl", "LeftCommand", "Z" };

        private readonly Dictionary<InputAction, List<string>> _keys = new Dictionary<InputAction, List<string>>();

        /// <summary>Bumped on every change, so input code can cache resolved key codes without per-frame allocations.</summary>
        public int Version { get; private set; }

        public InputBindings() => ResetDefaults();

        public void ResetDefaults()
        {
            _keys.Clear();
            foreach (var kv in Defaults) _keys[kv.Key] = new List<string>(kv.Value);
            Version++;
        }

        public IReadOnlyList<string> Keys(InputAction a) => _keys[a];

        public InputAction? ActionOf(string key)
        {
            foreach (var kv in _keys) if (kv.Value.Contains(key)) return kv.Key;
            return null;
        }

        /// <summary>Sets the primary key of an action. Returns the action that lost the key (if any) or null.</summary>
        public InputAction? Rebind(InputAction a, string key, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(key) || Reserved.Contains(key)) { error = "Phím này dành riêng"; return null; }
            InputAction? displaced = null;
            foreach (var kv in _keys)
            {
                if (kv.Key == a || !kv.Value.Contains(key)) continue;
                if (kv.Value.Count == 1) { error = "Phím đang là phím duy nhất của " + kv.Key; return null; } // never leave an action unbound
                kv.Value.Remove(key);
                displaced = kv.Key;
            }
            var list = _keys[a];
            list.Remove(key);
            if (list.Count > 0) list[0] = key; else list.Add(key);
            Version++;
            return displaced;
        }

        public string Serialize() => string.Join(";", _keys.OrderBy(k => (int)k.Key).Select(k => k.Key + "=" + string.Join(",", k.Value)));

        /// <summary>Unknown actions/empty lists are ignored; missing actions keep defaults (forward/backward compatible).</summary>
        public static InputBindings Parse(string s)
        {
            var b = new InputBindings();
            if (string.IsNullOrEmpty(s)) return b;
            foreach (var part in s.Split(';'))
            {
                var kv = part.Split('=');
                if (kv.Length != 2 || !Enum.TryParse(kv[0], out InputAction a)) continue;
                var keys = kv[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Where(k => !Reserved.Contains(k)).Distinct().ToList();
                if (keys.Count > 0) b._keys[a] = keys;
            }
            // de-duplicate across actions (first action keeps a shared key) without unbinding anything
            var seen = new HashSet<string>();
            foreach (var a in b._keys.Keys.OrderBy(x => (int)x).ToList())
            {
                var kept = b._keys[a].Where(k => seen.Add(k)).ToList();
                b._keys[a] = kept.Count > 0 ? kept : new List<string>(Defaults[a].Where(k => !seen.Contains(k)).Take(1));
            }
            b.Version++;
            return b;
        }
    }
}
