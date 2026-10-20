using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Phase V run rules: Ascension tiers, Challenge runs, Endless mutators and boss rotation.</summary>
    public sealed partial class GameSession
    {
        /// <summary>Decree options per draft (Ascension 5 → 2, no reroll).</summary>
        public int DraftSize = 3;
        /// <summary>Fraction of max HP surviving structures are repaired to at dawn (Ascension 9 → 0.6).</summary>
        public float DawnRepairFraction = 1f;
        public readonly HashSet<BuildingFamily> BannedFamilies = new HashSet<BuildingFamily>();
        public readonly List<string> ChallengeCurses = new List<string>();
        public readonly List<EndlessMutator> EndlessMutatorsActive = new List<EndlessMutator>();
        public ChallengeDefinition Challenge => RunRules.Challenge(Setup.ChallengeId);
        public bool IsEndlessNight => Setup.Endless && Night > TotalNights;

        partial void ApplyRunRulesImpl()
        {
            int a = Math.Max(0, Math.Min(RunRules.MaxAscension, Setup.Ascension));
            if (a >= 1) W.Mods.Add(new Modifier(Stat.MaxHp, Scope.Enemies, 0.10f));
            Spawner.EliteAffixes = a >= 3;
            float budget = a >= 4 ? 1.12f : 1f;
            DraftSize = a >= 5 ? 2 : 3;
            LimitBuildingSlots = a >= 6 ? 22 : 0;
            if (a >= 7) W.Mods.Add(new Modifier(Stat.NightReward, Scope.Any, -0.25f));
            if (a >= 8) { W.Mods.Add(new Modifier(Stat.MaxHp, Scope.Bosses, 0.20f)); W.Mods.Add(new Modifier(Stat.Damage, Scope.Bosses, 0.15f)); }
            DawnRepairFraction = a >= 9 ? 0.6f : 1f;

            BannedFamilies.Clear();
            ChallengeCurses.Clear();
            var ch = Challenge;
            if (ch != null)
            {
                foreach (var f in ch.Banned) BannedFamilies.Add(f);
                W.Mods.AddRange(ch.Modifiers);
                budget *= ch.BudgetMult;
                if (ch.CursedDecrees > 0)
                {
                    // seeded curses: applied as rules (not taken decrees) so save/load and rebuilds stay exact
                    var cursed = new List<DecreeDefinition>();
                    foreach (var id in Content.DecreeOrder) if (Content.Decrees[id].Category == DecreeCategory.Cursed) cursed.Add(Content.Decrees[id]);
                    var rng = Rng.Derive(Setup.Seed, 8100);
                    rng.Shuffle(cursed);
                    for (int i = 0; i < Math.Min(ch.CursedDecrees, cursed.Count); i++)
                    {
                        W.Mods.AddRange(cursed[i].Modifiers);
                        foreach (var h in cursed[i].Hooks) W.Mods.AddHook(h.Hook, h.Value);
                        ChallengeCurses.Add(cursed[i].Id);
                    }
                }
            }

            EndlessMutatorsActive.Clear();
            if (Setup.Endless)
                for (int n = TotalNights + 1; n <= Night; n++)
                {
                    // every endless night hardens the siege on top of its seeded mutator
                    W.Mods.Add(new Modifier(Stat.MaxHp, Scope.Enemies, RunRules.EndlessHpPerNight));
                    W.Mods.Add(new Modifier(Stat.MaxHp, Scope.Bosses, RunRules.EndlessHpPerNight));
                    W.Mods.Add(new Modifier(Stat.Damage, Scope.Enemies, RunRules.EndlessDamagePerNight));
                    W.Mods.Add(new Modifier(Stat.Damage, Scope.Bosses, RunRules.EndlessDamagePerNight));
                    budget *= 1f + RunRules.EndlessBudgetPerNight;
                    var m = RunRules.MutatorFor(Setup.Seed, n);
                    EndlessMutatorsActive.Add(m);
                    W.Mods.AddRange(m.Modifiers);
                    budget *= 1f + m.BudgetBonus;
                }
            BaseBudgetMult = budget;
        }

        /// <summary>
        /// Last Bastion (opt-in decree, never baseline rubber-banding): the first time each night the Castle falls
        /// below 30% HP it gains a shield and the player's General gets half of the Ultimate cooldown back.
        /// </summary>
        public const float LastBastionThreshold = 0.3f;

        private void TickLastBastion()
        {
            if (W.LastBastionUsed || !W.Mods.Has(Hook.LastBastion) || W.Castle == null || W.Castle.Dead) return;
            if (W.Castle.HpFraction >= LastBastionThreshold) return;
            W.LastBastionUsed = true;
            W.Castle.ShieldHp = Math.Max(W.Castle.ShieldHp, W.Castle.MaxHp * W.Mods.HookValue(Hook.LastBastion));
            var g = W.PlayerGeneral;
            if (g != null) g.UltCooldown *= 0.5f;
            W.Analytics.LastBastionTriggers++;
            W.Analytics.BuildLog.Add($"N{Night} last_bastion");
            W.Emit(SimEventType.StatusApplied, W.Castle.Center, 0, ElementId.Light, W.Castle.Id, "bastion");
            W.Emit(SimEventType.Announcement, W.Castle.Center, 0, ElementId.Light, W.Castle.Id, "Thành Lũy Cuối Cùng! Lâu Đài được bảo hộ.");
        }

        public bool CanRetire => Setup.Endless && Phase == RunPhase.Report && Night >= TotalNights;

        /// <summary>Endless: end the run on your own terms after a won night (counts as a victory).</summary>
        public bool RetireEndless()
        {
            if (!CanRetire) return false;
            Phase = RunPhase.Victory;
            Score = ComputeScore(true);
            ChronicleTrack();
            return true;
        }

        /// <summary>One-time setup effects at run creation (a restored save overwrites the economy afterwards).</summary>
        private void OnRunCreated()
        {
            int delta = (Setup.Ascension >= 2 ? -40 : 0) + (Challenge?.StartGoldDelta ?? 0);
            if (delta < 0) Economy.TrySpend(new Cost(Math.Min(Economy.Gold, -delta)), "run_rules");
        }

        /// <summary>Night plan with run rules applied (Ascension 10 adds the map's night-10 boss to the final night).</summary>
        private NightPlan MakePlan() => MakePlanFor(Night);

        private NightPlan MakePlanFor(int night)
        {
            ConfigureWaves();
            var plan = Waves.Plan(night, Math.Max(TotalNights, night), BossOverride);
            if (Setup.Ascension >= 10 && night == TotalNights && W.Map.Bosses != null && W.Map.Bosses.Length > 1 && W.Map.Bosses[1] != null && plan.Lanes.Count > 0)
            {
                plan.Groups.Add(new SpawnGroup { UnitId = W.Map.Bosses[1], Count = 1, LaneId = plan.Lanes[plan.Lanes.Count - 1], Time = 70f });
                plan.Groups.Sort((x, y) => x.Time.CompareTo(y.Time));
            }
            return plan;
        }

        private NightForecast _forecast;
        private NightPlan _forecastFor;

        /// <summary>Tonight's preview, built from the exact plan that will spawn (rolled at day start).</summary>
        public NightForecast Forecast
        {
            get
            {
                if (Plan == null) return null;
                if (_forecastFor != Plan) { _forecast = NightForecast.Build(Plan, Content, W.Map, CastleCenter()); _forecastFor = Plan; }
                return _forecast;
            }
        }

        /// <summary>
        /// Next night's preview (report screen) through the same MakePlan path the next day will use. It is an
        /// Estimate when something still pending can change the roll: a Royal Decree draft (budget hooks such as
        /// Insomnia) or tomorrow's endless mutator.
        /// </summary>
        private NightForecast _next;
        private string _nextKey;

        public NightForecast ForecastNext()
        {
            int next = Night + 1;
            string key = next + "/" + Phase + "/" + TakenDecrees.Count + "/" + W.Mods.All.Count + "/" + (W.Mods.Has(Hook.Insomnia) ? W.Mods.HookValue(Hook.Insomnia) : 0f);
            if (_next != null && _nextKey == key) return _next; // UI asks every frame
            _nextKey = key;
            var reasons = new List<string>();
            if (Setup.Endless && next > TotalNights) reasons.Add("Đột biến vô tận của đêm sau chưa được áp dụng");
            if (DecreeDraftPending) reasons.Add("Chiếu chỉ sắp chọn có thể đổi quy mô đợt quái");
            var plan = MakePlanFor(next);
            return _next = NightForecast.Build(plan, Content, W.Map, CastleCenter(), reasons.Count > 0 ? ForecastConfidence.Estimate : ForecastConfidence.Exact, reasons);
        }

        /// <summary>A Royal Decree draft is still ahead of (or open before) the next day.</summary>
        public bool DecreeDraftPending => Phase == RunPhase.DecreeDraft || (Phase == RunPhase.Report && IsDraftNight(Night) && DraftIndex < DraftsDueBy(Night));

        private Vec2 CastleCenter() => W.Castle != null ? W.Castle.Center : new Vec2(W.Map.CastleOrigin.X + 1.5f, W.Map.CastleOrigin.Y + 1.5f);

        /// <summary>Boss for endless nights (null = not overridden, "" = no boss tonight) and short campaigns.</summary>
        private string RulesBoss(int night)
        {
            if (Setup.Endless && night > TotalNights) return night % 5 == 0 ? RunRules.EndlessBoss(Content, Setup.Seed, night) : "";
            if (TotalNights < 15 && night == TotalNights && W.Map.Bosses != null && W.Map.Bosses.Length > 2) return W.Map.Bosses[2];
            return null;
        }
    }
}
