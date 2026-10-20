using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Runavelle.Core
{
    /// <summary>Atomic, checksummed JSON files with a .bak fallback. Engine-free (System.IO).</summary>
    public static class SaveStore
    {
        public static uint Fnv(string s)
        {
            uint h = 2166136261;
            foreach (char c in s) { h ^= c; h *= 16777619; }
            return h;
        }

        public static void Write(string path, string payload)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string envelope = MiniJson.Write(new Dictionary<string, object> { ["hash"] = Fnv(payload).ToString(), ["payload"] = payload });
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, envelope, Encoding.UTF8);
            if (File.Exists(path))
            {
                string bak = path + ".bak";
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(path, bak);
            }
            File.Move(tmp, path);
        }

        /// <summary>Returns the payload, falling back to the backup if the main file is corrupt. Null if neither is valid.</summary>
        public static string Read(string path, out bool usedBackup)
        {
            usedBackup = false;
            var p = TryRead(path);
            if (p != null) return p;
            usedBackup = true;
            // interrupted write: the old file was already moved to .bak but the new .tmp never got renamed.
            // A checksummed, complete .tmp is newer than .bak, so prefer it.
            return TryRead(path + ".tmp") ?? TryRead(path + ".bak");
        }

        private static string TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var env = MiniJson.Parse(File.ReadAllText(path, Encoding.UTF8));
                string payload = MiniJson.Str(env, "payload");
                string hash = MiniJson.Str(env, "hash");
                if (payload == null || hash != Fnv(payload).ToString()) return null;
                return payload;
            }
            catch (Exception) { return null; }
        }

        public static void Delete(string path)
        {
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(p)) File.Delete(p);
        }
    }

    /// <summary>
    /// Run snapshot at a day boundary (or a pending choice screen). Stores stable content IDs only — never Unity
    /// instance IDs. Version history: 1 = initial.
    /// </summary>
    public static class RunSave
    {
        // 2: Kingdom Chronicle ("chronicle"); 3: exact continuity — world RNG/time/ids, unit & building dynamic state
        public const int Version = 3;

        public static bool CanSave(GameSession s) => s.Phase == RunPhase.Day || s.Phase == RunPhase.DecreeDraft || s.Phase == RunPhase.GeneralEvolution || s.Phase == RunPhase.KingdomEvolution;

        public static string Serialize(GameSession s)
        {
            if (!CanSave(s)) throw new InvalidOperationException("Runs are saved only at day boundaries");
            s.NormalizeTransients(); // the saved run and the live run continue from the same rested state
            var root = new Dictionary<string, object>
            {
                ["saveVersion"] = Version,
                ["contentVersion"] = s.Content.Levels.Count + s.Content.Units.Count * 1000,
                ["map"] = s.Setup.MapId,
                ["general"] = s.Setup.GeneralId,
                ["seed"] = s.Setup.Seed,
                ["ascension"] = s.Setup.Ascension,
                ["endless"] = s.Setup.Endless,
                ["challenge"] = s.Setup.ChallengeId,
                ["daily"] = s.Setup.IsDaily,
                ["weapon"] = s.Setup.WeaponId,
                ["relic"] = s.Setup.RelicId,
                ["totalNights"] = s.Setup.TotalNights,
                ["unlocked"] = new List<object>(s.Setup.UnlockedGenerals),
                ["night"] = s.Night,
                ["phase"] = s.Phase.ToString(),
                ["gold"] = s.Economy.Gold,
                ["mana"] = s.Economy.Mana,
                ["lifetimeGold"] = s.Economy.LifetimeGoldEarned,
                ["lifetimeMana"] = s.Economy.LifetimeManaEarned,
                ["decrees"] = new List<object>(s.TakenDecrees),
                ["offer"] = new List<object>(s.DecreeOffer),
                ["draftIndex"] = s.DraftIndex,
                ["kingdom"] = s.KingdomEvolution,
                ["generalEvo"] = s.GeneralEvolutionId,
                ["kills"] = s.W.Analytics.TotalKills,
                ["soldiersLost"] = s.W.Analytics.SoldiersLost,
            };
            var ai = new List<object>();
            for (int i = 0; i < 2; i++)
                ai.Add(new Dictionary<string, object> { ["commissioned"] = s.AiSlotCommissioned[i], ["id"] = s.AiGeneralIds[i], ["lane"] = s.AiGeneralLanes[i] });
            root["ai"] = ai;
            var bl = new List<object>();
            foreach (var b in s.W.Buildings)
            {
                if (b.Dead || b.Lifetime >= 0f) continue;
                bl.Add(new Dictionary<string, object>
                {
                    ["level"] = b.Level.Id, ["x"] = b.Origin.X, ["y"] = b.Origin.Y, ["ruined"] = b.Ruined,
                    ["primary"] = (int)b.Primary, ["secondary"] = (int)b.Secondary, ["g"] = b.Invested.Gold, ["m"] = b.Invested.Mana,
                    ["day"] = b.DayBuilt, ["squad"] = b.Squad.Count,
                    ["id"] = b.Id, ["hp"] = b.Hp, ["shot"] = b.ShotCounter, ["shield"] = b.ShieldHp, ["rt"] = b.RespawnTimer,
                    ["soldiers"] = b.Squad.Select(id => s.W.Get(id) as Unit).Where(u => u != null && !u.Dead)
                        .Select(u => (object)new Dictionary<string, object> { ["id"] = u.Id, ["x"] = u.Position.X, ["y"] = u.Position.Y, ["hp"] = u.Hp, ["f"] = u.FacingX }).ToList(),
                });
            }
            root["buildings"] = bl;
            var nights = new List<object>();
            foreach (var n in s.W.Analytics.Nights)
                nights.Add(new Dictionary<string, object> { ["n"] = n.Night, ["d"] = n.Duration, ["k"] = n.Kills, ["c"] = n.CastleDamage, ["g"] = n.Gold, ["m"] = n.Mana, ["l"] = n.Lanes ?? "" });
            root["nights"] = nights;
            // v3 dynamic state
            var gens = new List<object>();
            foreach (var g in new[] { s.W.PlayerGeneral }.Concat(s.W.AIGenerals))
                if (g != null)
                    gens.Add(new Dictionary<string, object> { ["slot"] = g.IsPlayerControlled ? -1 : g.AiSlot, ["id"] = g.Id, ["x"] = g.Position.X, ["y"] = g.Position.Y, ["hp"] = g.Hp,
                        ["downed"] = g.State == UnitState.Downed, ["dt"] = g.DownedTimer, ["ult"] = g.UltCooldown, ["dash"] = g.DashCooldown, ["f"] = g.FacingX, ["rev"] = g.ReviveCount });
            root["generals"] = gens;
            root["rng"] = s.W.Rng.GetState().Select(v => (object)(double)v).ToList();
            root["time"] = s.W.Time;
            root["tick"] = s.W.Tick;
            root["nextId"] = s.W.NextId;
            root["unitOrder"] = s.W.Units.Where(u => !u.Dead).Select(u => (object)u.Id).ToList();
            root["buildingOrder"] = s.W.Buildings.Where(b => !b.Dead).Select(b => (object)b.Id).ToList();
            if (s.Chronicle != null) root["chronicle"] = s.Chronicle.ToJson();
            return MiniJson.Write(root);
        }

        /// <summary>Upgrades older save dictionaries in place to the current version.</summary>
        public static Dictionary<string, object> Migrate(Dictionary<string, object> root)
        {
            int v = MiniJson.Int(root, "saveVersion", 0);
            if (v == 0)
            {
                // v0 (pre-release format) had no phase / ai keys and stored the castle tier as "castleTier".
                if (!root.ContainsKey("phase")) root["phase"] = "Day";
                if (!root.ContainsKey("ai")) root["ai"] = new List<object>();
                if (root.TryGetValue("castleTier", out var ct) && ct is double tier && tier >= 2)
                {
                    var bl = MiniJson.Arr(root, "buildings") ?? new List<object>();
                    foreach (var o in bl)
                        if (o is Dictionary<string, object> d && MiniJson.Str(d, "level") == "castle_1") d["level"] = "castle_2";
                }
                v = 1;
            }
            if (v == 1) v = 2; // v1 → v2: no field changes; a missing "chronicle" is rebuilt by RestoreChronicle
            if (v == 2) v = 3; // v2 → v3: no field changes; without the dynamic block the load falls back to rested defaults
            root["saveVersion"] = (double)v;
            return root;
        }

        public static GameSession Deserialize(ContentDatabase db, string json)
        {
            var root = Migrate((Dictionary<string, object>)MiniJson.Parse(json));
            var setup = new RunSetup
            {
                MapId = db.Migrate(MiniJson.Str(root, "map")),
                GeneralId = db.Migrate(MiniJson.Str(root, "general")),
                Seed = ulong.Parse(MiniJson.Str(root, "seed", "1")),
                Ascension = MiniJson.Int(root, "ascension"),
                Endless = MiniJson.Bool(root, "endless"),
                ChallengeId = MiniJson.Str(root, "challenge"),
                IsDaily = MiniJson.Bool(root, "daily"),
                WeaponId = MiniJson.Str(root, "weapon"),
                RelicId = MiniJson.Str(root, "relic"),
                TotalNights = MiniJson.Int(root, "totalNights", ContentDatabase.TotalNights),
            };
            foreach (var u in MiniJson.Arr(root, "unlocked") ?? new List<object>()) setup.UnlockedGenerals.Add(u.ToString());
            var s = new GameSession(db, setup);
            s.RestoreFrom(root);
            s.RestoreChronicle(root);
            return s;
        }
    }

    public sealed partial class GameSession
    {
        /// <summary>v3: generals' state, world RNG/time/id counter and list order — makes a loaded run continue bit-identically.</summary>
        private void RestoreDynamic(Dictionary<string, object> root)
        {
            if (!root.ContainsKey("rng")) return; // older save: rested defaults
            foreach (var go in MiniJson.Arr(root, "generals") ?? new List<object>())
            {
                int slot = MiniJson.Int(go, "slot", -9);
                var g = slot == -1 ? W.PlayerGeneral : W.AIGenerals.FirstOrDefault(x => x.AiSlot == slot);
                if (g == null) continue;
                g.Position = g.PrevPosition = new Vec2(MiniJson.Float(go, "x"), MiniJson.Float(go, "y"));
                g.Hp = Math.Min(g.MaxHp, MiniJson.Float(go, "hp", g.MaxHp));
                if (MiniJson.Bool(go, "downed")) { g.State = UnitState.Downed; g.DownedTimer = MiniJson.Float(go, "dt"); }
                g.UltCooldown = MiniJson.Float(go, "ult");
                g.DashCooldown = MiniJson.Float(go, "dash");
                g.FacingX = MiniJson.Float(go, "f", 1f);
                g.ReviveCount = MiniJson.Int(go, "rev");
            }
            var rng = MiniJson.Arr(root, "rng");
            if (rng != null && rng.Count == 4) W.Rng.SetState(rng.Select(v => (uint)Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray());
            W.Time = MiniJson.Float(root, "time", W.Time);
            W.Tick = MiniJson.Int(root, "tick", W.Tick);
            W.NextId = Math.Max(W.NextId, MiniJson.Int(root, "nextId", W.NextId));
            void Reorder<T>(List<T> list, string key) where T : Actor
            {
                var order = MiniJson.Arr(root, key);
                if (order == null) return;
                var rank = new Dictionary<int, int>();
                for (int k = 0; k < order.Count; k++) rank[(int)Convert.ToDouble(order[k], System.Globalization.CultureInfo.InvariantCulture)] = k;
                var sorted = list.OrderBy(a => rank.TryGetValue(a.Id, out int r) ? r : int.MaxValue).ThenBy(a => a.Id).ToList();
                list.Clear();
                list.AddRange(sorted);
            }
            Reorder(W.Units, "unitOrder");
            Reorder(W.Buildings, "buildingOrder");
            NormalizeTransients();
        }

        /// <summary>Clears per-actor transient combat/AI state (paths, targets, cooldowns, statuses). Called when saving and after
        /// loading so both continue from the same rested day state; it never touches HP, positions or progression.</summary>
        public void NormalizeTransients()
        {
            foreach (var u in W.Units)
            {
                u.Path.Clear(); u.PathIndex = 0; u.RepathTimer = 0f; u.AiDecisionTimer = 0f; u.TargetId = -1; u.IgnoreTargetId = -1; u.IgnoreTimer = 0f;
                u.ChaseTimer = 0f; u.AttackCooldown = 0f; u.SecondaryCooldown = 0f; u.AttackAnim = 0f; u.IFrames = 0f; u.StuckTimer = 0f; u.LastProgressPos = u.Position;
                u.LastHitTime = -999f; u.MoveInput = Vec2.Zero; u.PrevPosition = u.Position;
                if (u.State != UnitState.Downed) u.State = UnitState.Idle;
                u.Status.Clear();
            }
            foreach (var b in W.Buildings) { b.AttackCooldown = 0f; b.LastHitTime = -999f; b.Status.Clear(); }
        }

        internal void RestoreFrom(Dictionary<string, object> root)
        {
            // remove the default buildings created by the constructor except the castle
            foreach (var b in W.Buildings.ToArray()) if (b.Def.Family != BuildingFamily.Castle) Spawner.RemoveBuilding(b);
            foreach (var u in W.Units.ToArray()) if (u.Role == UnitRole.Soldier) { u.Dead = true; }
            CleanupUnitsPublic();

            Night = MiniJson.Int(root, "night", 1);
            W.Night = Night;
            DraftIndex = MiniJson.Int(root, "draftIndex");
            foreach (var d in MiniJson.Arr(root, "decrees") ?? new List<object>())
            {
                string id = Content.Migrate(d.ToString());
                if (Content.Decrees.TryGetValue(id, out var def))
                {
                    TakenDecrees.Add(id);
                    W.Mods.AddRange(def.Modifiers);
                    foreach (var h in def.Hooks) W.Mods.AddHook(h.Hook, h.Value);
                }
            }
            string kingdom = MiniJson.Str(root, "kingdom");
            if (kingdom != null)
            {
                var k = KingdomEvolutions.Find(x => x.Id == kingdom);
                if (k != null) { KingdomEvolution = kingdom; W.Mods.AddRange(k.Modifiers); foreach (var h in k.Hooks) W.Mods.AddHook(h.Hook, h.Value); }
            }
            string evo = MiniJson.Str(root, "generalEvo");
            if (evo != null)
                foreach (var e in W.PlayerGeneral.General.Evolutions)
                    if (e.Id == evo) { GeneralEvolutionId = evo; W.PlayerGeneral.Evolution = e; }

            foreach (var o in MiniJson.Arr(root, "buildings") ?? new List<object>())
            {
                string lvl = Content.Migrate(MiniJson.Str(o, "level"));
                if (!Content.Levels.ContainsKey(lvl)) continue; // unknown content is skipped, never crashes a load
                var origin = new Int2(MiniJson.Int(o, "x"), MiniJson.Int(o, "y"));
                Building b;
                bool hasDyn = o is Dictionary<string, object> od && od.ContainsKey("id");
                if (Content.BuildingOfLevel(lvl).Family == BuildingFamily.Castle)
                {
                    b = W.Castle;
                    b.Level = Content.Level(lvl);
                }
                else
                {
                    if (hasDyn) W.NextId = MiniJson.Int(o, "id"); // recreate with the same id (AI tie-breaks and order use ids)
                    b = Spawner.CreateBuilding(lvl, origin);
                }
                int p = MiniJson.Int(o, "primary"), sec = MiniJson.Int(o, "secondary");
                b.Primary = Elements.IsValid(p) ? (ElementId)p : ElementId.None;
                b.Secondary = Elements.IsValid(sec) ? (ElementId)sec : ElementId.None;
                b.Invested = new Cost(MiniJson.Int(o, "g"), MiniJson.Int(o, "m"));
                b.DayBuilt = MiniJson.Int(o, "day");
                Spawner.ApplyBuildingStats(b, true);
                b.Ruined = MiniJson.Bool(o, "ruined");
                if (b.Ruined) b.Hp = 0;
                if (hasDyn && !b.Ruined)
                {
                    b.Hp = Math.Min(b.MaxHp, MiniJson.Float(o, "hp", b.MaxHp));
                    b.ShotCounter = MiniJson.Int(o, "shot");
                    b.ShieldHp = MiniJson.Float(o, "shield");
                    b.RespawnTimer = MiniJson.Float(o, "rt");
                }
                if (b.Def.Family == BuildingFamily.Barracks && !b.Ruined)
                {
                    var saved = MiniJson.Arr(o, "soldiers");
                    if (saved != null)
                    {
                        foreach (var so in saved)
                        {
                            W.NextId = MiniJson.Int(so, "id");
                            var u = Spawner.SpawnSoldier(b, new Vec2(MiniJson.Float(so, "x"), MiniJson.Float(so, "y")));
                            u.Hp = Math.Min(u.MaxHp, MiniJson.Float(so, "hp", u.MaxHp));
                            u.FacingX = MiniJson.Float(so, "f", 1f);
                        }
                    }
                    else
                    {
                        int n = Math.Min(Spawner.SquadSize(b), MiniJson.Int(o, "squad", Spawner.SquadSize(b)));
                        for (int i = 0; i < n; i++) Spawner.SpawnSoldier(b, b.Center);
                    }
                }
            }
            W.TopologyVersion++;
            Economy.Restore(MiniJson.Int(root, "gold"), MiniJson.Int(root, "mana"), MiniJson.Int(root, "lifetimeGold"), MiniJson.Int(root, "lifetimeMana"));
            W.Analytics.TotalKills = MiniJson.Int(root, "kills");
            W.Analytics.SoldiersLost = MiniJson.Int(root, "soldiersLost");
            foreach (var n in MiniJson.Arr(root, "nights") ?? new List<object>())
                W.Analytics.Nights.Add(new NightRecord { Night = MiniJson.Int(n, "n"), Duration = MiniJson.Float(n, "d"), Kills = MiniJson.Int(n, "k"), CastleDamage = MiniJson.Float(n, "c"), Gold = MiniJson.Int(n, "g"), Mana = MiniJson.Int(n, "m"), Lanes = MiniJson.Str(n, "l") });
            var ai = MiniJson.Arr(root, "ai") ?? new List<object>();
            for (int i = 0; i < Math.Min(2, ai.Count); i++)
            {
                if (!MiniJson.Bool(ai[i], "commissioned")) continue;
                string gid = Content.Migrate(MiniJson.Str(ai[i], "id"));
                if (gid == null || !Content.Generals.ContainsKey(gid)) continue;
                AiSlotCommissioned[i] = true;
                AiGeneralIds[i] = gid;
                AiGeneralLanes[i] = MiniJson.Str(ai[i], "lane");
                var savedGen = (MiniJson.Arr(root, "generals") ?? new List<object>()).FirstOrDefault(x => MiniJson.Int(x, "slot", -9) == i);
                if (savedGen != null) W.NextId = MiniJson.Int(savedGen, "id");
                Spawner.SpawnGeneral(Content.Generals[gid], Generals_RespawnPoint(), false, i, null);
            }
            Plan = MakePlan();
            ApplyDayHazards();
            ChooseObjective();
            if (AiGeneralLanes[0] != null || AiGeneralLanes[1] != null) PlaceAIGenerals();
            RebuildModifiers();
            Plan = MakePlan();
            RestoreDynamic(root);
            foreach (var b in W.Buildings) if (!b.Ruined) b.Hp = b.MaxHp;
            string phase = MiniJson.Str(root, "phase", "Day");
            if (Enum.TryParse(phase, out RunPhase ph)) Phase = ph;
            if (Phase == RunPhase.DecreeDraft)
            {
                DecreeOffer.Clear();
                foreach (var d in MiniJson.Arr(root, "offer") ?? new List<object>()) if (Content.Decrees.ContainsKey(d.ToString())) DecreeOffer.Add(d.ToString());
                if (DecreeOffer.Count == 0) GenerateDraft();
            }
        }

        private void CleanupUnitsPublic() => CleanupUnits();
    }
}
