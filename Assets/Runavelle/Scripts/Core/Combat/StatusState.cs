using System;

namespace Runavelle.Core
{
    /// <summary>Per-actor elemental gauges and timed combat states. Pure data + tick; rules live in CombatSystem.</summary>
    public sealed class StatusState
    {
        public const float MaxGauge = 100f;
        /// <summary>Gauge required on the existing aura before a different element can react.</summary>
        public const float ReactThreshold = 10f;

        public readonly float[] Gauge = new float[Elements.Count];
        public float FreezeTimer, StunTimer, RootTimer;
        public float SlowTimer, SlowAmount;
        public float AttackSlowTimer, AttackSlowAmount;
        public float BlindTimer;
        public float ArmorShredTimer;
        public float VulnTimer, VulnAmount;
        public float ShieldHp;
        public float GuardTimer, GuardAmount;
        public float FrenzyTimer, FrenzyAmount;
        public float CcCount;
        public float BurnTickTimer;
        public readonly float[] ReactionIcd = new float[32];
        /// <summary>Who last applied each element (for DoT attribution).</summary>
        public readonly int[] AppliedBy = new int[Elements.Count];
        public readonly SourceKind[] AppliedKind = new SourceKind[Elements.Count];

        public bool Frozen => FreezeTimer > 0f;
        public bool Stunned => StunTimer > 0f || FreezeTimer > 0f;
        public bool Rooted => RootTimer > 0f || Stunned;

        public void Clear()
        {
            Array.Clear(Gauge, 0, Gauge.Length);
            FreezeTimer = StunTimer = RootTimer = SlowTimer = SlowAmount = 0f;
            AttackSlowTimer = AttackSlowAmount = BlindTimer = ArmorShredTimer = VulnTimer = VulnAmount = 0f;
            ShieldHp = GuardTimer = GuardAmount = FrenzyTimer = FrenzyAmount = CcCount = 0f;
            Array.Clear(ReactionIcd, 0, ReactionIcd.Length);
        }

        /// <summary>Removes harmful enemy-applied effects (Purify / Sanctuary cleanse).</summary>
        public void Cleanse()
        {
            Array.Clear(Gauge, 0, Gauge.Length);
            FreezeTimer = StunTimer = RootTimer = SlowTimer = 0f;
            AttackSlowTimer = BlindTimer = ArmorShredTimer = VulnTimer = 0f;
        }

        public void Tick(float dt, float waterDecayMult)
        {
            for (int i = 1; i < Gauge.Length; i++)
            {
                if (Gauge[i] <= 0f) continue;
                float decay = Elements.DecayPerSecond((ElementId)i) * (i == (int)ElementId.Water ? waterDecayMult : 1f);
                Gauge[i] = Math.Max(0f, Gauge[i] - decay * dt);
            }
            FreezeTimer = Math.Max(0f, FreezeTimer - dt);
            StunTimer = Math.Max(0f, StunTimer - dt);
            RootTimer = Math.Max(0f, RootTimer - dt);
            if ((SlowTimer -= dt) <= 0f) { SlowTimer = 0f; SlowAmount = 0f; }
            if ((AttackSlowTimer -= dt) <= 0f) { AttackSlowTimer = 0f; AttackSlowAmount = 0f; }
            BlindTimer = Math.Max(0f, BlindTimer - dt);
            ArmorShredTimer = Math.Max(0f, ArmorShredTimer - dt);
            if ((VulnTimer -= dt) <= 0f) { VulnTimer = 0f; VulnAmount = 0f; }
            if ((GuardTimer -= dt) <= 0f) { GuardTimer = 0f; GuardAmount = 0f; }
            if ((FrenzyTimer -= dt) <= 0f) { FrenzyTimer = 0f; FrenzyAmount = 0f; }
            CcCount = Math.Max(0f, CcCount - dt * 0.1f);
            for (int i = 0; i < ReactionIcd.Length; i++)
                if (ReactionIcd[i] > 0f) ReactionIcd[i] -= dt;
        }

        /// <summary>Movement speed multiplier from all control effects.</summary>
        public float MoveMult(float chillStrength)
        {
            if (Stunned || RootTimer > 0f) return 0f;
            float chill = 1f - Gauge[(int)ElementId.Ice] / MaxGauge * chillStrength;
            float slow = 1f - SlowAmount;
            return Math.Max(0.15f, Math.Min(chill, slow));
        }

        public float AttackSpeedMult()
        {
            float m = (1f - AttackSlowAmount) * (1f + FrenzyAmount);
            return Math.Max(0.2f, m);
        }

        public void AddSlow(float amount, float duration)
        {
            if (amount >= SlowAmount) SlowAmount = amount;
            SlowTimer = Math.Max(SlowTimer, duration);
        }

        public void AddAttackSlow(float amount, float duration)
        {
            if (amount >= AttackSlowAmount) AttackSlowAmount = amount;
            AttackSlowTimer = Math.Max(AttackSlowTimer, duration);
        }

        public void AddVulnerability(float amount, float duration)
        {
            if (amount >= VulnAmount) VulnAmount = amount;
            VulnTimer = Math.Max(VulnTimer, duration);
        }

        public void AddGuard(float amount, float duration)
        {
            if (amount >= GuardAmount) GuardAmount = amount;
            GuardTimer = Math.Max(GuardTimer, duration);
        }

        public void AddFrenzy(float amount, float duration)
        {
            if (amount >= FrenzyAmount) FrenzyAmount = amount;
            FrenzyTimer = Math.Max(FrenzyTimer, duration);
        }

        /// <summary>Hard CC duration after diminishing returns (bosses/elites: ccResist; repeated CC is weaker).</summary>
        public float CcDuration(float baseDuration, float ccResist)
        {
            float d = baseDuration * (1f - ccResist) / (1f + CcCount * 0.6f);
            CcCount += 1f;
            return Math.Max(0.15f, d);
        }

        public ElementId DominantAura()
        {
            ElementId best = ElementId.None;
            float v = ReactThreshold - 0.001f;
            for (int i = 1; i < Gauge.Length; i++)
                if (Gauge[i] > v) { v = Gauge[i]; best = (ElementId)i; }
            return best;
        }
    }
}
