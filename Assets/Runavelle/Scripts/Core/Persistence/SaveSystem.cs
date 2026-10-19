using System;
using System.Collections.Generic;
using System.IO;
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
            return TryRead(path + ".bak");
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
        public const int Version = 1;

        public static bool CanSave(GameSession s) => s.Phase == RunPhase.Day || s.Phase == RunPhase.DecreeDraft || s.Phase == RunPhase.GeneralEvolution || s.Phase == RunPhase.KingdomEvolution;

        public static string Serialize(GameSession s)
        {
            if (!CanSave(s)) throw new InvalidOperationException("Runs are saved only at day boundaries");
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
                });
            }
            root["buildings"] = bl;
            var nights = new List<object>();
            foreach (var n in s.W.Analytics.Nights)
                nights.Add(new Dictionary<string, object> { ["n"] = n.Night, ["d"] = n.Duration, ["k"] = n.Kills, ["c"] = n.CastleDamage, ["g"] = n.Gold, ["m"] = n.Mana, ["l"] = n.Lanes ?? "" });
            root["nights"] = nights;
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
            return s;
        }
    }

    public sealed partial class GameSession
    {
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
                if (Content.BuildingOfLevel(lvl).Family == BuildingFamily.Castle)
                {
                    b = W.Castle;
                    b.Level = Content.Level(lvl);
                }
                else b = Spawner.CreateBuilding(lvl, origin);
                int p = MiniJson.Int(o, "primary"), sec = MiniJson.Int(o, "secondary");
                b.Primary = Elements.IsValid(p) ? (ElementId)p : ElementId.None;
                b.Secondary = Elements.IsValid(sec) ? (ElementId)sec : ElementId.None;
                b.Invested = new Cost(MiniJson.Int(o, "g"), MiniJson.Int(o, "m"));
                b.DayBuilt = MiniJson.Int(o, "day");
                Spawner.ApplyBuildingStats(b, true);
                b.Ruined = MiniJson.Bool(o, "ruined");
                if (b.Ruined) b.Hp = 0;
                if (b.Def.Family == BuildingFamily.Barracks && !b.Ruined)
                {
                    int n = Math.Min(Spawner.SquadSize(b), MiniJson.Int(o, "squad", Spawner.SquadSize(b)));
                    for (int i = 0; i < n; i++) Spawner.SpawnSoldier(b, b.Center);
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
                Spawner.SpawnGeneral(Content.Generals[gid], Generals_RespawnPoint(), false, i, null);
            }
            Plan = Waves.Plan(Night, Math.Max(TotalNights, Night), BossOverride);
            ApplyDayHazards();
            ChooseObjective();
            if (AiGeneralLanes[0] != null || AiGeneralLanes[1] != null) PlaceAIGenerals();
            RebuildModifiers();
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
