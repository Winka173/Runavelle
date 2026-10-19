using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    [Serializable]
    public sealed class BreachRecord
    {
        public int Night;
        public float X, Y;
        public string LevelId;
        public string Cause;
    }

    [Serializable]
    public sealed class NightRecord
    {
        public int Night;
        public float Duration;
        public int Kills;
        public float CastleDamage;
        public int Gold, Mana;
        public int SoldiersLost;
        public int BuildingsLost;
        public string Lanes;
    }

    /// <summary>Local, offline per-run metrics. Nothing is uploaded.</summary>
    public sealed class RunAnalytics
    {
        public readonly Dictionary<string, float> DamageBySource = new Dictionary<string, float>();
        public readonly Dictionary<SourceKind, float> DamageByKind = new Dictionary<SourceKind, float>();
        public readonly Dictionary<ReactionId, int> Reactions = new Dictionary<ReactionId, int>();
        public readonly Dictionary<ReactionId, float> ReactionDamage = new Dictionary<ReactionId, float>();
        public readonly Dictionary<string, float> CastleDamageBy = new Dictionary<string, float>();
        public readonly Dictionary<string, int> KillsByEnemy = new Dictionary<string, int>();
        public readonly List<BreachRecord> Breaches = new List<BreachRecord>();
        public readonly List<NightRecord> Nights = new List<NightRecord>();
        public readonly List<string> BuildLog = new List<string>();
        public int TotalKills;
        public int SoldiersLost;
        public int PeakEnemies;
        public int MaxReactionsInTick;

        // per-night scratch
        public float NightCastleDamage;
        public int NightKills;
        public int NightSoldiersLost;
        public int NightBuildingsLost;

        public void AddDamage(SourceKind kind, string name, float amount)
        {
            DamageByKind.TryGetValue(kind, out float k); DamageByKind[kind] = k + amount;
            if (name != null) { DamageBySource.TryGetValue(name, out float v); DamageBySource[name] = v + amount; }
        }

        public void AddReaction(ReactionId id, float damage)
        {
            Reactions.TryGetValue(id, out int c); Reactions[id] = c + 1;
            ReactionDamage.TryGetValue(id, out float d); ReactionDamage[id] = d + damage;
        }

        public void AddReactionDamage(ReactionId id, float damage)
        {
            ReactionDamage.TryGetValue(id, out float d); ReactionDamage[id] = d + damage;
        }

        public void AddCastleDamage(string enemyName, float amount)
        {
            CastleDamageBy.TryGetValue(enemyName ?? "?", out float v); CastleDamageBy[enemyName ?? "?"] = v + amount;
            NightCastleDamage += amount;
        }

        public void AddKill(string enemyId)
        {
            KillsByEnemy.TryGetValue(enemyId, out int k); KillsByEnemy[enemyId] = k + 1;
            TotalKills++; NightKills++;
        }

        public void ResetNight()
        {
            NightCastleDamage = 0; NightKills = 0; NightSoldiersLost = 0; NightBuildingsLost = 0;
        }

        public static List<KeyValuePair<TKey, TVal>> Top<TKey, TVal>(Dictionary<TKey, TVal> d, int n) where TVal : IComparable<TVal>
        {
            var list = new List<KeyValuePair<TKey, TVal>>(d);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }
    }
}
