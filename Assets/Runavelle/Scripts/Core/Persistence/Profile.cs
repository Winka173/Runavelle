using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    [Serializable]
    public sealed class RunSummary
    {
        public string Map, General, Seed, Result;
        public int Nights, Kills, Ascension, Score;
        public bool Daily;
        public string Date;
        public string Challenge;
    }

    /// <summary>Offline player profile: unlocks (options, never stat inflation), settings, history, mastery.</summary>
    public sealed class Profile
    {
        public const int Version = 1;
        public int Victories;
        public int BestNight;
        public int RunsPlayed;
        public readonly Dictionary<string, int> MaxAscensionCleared = new Dictionary<string, int>();
        public readonly Dictionary<string, int> GeneralMastery = new Dictionary<string, int>();
        public readonly HashSet<string> Achievements = new HashSet<string>();
        public readonly HashSet<string> Cosmetics = new HashSet<string>();
        public readonly List<RunSummary> History = new List<RunSummary>();
        public readonly Dictionary<string, string> Settings = new Dictionary<string, string>();
        public bool TutorialDone;
        public bool EndlessUnlocked => Victories > 0;

        public bool IsUnlocked(UnlockRule rule, int value)
        {
            switch (rule)
            {
                case UnlockRule.Default: return true;
                case UnlockRule.ReachNight5: return BestNight >= 5;
                case UnlockRule.ReachNight10: return BestNight >= 10;
                case UnlockRule.ReachNight12: return BestNight >= 12;
                case UnlockRule.FirstVictory: return Victories >= 1;
                case UnlockRule.Victories2: return Victories >= 2;
                case UnlockRule.Victories3: return Victories >= 3;
                case UnlockRule.MasteryPoints: return TotalMastery() >= value;
                default: return false;
            }
        }

        public static string UnlockText(UnlockRule rule, int value)
        {
            switch (rule)
            {
                case UnlockRule.ReachNight5: return "Sống sót tới đêm 5";
                case UnlockRule.ReachNight10: return "Sống sót tới đêm 10";
                case UnlockRule.ReachNight12: return "Sống sót tới đêm 12";
                case UnlockRule.FirstVictory: return "Thắng 1 lần";
                case UnlockRule.Victories2: return "Thắng 2 lần";
                case UnlockRule.Victories3: return "Thắng 3 lần";
                case UnlockRule.MasteryPoints: return "Đạt " + value + " điểm thông thạo";
                default: return "";
            }
        }

        public int TotalMastery()
        {
            int n = 0;
            foreach (var v in GeneralMastery.Values) n += v;
            return n;
        }

        public List<string> UnlockedGenerals(ContentDatabase db)
        {
            var list = new List<string>();
            foreach (var id in db.GeneralOrder)
            {
                var g = db.Generals[id];
                if (IsUnlocked(g.Unlock, g.UnlockValue)) list.Add(id);
            }
            return list;
        }

        public bool MapUnlocked(MapDefinition m) => m.UnlockOrder == 0 || BestNight >= MapUnlockNight(m) || Victories >= m.UnlockOrder;
        public static int MapUnlockNight(MapDefinition m) => System.Math.Min(15, 3 + m.UnlockOrder * 2);

        public int MaxAscension(string mapId) => MaxAscensionCleared.TryGetValue(mapId, out int v) ? v : -1;

        /// <summary>Records a finished run; returns newly unlocked achievement ids.</summary>
        public List<string> RecordRun(GameSession s, bool victory)
        {
            RunsPlayed++;
            int nights = victory ? s.TotalNights : s.Night - 1;
            BestNight = Math.Max(BestNight, nights);
            if (victory)
            {
                Victories++;
                int cur = MaxAscension(s.Setup.MapId);
                if (s.Setup.Ascension > cur) MaxAscensionCleared[s.Setup.MapId] = s.Setup.Ascension;
            }
            GeneralMastery.TryGetValue(s.Setup.GeneralId, out int m);
            GeneralMastery[s.Setup.GeneralId] = m + nights + (victory ? 10 : 0) + s.Setup.Ascension * 3;
            History.Insert(0, new RunSummary { Map = s.Setup.MapId, General = s.Setup.GeneralId, Seed = s.Setup.Seed.ToString(), Result = victory ? "WIN" : "LOSS", Nights = nights,
                Kills = s.W.Analytics.TotalKills, Ascension = s.Setup.Ascension, Score = s.ComputeScore(victory), Daily = s.Setup.IsDaily, Challenge = s.Setup.ChallengeId,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd") });
            if (History.Count > 30) History.RemoveRange(30, History.Count - 30);
            return Mastery.Evaluate(this, s, victory);
        }

        public string Serialize()
        {
            var hist = new List<object>();
            foreach (var h in History)
                hist.Add(new Dictionary<string, object> { ["map"] = h.Map, ["general"] = h.General, ["seed"] = h.Seed, ["result"] = h.Result, ["nights"] = h.Nights, ["kills"] = h.Kills,
                    ["asc"] = h.Ascension, ["score"] = h.Score, ["daily"] = h.Daily, ["date"] = h.Date, ["challenge"] = h.Challenge });
            var asc = new Dictionary<string, object>();
            foreach (var kv in MaxAscensionCleared) asc[kv.Key] = kv.Value;
            var mast = new Dictionary<string, object>();
            foreach (var kv in GeneralMastery) mast[kv.Key] = kv.Value;
            var settings = new Dictionary<string, object>();
            foreach (var kv in Settings) settings[kv.Key] = kv.Value;
            return MiniJson.Write(new Dictionary<string, object>
            {
                ["version"] = Version, ["victories"] = Victories, ["bestNight"] = BestNight, ["runs"] = RunsPlayed, ["tutorial"] = TutorialDone,
                ["asc"] = asc, ["mastery"] = mast, ["achievements"] = new List<object>(Achievements), ["cosmetics"] = new List<object>(Cosmetics),
                ["history"] = hist, ["settings"] = settings,
            });
        }

        public static Profile Deserialize(string json)
        {
            var p = new Profile();
            if (string.IsNullOrEmpty(json)) return p;
            var root = MiniJson.Parse(json);
            p.Victories = MiniJson.Int(root, "victories");
            p.BestNight = MiniJson.Int(root, "bestNight");
            p.RunsPlayed = MiniJson.Int(root, "runs");
            p.TutorialDone = MiniJson.Bool(root, "tutorial");
            var asc = MiniJson.Obj(root, "asc");
            if (asc != null) foreach (var kv in asc) p.MaxAscensionCleared[kv.Key] = (int)(double)kv.Value;
            var mast = MiniJson.Obj(root, "mastery");
            if (mast != null) foreach (var kv in mast) p.GeneralMastery[kv.Key] = (int)(double)kv.Value;
            foreach (var a in MiniJson.Arr(root, "achievements") ?? new List<object>()) p.Achievements.Add(a.ToString());
            foreach (var a in MiniJson.Arr(root, "cosmetics") ?? new List<object>()) p.Cosmetics.Add(a.ToString());
            foreach (var h in MiniJson.Arr(root, "history") ?? new List<object>())
                p.History.Add(new RunSummary { Map = MiniJson.Str(h, "map"), General = MiniJson.Str(h, "general"), Seed = MiniJson.Str(h, "seed"), Result = MiniJson.Str(h, "result"),
                    Nights = MiniJson.Int(h, "nights"), Kills = MiniJson.Int(h, "kills"), Ascension = MiniJson.Int(h, "asc"), Score = MiniJson.Int(h, "score"), Daily = MiniJson.Bool(h, "daily"),
                    Date = MiniJson.Str(h, "date"), Challenge = MiniJson.Str(h, "challenge") });
            var st = MiniJson.Obj(root, "settings");
            if (st != null) foreach (var kv in st) p.Settings[kv.Key] = kv.Value?.ToString();
            return p;
        }
    }

    /// <summary>Achievements / mastery badges (cosmetic + sidegrade unlocks only). Extended in Phase V.</summary>
    public static partial class Mastery
    {
        public static List<string> Evaluate(Profile p, GameSession s, bool victory)
        {
            var got = new List<string>();
            void Grant(string id) { if (p.Achievements.Add(id)) got.Add(id); }
            if (victory) Grant("first_victory");
            if (s.Night >= 5 || victory) Grant("night_5");
            if (s.Night >= 10 || victory) Grant("night_10");
            EvaluateMore(p, s, victory, Grant);
            return got;
        }

        public static string Name(string id)
        {
            switch (id)
            {
                case "first_victory": return "Chiến thắng đầu tiên";
                case "night_5": return "Qua đêm 5";
                case "night_10": return "Qua đêm 10";
                default: return NameMore(id) ?? id;
            }
        }

        static partial void NameMoreImpl(string id, ref string name);
        private static string NameMore(string id) { string n = null; NameMoreImpl(id, ref n); return n; }

        static partial void EvaluateMoreImpl(Profile p, GameSession s, bool victory, Action<string> grant);
        private static void EvaluateMore(Profile p, GameSession s, bool victory, Action<string> grant) => EvaluateMoreImpl(p, s, victory, grant);
    }
}
