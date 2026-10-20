using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Presentation-only combat feedback rules (impact tiers, effect budgets, camera shake aggregation, damage-text
    /// aggregation). Engine-free so it is unit-tested; the Unity layer only renders what these classes decide.
    /// Nothing here feeds back into the simulation.
    /// </summary>
    public enum ImpactTier { None = 0, Light = 1, Medium = 2, Heavy = 3, Epic = 4 }

    public static class FeedbackRules
    {
        /// <summary>Classifies a simulation event by gameplay importance (drives VFX scale, shake, sound priority).</summary>
        public static ImpactTier Classify(in SimEvent e, World w)
        {
            switch (e.Type)
            {
                case SimEventType.UltimateCast:
                case SimEventType.BossPhase:
                    return ImpactTier.Epic;
                case SimEventType.Kill:
                {
                    var u = w.Get(e.ActorId) as Unit;
                    if (u != null && u.IsBoss) return ImpactTier.Epic;
                    if (u != null && u.IsElite) return ImpactTier.Heavy;
                    return ImpactTier.Light;
                }
                case SimEventType.WallBreached:
                case SimEventType.BuildingDestroyed:
                case SimEventType.Explosion:
                    return ImpactTier.Heavy;
                case SimEventType.Reaction:
                    return e.Text == "arc" || e.Text == "chain" ? ImpactTier.Light : ImpactTier.Medium;
                case SimEventType.Hazard:
                    return ImpactTier.Medium;
                case SimEventType.Hit:
                {
                    var t = w.Get(e.ActorId);
                    if (t == null) return ImpactTier.Light;
                    if (t == w.Castle) return e.Value >= 40f ? ImpactTier.Heavy : ImpactTier.Medium;
                    if (t.IsBoss || e.Value >= 120f) return ImpactTier.Medium;
                    if (t is Unit tu && tu.Role == UnitRole.General) return e.Value >= 40f ? ImpactTier.Medium : ImpactTier.Light;
                    return ImpactTier.Light;
                }
                default:
                    return ImpactTier.None;
            }
        }

        /// <summary>Camera impulse requested per tier (before the player's shake setting).</summary>
        public static float ShakeFor(ImpactTier t) => t == ImpactTier.Epic ? 0.45f : t == ImpactTier.Heavy ? 0.28f : t == ImpactTier.Medium ? 0.08f : 0f;

        /// <summary>Visual-only hold (local hit-stop) on the struck unit's sprite, seconds. Never pauses the simulation.</summary>
        public static float LocalHoldFor(ImpactTier t) => t == ImpactTier.Epic ? 0.12f : t == ImpactTier.Heavy ? 0.07f : 0f;
    }

    /// <summary>Token buckets per tier: caps how many feedback effects of each tier may start per second (big fights stay readable).</summary>
    public sealed class FeedbackBudget
    {
        private readonly float[] _tokens = new float[5];
        private float _last;
        public static readonly float[] RatePerSecond = { 0f, 24f, 10f, 5f, 4f };
        public static readonly float[] Burst = { 0f, 12f, 6f, 4f, 3f };
        public int Denied;

        public FeedbackBudget()
        {
            for (int i = 0; i < 5; i++) _tokens[i] = Burst[i];
        }

        public bool Allow(ImpactTier t, float now)
        {
            if (t == ImpactTier.None) return false;
            float dt = Math.Max(0f, now - _last);
            _last = now;
            for (int i = 1; i < 5; i++) _tokens[i] = Math.Min(Burst[i], _tokens[i] + dt * RatePerSecond[i]);
            int k = (int)t;
            if (_tokens[k] >= 1f) { _tokens[k] -= 1f; return true; }
            if (t == ImpactTier.Epic) return true; // epic moments are rare by construction; never dropped
            Denied++;
            return false;
        }
    }

    /// <summary>
    /// Priority-aggregated, damped camera shake: simultaneous requests take the maximum (AoE on 30 enemies is one
    /// shake, not 30), strength scales with the player's setting (0 = off) and is hard-capped.
    /// </summary>
    public sealed class ShakeAggregator
    {
        public const float Cap = 0.6f;
        public const float DecayPerSecond = 3f;
        public float Strength = 1f;
        public float Value { get; private set; }

        public void Request(float amount)
        {
            if (Strength <= 0f || amount <= 0f) return;
            Value = Math.Min(Cap, Math.Max(Value, amount * Strength));
        }

        public void Update(float dt) => Value = Math.Max(0f, Value - dt * DecayPerSecond * Math.Max(0.35f, Value));
    }

    public enum DamageTextMode { Off = 0, Important = 1, All = 2 }
    public enum DamageTextKind { Normal, Elemental, Reaction, Elite, Boss, Incoming, Heal }

    public sealed class DamageText
    {
        public int TargetId;
        public float Value;
        public float Age;
        public float Life = 0.9f;
        public DamageTextKind Kind;
        public ElementId Element;
        public Vec2 Position;
        public int Merges;
        /// <summary>Display size multiplier: grows with merged total and importance.</summary>
        public float Emphasis => (Kind == DamageTextKind.Reaction || Kind == DamageTextKind.Boss ? 1.35f : Kind == DamageTextKind.Elite ? 1.2f : 1f) * (1f + Math.Min(0.6f, Value / 400f));
    }

    /// <summary>
    /// Groups repeated hits on the same target within a short window into one growing number, filters by mode and
    /// keeps a hard cap on simultaneous numbers (dropping the smallest first).
    /// </summary>
    public sealed class DamageTextAggregator
    {
        public const float MergeWindow = 0.35f;
        public const int MaxActive = 36;
        public DamageTextMode Mode = DamageTextMode.All;
        public readonly List<DamageText> Active = new List<DamageText>();

        public static bool Important(DamageTextKind k) => k != DamageTextKind.Normal && k != DamageTextKind.Elemental;

        public DamageText Add(int targetId, float value, DamageTextKind kind, ElementId e, Vec2 pos)
        {
            if (Mode == DamageTextMode.Off || value < 0.5f) return null;
            if (Mode == DamageTextMode.Important && !Important(kind)) return null;
            foreach (var d in Active)
                if (d.TargetId == targetId && targetId >= 0 && d.Age < MergeWindow && d.Kind == kind)
                {
                    d.Value += value;
                    d.Merges++;
                    d.Age = Math.Min(d.Age, 0.1f);
                    d.Position = pos;
                    return d;
                }
            if (Active.Count >= MaxActive)
            {
                int smallest = 0;
                for (int i = 1; i < Active.Count; i++) if (Active[i].Value < Active[smallest].Value) smallest = i;
                if (Active[smallest].Value >= value && !Important(kind)) return null;
                Active.RemoveAt(smallest);
            }
            var t = new DamageText { TargetId = targetId, Value = value, Kind = kind, Element = e, Position = pos, Life = Important(kind) ? 1.2f : 0.9f };
            Active.Add(t);
            return t;
        }

        public void Update(float dt)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Active[i].Age += dt;
                if (Active[i].Age >= Active[i].Life) Active.RemoveAt(i);
            }
        }
    }
}
