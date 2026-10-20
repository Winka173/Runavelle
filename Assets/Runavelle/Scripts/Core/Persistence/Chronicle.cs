using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Runavelle.Core
{
    public enum ChronicleKind
    {
        RunStart, CastleTier, KingdomEvolution, GeneralEvolution, Decree, FirstElite, BossArrives, BossDefeat,
        WallBreach, BreachOrders, EmergencyUltimate, NightVictory, NightDefeat, RunEnd
    }

    public sealed class ChronicleEntry
    {
        public int Night;
        public bool AtNight;
        public float Time;
        public ChronicleKind Kind;
        public string Text;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
            { ["n"] = Night, ["a"] = AtNight, ["t"] = Math.Round(Time, 1), ["k"] = Kind.ToString(), ["x"] = Text };

        public static ChronicleEntry FromJson(Dictionary<string, object> d)
        {
            Enum.TryParse(MiniJson.Str(d, "k", "RunStart"), out ChronicleKind k);
            return new ChronicleEntry { Night = MiniJson.Int(d, "n"), AtNight = MiniJson.Bool(d, "a"), Time = MiniJson.Float(d, "t"), Kind = k, Text = MiniJson.Str(d, "x", "") };
        }
    }

    /// <summary>
    /// Kingdom Chronicle of one run: deterministic event summaries recorded from simulation state (never synthetic images),
    /// plus the metadata needed to replay the seed and to compare builds. Stored in the run save and archived in the Profile.
    /// </summary>
    public sealed class RunChronicle
    {
        public string Map, General, Challenge, Result = "";
        public ulong Seed;
        public int Ascension, Nights, ContentVersion;
        public bool Endless;
        public string StartedUtc = "";
        public readonly List<string> Decrees = new List<string>();
        /// <summary>End-of-run build: level id → count (the "end snapshot" as data).</summary>
        public readonly SortedDictionary<string, int> FinalBuild = new SortedDictionary<string, int>(StringComparer.Ordinal);
        public readonly List<ChronicleEntry> Entries = new List<ChronicleEntry>();
        public const int MaxEntries = 160;

        public void Add(ChronicleEntry e)
        {
            if (Entries.Count >= MaxEntries) return; // bounded: a run cannot grow the save without limit
            Entries.Add(e);
        }

        public RunSetup ToReplaySetup() => new RunSetup { MapId = Map, GeneralId = General, Seed = Seed, Ascension = Ascension, ChallengeId = Challenge, Endless = Endless };

        // ------------------------------------------------------------------ search (offline, diacritics/case-insensitive)
        // Vietnamese diacritics → base letters. A table instead of string.Normalize: Normalize is a no-op under
        // invariant globalization (headless tools) and not guaranteed on every IL2CPP target.
        private const string FoldFrom = "àÀáÁạẠảẢãÃâÂầẦấẤậẬẩẨẫẪăĂằẰắẮặẶẳẲẵẴèÈéÉẹẸẻẺẽẼêÊềỀếẾệỆểỂễỄìÌíÍịỊỉỈĩĨòÒóÓọỌỏỎõÕôÔồỒốỐộỘổỔỗỖơƠờỜớỚợỢởỞỡỠùÙúÚụỤủỦũŨưƯừỪứỨựỰửỬữỮỳỲýÝỵỴỷỶỹỸđĐ";
        private const string FoldTo = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaeeeeeeeeeeeeeeeeeeeeeeiiiiiiiiiioooooooooooooooooooooooooooooooooouuuuuuuuuuuuuuuuuuuuuuyyyyyyyyyydd";

        public static string Fold(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                int i = FoldFrom.IndexOf(ch);
                sb.Append(i >= 0 ? FoldTo[i] : char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        /// <summary>Entries whose text (or the run's map/general) contains every query word.</summary>
        public List<ChronicleEntry> Search(string query, ContentDatabase db = null)
        {
            var words = Fold(query).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string meta = Fold(Map + " " + General + " " + (db != null && Map != null && db.Maps.TryGetValue(Map, out var m) ? m.Name : "")
                               + " " + (db != null && General != null && db.Generals.TryGetValue(General, out var g) ? g.Name : ""));
            var hits = new List<ChronicleEntry>();
            foreach (var e in Entries)
            {
                string hay = Fold(e.Text) + " " + meta + " dem " + e.Night;
                bool all = true;
                foreach (var w in words) if (!hay.Contains(w)) { all = false; break; }
                if (all) hits.Add(e);
            }
            return hits;
        }

        // ------------------------------------------------------------------ persistence
        public Dictionary<string, object> ToJson()
        {
            var entries = new List<object>();
            foreach (var e in Entries) entries.Add(e.ToJson());
            var build = new Dictionary<string, object>();
            foreach (var kv in FinalBuild) build[kv.Key] = kv.Value;
            return new Dictionary<string, object>
            {
                ["map"] = Map, ["general"] = General, ["seed"] = Seed.ToString(), ["asc"] = Ascension, ["challenge"] = Challenge, ["endless"] = Endless,
                ["result"] = Result, ["nights"] = Nights, ["content"] = ContentVersion, ["started"] = StartedUtc,
                ["decrees"] = new List<object>(Decrees), ["build"] = build, ["entries"] = entries,
            };
        }

        public static RunChronicle FromJson(Dictionary<string, object> d)
        {
            var c = new RunChronicle
            {
                Map = MiniJson.Str(d, "map"), General = MiniJson.Str(d, "general"), Challenge = MiniJson.Str(d, "challenge"),
                Seed = ulong.TryParse(MiniJson.Str(d, "seed", "0"), out var sd) ? sd : 0, Ascension = MiniJson.Int(d, "asc"), Endless = MiniJson.Bool(d, "endless"),
                Result = MiniJson.Str(d, "result", ""), Nights = MiniJson.Int(d, "nights"), ContentVersion = MiniJson.Int(d, "content"), StartedUtc = MiniJson.Str(d, "started", ""),
            };
            foreach (var x in MiniJson.Arr(d, "decrees") ?? new List<object>()) c.Decrees.Add(x.ToString());
            if (d.TryGetValue("build", out var bo) && bo is Dictionary<string, object> b)
                foreach (var kv in b) c.FinalBuild[kv.Key] = (int)Convert.ToDouble(kv.Value, CultureInfo.InvariantCulture);
            foreach (var x in MiniJson.Arr(d, "entries") ?? new List<object>())
                if (x is Dictionary<string, object> ed) c.Entries.Add(ChronicleEntry.FromJson(ed));
            return c;
        }
    }
}
