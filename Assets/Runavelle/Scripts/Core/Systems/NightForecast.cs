using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Exact = derived from the already-rolled plan that will spawn; Estimate = a choice/mutator can still change it.</summary>
    public enum ForecastConfidence { Exact, Estimate }

    public enum SpecialThreatKind { Boss, Flyer, Siege, Burrower, Stealth, ManaDrain, Summoner, Artillery, Teleporter }

    public sealed class FrontForecast
    {
        public string LaneId;
        public string Name;
        /// <summary>Compass direction of the spawn relative to the Castle (Bắc, Đông Nam, …).</summary>
        public string Direction;
        public bool FlyerOnly;
        public int Enemies;
        public int Threat;
        /// <summary>Seconds into the night when the first group of this front spawns.</summary>
        public float FirstContact;
    }

    public sealed class SpecialThreat
    {
        public SpecialThreatKind Kind;
        public string UnitId;
        public string Name;
        public int Count;
        public string LaneId;
        /// <summary>Ignores walls/gates (flies over, tunnels under, blinks past) — the "bypass route" the preview must telegraph.</summary>
        public bool BypassesWalls;
        public string Note;
    }

    /// <summary>
    /// Pre-night intelligence built only from the authoritative NightPlan (the same object the session spawns from),
    /// so the preview cannot drift from the real wave. No second wave model.
    /// </summary>
    public sealed class NightForecast
    {
        public int Night;
        public ForecastConfidence Confidence;
        public readonly List<string> Uncertainty = new List<string>();
        public readonly List<FrontForecast> Fronts = new List<FrontForecast>();
        public readonly List<SpecialThreat> Specials = new List<SpecialThreat>();
        public readonly Dictionary<string, int> Composition = new Dictionary<string, int>();
        public int TotalEnemies;
        public int TotalThreat;
        public string BossUnitId;

        public bool AnyBypass
        {
            get { foreach (var s in Specials) if (s.BypassesWalls) return true; return false; }
        }

        public static NightForecast Build(NightPlan plan, ContentDatabase db, MapDefinition map, Vec2 castle, ForecastConfidence confidence = ForecastConfidence.Exact, IEnumerable<string> uncertainty = null)
        {
            var f = new NightForecast { Night = plan.Night, Confidence = confidence, BossUnitId = plan.BossUnitId };
            if (uncertainty != null) f.Uncertainty.AddRange(uncertainty);
            var fronts = new Dictionary<string, FrontForecast>();
            FrontForecast Front(string laneId)
            {
                if (fronts.TryGetValue(laneId, out var fr)) return fr;
                var lane = map.Lanes.Find(l => l.Id == laneId);
                fr = new FrontForecast { LaneId = laneId, Name = lane?.Name ?? laneId, FlyerOnly = lane != null && lane.FlyerOnly, FirstContact = float.MaxValue,
                    Direction = lane != null ? Compass(lane.Center - castle) : "?" };
                fronts[laneId] = fr;
                return fr;
            }
            foreach (var id in plan.Lanes) Front(id); // advertised fronts first, in plan order
            var specials = new Dictionary<(SpecialThreatKind, string, string), SpecialThreat>();
            foreach (var g in plan.Groups)
            {
                var u = db.Unit(g.UnitId);
                var fr = Front(g.LaneId);
                fr.Enemies += g.Count;
                fr.Threat += g.Count * u.ThreatCost;
                fr.FirstContact = Math.Min(fr.FirstContact, g.Time);
                f.TotalEnemies += g.Count;
                f.TotalThreat += g.Count * u.ThreatCost;
                f.Composition.TryGetValue(g.UnitId, out int n);
                f.Composition[g.UnitId] = n + g.Count;
                foreach (var (kind, bypass, note) in Classify(u, db))
                {
                    var key = (kind, g.UnitId, g.LaneId);
                    if (!specials.TryGetValue(key, out var s))
                    {
                        s = new SpecialThreat { Kind = kind, UnitId = g.UnitId, Name = u.Name, LaneId = g.LaneId, BypassesWalls = bypass, Note = note };
                        specials[key] = s;
                        f.Specials.Add(s);
                    }
                    s.Count += g.Count;
                }
            }
            f.Fronts.AddRange(fronts.Values);
            return f;
        }

        public static IEnumerable<(SpecialThreatKind kind, bool bypass, string note)> Classify(UnitDefinition u, ContentDatabase db)
        {
            if (u.Behavior == EnemyBehavior.Boss)
            {
                yield return (SpecialThreatKind.Boss, false, "Trùm – " + (db.Bosses.TryGetValue(u.Id, out var bd) && !string.IsNullOrEmpty(bd.Counterplay) ? bd.Counterplay : "tập trung hỏa lực"));
                bool teleports = false;
                if (db.Bosses.TryGetValue(u.Id, out var b))
                    foreach (var ph in b.Phases)
                        foreach (var a in ph.Abilities)
                            teleports |= a.Kind == BossAbilityKind.Teleport;
                if (teleports) yield return (SpecialThreatKind.Teleporter, true, "Dịch chuyển qua tường – giữ quân ở tuyến trong");
            }
            if (u.Flying || u.Move == MoveKind.Flying) yield return (SpecialThreatKind.Flyer, true, "Bay qua tường – cần tháp bắn được trên không");
            if (u.Move == MoveKind.Burrow) yield return (SpecialThreatKind.Burrower, true, "Độn thổ, trồi lên sau tường");
            if (u.Behavior == EnemyBehavior.Siege || u.Behavior == EnemyBehavior.GateBreaker) yield return (SpecialThreatKind.Siege, false, "Phá tường/cổng nhanh");
            if (u.Behavior == EnemyBehavior.Artillery) yield return (SpecialThreatKind.Artillery, false, "Bắn công trình từ xa");
            if (u.StealthRange > 0f) yield return (SpecialThreatKind.Stealth, false, "Tàng hình – lộ diện khi tới gần");
            if (u.ManaDrain > 0f || u.Behavior == EnemyBehavior.ManaDrainer) yield return (SpecialThreatKind.ManaDrain, false, "Nhắm Giếng Mana");
            if (u.Behavior == EnemyBehavior.Summoner || u.Behavior == EnemyBehavior.Necromancer) yield return (SpecialThreatKind.Summoner, false, "Triệu hồi thêm quân (ngoài dự báo)");
        }

        /// <summary>8-way compass label for a direction in world space (+Y = north/up on screen).</summary>
        public static string Compass(Vec2 d)
        {
            if (Math.Abs(d.X) < 1e-4f && Math.Abs(d.Y) < 1e-4f) return "Trung tâm";
            double a = Math.Atan2(d.Y, d.X) * 180.0 / Math.PI; // 0 = east, 90 = north
            int sector = (int)Math.Round(((a % 360) + 360) % 360 / 45.0) % 8;
            switch (sector)
            {
                case 0: return "Đông";
                case 1: return "Đông Bắc";
                case 2: return "Bắc";
                case 3: return "Tây Bắc";
                case 4: return "Tây";
                case 5: return "Tây Nam";
                case 6: return "Nam";
                default: return "Đông Nam";
            }
        }

        /// <summary>True when the plan's spawn composition (unit × lane × count) is exactly what this forecast advertised.</summary>
        public bool Matches(NightPlan plan, ContentDatabase db, out string diff)
        {
            var actual = new Dictionary<string, int>();
            var lanes = new Dictionary<string, int>();
            foreach (var g in plan.Groups)
            {
                actual.TryGetValue(g.UnitId, out int n); actual[g.UnitId] = n + g.Count;
                lanes.TryGetValue(g.LaneId, out int m); lanes[g.LaneId] = m + g.Count;
            }
            foreach (var kv in actual)
                if (!Composition.TryGetValue(kv.Key, out int c) || c != kv.Value) { diff = $"{kv.Key}: forecast {c} vs plan {kv.Value}"; return false; }
            foreach (var kv in Composition)
                if (!actual.ContainsKey(kv.Key)) { diff = $"{kv.Key}: forecast {kv.Value} vs plan 0"; return false; }
            foreach (var fr in Fronts)
            {
                lanes.TryGetValue(fr.LaneId, out int m);
                if (m != fr.Enemies) { diff = $"lane {fr.LaneId}: forecast {fr.Enemies} vs plan {m}"; return false; }
            }
            diff = null;
            return true;
        }
    }
}
