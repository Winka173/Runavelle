using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    [Serializable]
    public sealed class SpawnGroup
    {
        public string UnitId;
        public int Count;
        public string LaneId;
        public float Time;
        public float Interval = 0.6f;
        public int Spawned;
    }

    /// <summary>Seeded plan for one night: known (and previewed) before the night starts.</summary>
    public sealed class NightPlan
    {
        public int Night;
        public int Budget;
        public float HpMult = 1f;
        public readonly List<string> Lanes = new List<string>();
        public readonly List<SpawnGroup> Groups = new List<SpawnGroup>();
        public readonly List<string> Tags = new List<string>();
        public string BossUnitId;
        public string Modifier;
        public int TotalEnemies
        {
            get { int n = 0; foreach (var g in Groups) n += g.Count; return n; }
        }
        public bool AllSpawned
        {
            get { foreach (var g in Groups) if (g.Spawned < g.Count) return false; return true; }
        }
    }

    /// <summary>
    /// Threat-budget wave generation: authored night bands (boss nights 5/10/15, unlock nights per enemy) with seeded
    /// variation of composition and attack fronts. Never adapts to punish the player's build.
    /// </summary>
    public sealed class WaveDirector
    {
        private readonly ContentDatabase _db;
        private readonly MapDefinition _map;
        private readonly ulong _seed;
        public float BudgetMult = 1f;
        public float HpScaleMult = 1f;
        public List<Faction> Factions = new List<Faction> { Faction.Marauders };

        public WaveDirector(ContentDatabase db, MapDefinition map, ulong seed)
        {
            _db = db; _map = map; _seed = seed;
        }

        public static int BaseBudget(int night) => (int)Math.Round(7 + night * 4.2f + Math.Pow(night, 1.55) * 1.1f);

        public static string BossFor(int night, int totalNights)
        {
            if (night == 5) return "boss_gatebreaker";
            if (night == 10) return "boss_rift_warlock";
            if (night == totalNights) return "boss_siege_lord";
            return null;
        }

        public NightPlan Plan(int night, int totalNights, Func<int, string> bossOverride = null)
        {
            var rng = Rng.Derive(_seed, 1000 + (ulong)night);
            var plan = new NightPlan { Night = night };
            plan.Budget = (int)Math.Round(BaseBudget(night) * BudgetMult);
            plan.HpMult = (1f + 0.055f * (night - 1)) * HpScaleMult;

            // fronts
            var ground = new List<SpawnLane>();
            var sky = new List<SpawnLane>();
            foreach (var l in _map.Lanes) if (l.MinNight <= night) (l.FlyerOnly ? sky : ground).Add(l);
            if (ground.Count == 0) foreach (var l in _map.Lanes) if (!l.FlyerOnly) { ground.Add(l); break; }
            int fronts = night <= 3 ? 1 : night <= 8 ? (rng.Chance(0.5f) ? 2 : 1) : night < 12 ? 2 : Math.Min(ground.Count, rng.Chance(0.5f) ? 3 : 2);
            if (night == totalNights) fronts = ground.Count;
            fronts = Math.Max(1, Math.Min(fronts, ground.Count));
            var lanes = new List<SpawnLane>(ground);
            rng.Shuffle(lanes);
            for (int i = 0; i < fronts; i++) plan.Lanes.Add(lanes[i].Id);

            // pool
            var pool = new List<UnitDefinition>();
            foreach (var u in _db.Units.Values)
            {
                if (u.Role != UnitRole.Enemy || u.Behavior == EnemyBehavior.Boss || u.UnlockNight > night) continue;
                if (!Factions.Contains(u.Faction)) continue;
                pool.Add(u);
            }
            // thin faction mixes (early nights) are topped up with Marauders so every night has variety
            if (pool.Count < 4 && !Factions.Contains(Faction.Marauders))
                foreach (var u in _db.Units.Values)
                    if (u.Role == UnitRole.Enemy && u.Behavior != EnemyBehavior.Boss && u.UnlockNight <= night && u.Faction == Faction.Marauders) pool.Add(u);
            pool.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            bool skyAvailable = sky.Count > 0;

            string boss = bossOverride?.Invoke(night) ?? BossFor(night, totalNights);
            if (boss != null && _db.Units.ContainsKey(boss))
            {
                plan.BossUnitId = boss;
                plan.Budget = (int)(plan.Budget * 0.75f);
            }

            // composition: a few "squads" per front, weighted toward variety
            int remaining = plan.Budget;
            float t = 4f;
            int guard = 0;
            var used = new Dictionary<string, int>();
            int flyers = 0;
            while (remaining > 0 && guard++ < 200)
            {
                var weights = new List<float>();
                foreach (var u in pool)
                {
                    float w = u.ThreatCost <= remaining ? 1f : 0f;
                    if (w > 0f)
                    {
                        used.TryGetValue(u.Id, out int c);
                        w /= 1f + c * 0.7f;
                        if (u.UnlockNight == night) w *= 2.5f; // introduce new enemies on their night
                        if (u.HasTag("swarm")) w *= 1.3f;
                    }
                    weights.Add(w);
                }
                int idx = rng.PickWeighted(weights);
                if (idx < 0) break;
                var def = pool[idx];
                int cap = def.HasTag("swarm") ? 8 : 4;
                if (def.UnlockNight == night) cap = Math.Min(cap, def.HasTag("swarm") ? 6 : 2); // introduce gently
                if (def.HasTag("siege") && night < 8) cap = Math.Min(cap, 1 + night / 4);
                if (!def.HasTag("swarm") && night <= 4) { used.TryGetValue(def.Id, out int early); cap = Math.Min(cap, 2 - early); if (cap <= 0) { pool.Remove(def); continue; } } // no early brute spikes
                if (def.Flying)
                {
                    // air pressure ramps with the night so early towers are not overwhelmed by swarms of flyers
                    int flyerCap = 2 + night * 3 / 2 - flyers;
                    if (flyerCap <= 0) { pool.Remove(def); continue; }
                    cap = Math.Min(cap, flyerCap);
                }
                used.TryGetValue(def.Id, out int already);
                if (def.HasTag("siege") && already >= 1 + night / 3) { pool.Remove(def); continue; }
                int maxCount = Math.Max(1, Math.Min(cap, remaining / def.ThreatCost));
                int count = rng.Range(Math.Max(1, maxCount / 2), maxCount + 1);
                remaining -= count * def.ThreatCost;
                used.TryGetValue(def.Id, out int cc);
                used[def.Id] = cc + count;
                if (def.Flying) flyers += count;
                string lane = def.Flying && skyAvailable && rng.Chance(0.5f) ? sky[rng.Range(0, sky.Count)].Id : plan.Lanes[rng.Range(0, plan.Lanes.Count)];
                plan.Groups.Add(new SpawnGroup { UnitId = def.Id, Count = count, LaneId = lane, Time = t, Interval = def.HasTag("swarm") ? 0.35f : 0.8f });
                t += rng.Range(3f, 7f) + Math.Max(0, 6 - night) * 0.6f;
            }
            if (plan.BossUnitId != null)
                plan.Groups.Add(new SpawnGroup { UnitId = plan.BossUnitId, Count = 1, LaneId = plan.Lanes[0], Time = Math.Max(12f, t * 0.55f) });
            plan.Groups.Sort((a, b) => a.Time.CompareTo(b.Time));
            BuildTags(plan);
            return plan;
        }

        public void BuildTags(NightPlan plan)
        {
            plan.Tags.Clear();
            var seen = new HashSet<string>();
            void Tag(string s) { if (seen.Add(s)) plan.Tags.Add(s); }
            foreach (var g in plan.Groups)
            {
                var u = _db.Unit(g.UnitId);
                if (u.Behavior == EnemyBehavior.Boss) Tag("BOSS: " + u.Name);
                if (u.Flying) Tag("Kẻ bay (vượt địa hình)");
                if (u.Behavior == EnemyBehavior.Siege || u.Behavior == EnemyBehavior.GateBreaker) Tag("Công thành");
                if (u.Move == MoveKind.Burrow) Tag("Độn thổ (trồi lên sau tường)");
                if (u.StealthRange > 0f) Tag("Tàng hình");
                if (u.ManaDrain > 0f) Tag("Hút Mana");
                if (u.HasTag("armored")) Tag("Giáp dày (dùng nguyên tố)");
                if (u.HasTag("elite")) Tag("Tinh anh");
                if (u.Behavior == EnemyBehavior.Summoner || u.Behavior == EnemyBehavior.Necromancer) Tag("Triệu hồi");
                if (u.HasTag("swarm") && g.Count >= 6) Tag("Bầy đàn");
            }
        }
    }
}
