using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum HighlightKind { BossDefeat, EliteDefeat, WallBreach, BigReaction, Ultimate, CloseCall, MultiKill, GeneralDown, BossPhase }

    public struct Highlight
    {
        public int Night;
        public float Time;
        public HighlightKind Kind;
        public int Score;
        public Vec2 Position;
        public string Text;
    }

    /// <summary>
    /// Battle Highlights: a fixed-size ring buffer of tagged event records (time, place, kind, score) — an event
    /// timeline plus camera targets, NOT a video or deterministic replay. After a night the best few moments are shown
    /// as an honest summary with "jump camera here".
    /// </summary>
    public sealed class HighlightRecorder
    {
        public const int Capacity = 96;
        public const float MinSpacing = 3f;
        private readonly Highlight[] _ring = new Highlight[Capacity];
        private int _next, _count;
        private float _killWindowStart = -99f;
        private int _killsInWindow;
        private bool _closeCallFired;
        private int _night = -1;

        public int Count => _count;

        private void Add(Highlight h)
        {
            _ring[_next] = h;
            _next = (_next + 1) % Capacity;
            _count = Math.Min(Capacity, _count + 1);
        }

        public void Observe(in SimEvent e, World w, float nightTime)
        {
            if (!w.IsNight) return;
            if (w.Night != _night) { _night = w.Night; _closeCallFired = false; _killsInWindow = 0; }
            switch (e.Type)
            {
                case SimEventType.Kill:
                {
                    var u = w.Get(e.ActorId) as Unit;
                    if (u != null && u.Team == Team.Enemy)
                    {
                        if (u.IsBoss) Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.BossDefeat, Score = 100, Position = e.Position, Text = "Hạ " + u.Def.Name });
                        else if (u.IsElite) Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.EliteDefeat, Score = 40, Position = e.Position, Text = "Hạ tinh anh " + u.Def.Name });
                        if (nightTime - _killWindowStart > 2f) { _killWindowStart = nightTime; _killsInWindow = 0; }
                        if (++_killsInWindow == 6) Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.MultiKill, Score = 45, Position = e.Position, Text = "Quét sạch 6 kẻ địch trong 2 giây" });
                    }
                    break;
                }
                case SimEventType.WallBreached:
                    Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.WallBreach, Score = 50, Position = e.Position, Text = "Tường thủng: " + e.Text });
                    break;
                case SimEventType.UltimateCast:
                    Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.Ultimate, Score = 55, Position = e.Position, Text = "Tối thượng: " + e.Text });
                    break;
                case SimEventType.BossPhase:
                    Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.BossPhase, Score = 35, Position = e.Position, Text = e.Text });
                    break;
                case SimEventType.GeneralDowned:
                    Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.GeneralDown, Score = 30, Position = e.Position, Text = e.Text + " gục ngã" });
                    break;
                case SimEventType.Reaction:
                    if (e.Text != "arc" && e.Text != "chain" && ElementGrammar.For(e.Reaction).Weight >= 2)
                        Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.BigReaction, Score = 25, Position = e.Position, Text = "Phản ứng " + e.Text });
                    break;
                case SimEventType.Hit:
                    if (!_closeCallFired && w.Castle != null && e.ActorId == w.Castle.Id && w.Castle.HpFraction < 0.25f && !w.Castle.Dead)
                    {
                        _closeCallFired = true;
                        Add(new Highlight { Night = w.Night, Time = nightTime, Kind = HighlightKind.CloseCall, Score = 70, Position = e.Position, Text = "Lâu Đài còn dưới 25% máu!" });
                    }
                    break;
            }
        }

        /// <summary>Best moments of a night: highest score first, at least MinSpacing seconds apart, then in time order.</summary>
        public List<Highlight> Top(int night, int n)
        {
            var all = new List<Highlight>();
            for (int i = 0; i < _count; i++) if (_ring[i].Night == night) all.Add(_ring[i]);
            all.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Time.CompareTo(b.Time));
            var pick = new List<Highlight>();
            foreach (var h in all)
            {
                bool close = false;
                foreach (var p in pick) if (Math.Abs(p.Time - h.Time) < MinSpacing) close = true;
                if (!close) pick.Add(h);
                if (pick.Count >= n) break;
            }
            pick.Sort((a, b) => a.Time.CompareTo(b.Time));
            return pick;
        }
    }
}
