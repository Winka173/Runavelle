using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// P6 smart camera without hijacking: urgent events off-screen (breach, castle hit, boss, AI general down) show an
    /// arrow at the screen edge. The camera only moves when the player taps one (a 2.5 s peek), and any movement input ends the peek.
    /// </summary>
    public sealed partial class GameRoot
    {
        private struct Urgent { public Vector2 Pos; public float Time; public string Label; public Color Col; }
        private readonly List<Urgent> _urgent = new List<Urgent>();
        private Vector2 _peek;
        private float _peekUntil = -1f;
        private float _castleUrgentAt = -99f;

        private void RecordUrgent(Vector2 pos, string label, Color col)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _urgent.Count; i++)
                if ((_urgent[i].Pos - pos).sqrMagnitude < 9f) { _urgent[i] = new Urgent { Pos = pos, Time = now, Label = label, Col = col }; return; }
            _urgent.Add(new Urgent { Pos = pos, Time = now, Label = label, Col = col });
            if (_urgent.Count > 6) _urgent.RemoveAt(0);
        }

        private void RecordCastleUrgent(Vector2 pos)
        {
            if (Time.unscaledTime - _castleUrgentAt < 4f) return;
            _castleUrgentAt = Time.unscaledTime;
            RecordUrgent(pos, "Lâu Đài bị tấn công", new Color(1f, 0.35f, 0.25f));
        }

        /// <summary>True while a player-requested peek is active (camera eases to it; general control is untouched).</summary>
        private bool Peeking(out Vector2 at)
        {
            at = _peek;
            if (_peekUntil < 0f) return false;
            if (Time.unscaledTime > _peekUntil || Input.Move.sqrMagnitude > 0.04f) { _peekUntil = -1f; return false; }
            return true;
        }

        private void DrawUrgentEdges()
        {
            if (Session == null || Session.Phase != RunPhase.Night || Rig?.Cam == null) return;
            float now = Time.unscaledTime;
            _urgent.RemoveAll(u => now - u.Time > 6f);
            foreach (var u in _urgent)
            {
                var vp = Rig.Cam.WorldToViewportPoint(new Vector3(u.Pos.x, u.Pos.y, 0));
                if (vp.x > 0.06f && vp.x < 0.94f && vp.y > 0.06f && vp.y < 0.94f) continue; // on screen: no arrow
                var d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                float k = 0.44f / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y));
                var e = new Vector2(0.5f + d.x * k, 0.5f + d.y * k);
                float gx = Gui.Safe.x + e.x * Gui.Safe.width, gy = Gui.Safe.y + (1f - e.y) * Gui.Safe.height;
                var r = new Rect(gx - 120, gy - 34, 240, 68);
                float pulse = Settings.ReducedMotion ? 1f : 0.75f + 0.25f * Mathf.Sin(now * 6f);
                var col = new Color(u.Col.r, u.Col.g, u.Col.b, pulse);
                string arrow = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x > 0 ? ">>" : "<<") : (d.y > 0 ? "^^" : "vv"); // font has no arrow glyphs
                if (Gui.Btn(r, arrow + " " + u.Label, true, Gui.ButtonSmall)) { _peek = u.Pos; _peekUntil = now + 2.5f; }
                Gui.Rect(new Rect(r.x, r.yMax - 6, r.width, 6), col);
            }
        }
    }
}
