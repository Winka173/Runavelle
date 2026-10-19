using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum RunPhase { Day, Night, Report, DecreeDraft, GeneralEvolution, KingdomEvolution, Victory, Defeat }

    [Serializable]
    public sealed class RunSetup
    {
        public string MapId = "moonstone_valley";
        public string GeneralId = "roland";
        public ulong Seed = 12345;
        public int Ascension;
        public bool Endless;
        public string ChallengeId;
        public bool IsDaily;
        public string WeaponId;
        public string RelicId;
        public List<string> UnlockedGenerals = new List<string>();
        public int TotalNights = ContentDatabase.TotalNights;
    }

    public sealed class NightReport
    {
        public int Night;
        public bool Victory;
        public float Duration;
        public int Kills;
        public float CastleDamage;
        public int SoldiersLost;
        public int BuildingsLost;
        public Cost Income;
        public Cost Reward;
        public Cost Bounty;
        public int ManaDrained;
        public int ReinforceCost;
        public int Reinforced;
        public List<string> Notes = new List<string>();
    }

    public struct PlaceResult
    {
        public bool Ok;
        public string Reason;
        public Cost Cost;
        public static PlaceResult Fail(string r) => new PlaceResult { Ok = false, Reason = r };
    }

    /// <summary>
    /// One run of Runavelle. Owns the World and all systems; exposes the only mutation API used by presentation,
    /// bots and tests. Deterministic for a given seed + command sequence on the same build/architecture.
    /// </summary>
    public sealed partial class GameSession
    {
        public readonly ContentDatabase Content;
        public readonly RunSetup Setup;
        public readonly World W;
        public readonly Economy Economy;
        public readonly NavigationService Nav;
        public readonly CombatSystem Combat;
        public readonly Spawner Spawner;
        public readonly EffectsSystem Fx;
        public readonly Movement Move;
        public readonly AuraSystem Auras;
        public readonly EnemyAI Enemies;
        public readonly DefenderAI Defenders;
        public readonly GeneralSystem Generals;
        public readonly BossSystem Bosses;
        public readonly WaveDirector Waves;

        public RunPhase Phase { get; private set; } = RunPhase.Day;
        /// <summary>Upcoming (during day) or current (during night) night number, 1-based.</summary>
        public int Night { get; private set; } = 1;
        public int TotalNights => Setup.TotalNights;
        public NightPlan Plan { get; private set; }
        public NightReport LastReport { get; private set; }
        public float NightTime { get; private set; }
        public string DefeatCause;
        public readonly List<string> TakenDecrees = new List<string>();
        public readonly List<string> DecreeOffer = new List<string>();
        public int DraftIndex;
        public int RerollsUsed;
        public string KingdomEvolution;
        public string GeneralEvolutionId;
        public readonly string[] AiGeneralIds = new string[2];
        public readonly string[] AiGeneralLanes = new string[2];
        public readonly bool[] AiSlotCommissioned = new bool[2];
        public readonly List<int> UndoStack = new List<int>();
        public int Score;
        /// <summary>Set at day boundaries; the host saves and clears it.</summary>
        public bool AutosaveRequested;
        private float _enrageAt = 240f;

        public GameSession(ContentDatabase content, RunSetup setup)
        {
            Content = content;
            Setup = setup;
            var map = content.Maps[content.Migrate(setup.MapId)];
            W = new World(content, map, setup.Seed);
            Economy = new Economy(ContentDatabase.StartingGold, ContentDatabase.StartingMana);
            Nav = new NavigationService(W);
            Combat = new CombatSystem(W);
            Spawner = new Spawner(W);
            Fx = new EffectsSystem(W, Combat);
            Move = new Movement(W, Combat);
            Auras = new AuraSystem(W, Combat, Spawner);
            Enemies = new EnemyAI(W, Combat, Fx, Nav, Move, Spawner);
            Defenders = new DefenderAI(W, Combat, Fx, Nav, Move, Spawner);
            Generals = new GeneralSystem(W, Combat, Fx, Nav, Move, Spawner);
            Bosses = new BossSystem(W, Combat, Fx, Nav, Move);
            Waves = new WaveDirector(content, map, setup.Seed);
            Enemies.SpawnSummon = SpawnSummon;
            Bosses.SpawnSummon = SpawnSummon;
            Generals.SpawnBarrier = SpawnBarrier;
            Generals.SpawnAllySummon = SpawnAllySummon;
            Combat.OnKilled = OnKilled;
            ApplyRunRules();

            Spawner.CreateBuilding("castle_1", map.CastleOrigin);
            foreach (var t in map.RuinWalls)
            {
                var rb = Spawner.CreateBuilding("wall_stone", t);
                rb.Ruined = true;
                rb.Hp = 0;
                rb.Invested = new Cost(6);
            }
            var g = content.Generals[content.Migrate(setup.GeneralId)];
            Spawner.SpawnGeneral(g, Generals_RespawnPoint(), true, -1, null);
            W.TopologyVersion++;
            Plan = Waves.Plan(Night, TotalNights, BossOverride);
        }

        private Vec2 Generals_RespawnPoint()
        {
            var c = W.Castle.Center;
            return Nav.NearestFriendlyPassable(Int2.FromPosition(c + new Vec2(0, -2.2f))).Center;
        }

        // ------------------------------------------------------------------ tick
        public void Tick()
        {
            float dt = World.Dt;
            W.Tick++;
            W.Time += dt;
            W.ReactionsThisTick = 0;
            foreach (var u in W.Units) u.PrevPosition = u.Position;
            foreach (var p in W.Projectiles) p.PrevPosition = p.Position;

            if (Phase == RunPhase.Night) NightTick(dt);
            else if (Phase == RunPhase.Day) DayTick(dt);
        }

        private void DayTick(float dt)
        {
            W.RebuildSpatial();
            Auras.Tick(dt);
            Generals.Tick(W.PlayerGeneral, dt);
            foreach (var g in W.AIGenerals) Generals.Tick(g, dt);
            for (int i = 0; i < W.Units.Count; i++)
            {
                var u = W.Units[i];
                if (u.Dead || u.Role != UnitRole.Soldier && u.Role != UnitRole.Summon) continue;
                Defenders.TickSoldier(u, dt);
            }
            CleanupUnits();
        }

        private void NightTick(float dt)
        {
            NightTime += dt;
            SpawnScheduled();
            W.RebuildSpatial();
            Auras.Tick(dt);
            W.CastleGuardTimer = Math.Max(0f, W.CastleGuardTimer - dt);
            Move.EnrageMult = NightTime > _enrageAt ? 1.5f : 1f;

            Generals.Tick(W.PlayerGeneral, dt);
            for (int i = 0; i < W.AIGenerals.Count; i++) Generals.Tick(W.AIGenerals[i], dt);
            int count = W.Units.Count;
            int enemies = 0;
            for (int i = 0; i < count; i++)
            {
                var u = W.Units[i];
                if (u.Dead) continue;
                if (u.Team == Team.Enemy)
                {
                    enemies++;
                    Enemies.Tick(u, dt);
                    if (u.Boss != null && !u.Dead) Bosses.Tick(u, dt);
                }
                else if (u.Role == UnitRole.Soldier || u.Role == UnitRole.Summon) Defenders.TickSoldier(u, dt);
            }
            W.Analytics.PeakEnemies = Math.Max(W.Analytics.PeakEnemies, enemies);
            for (int i = 0; i < W.Buildings.Count; i++) Defenders.TickStructure(W.Buildings[i], dt);
            NightRespawns(dt);
            Fx.TickProjectiles(dt);
            Fx.TickZones(dt);
            Fx.TickTelegraphs(dt);
            TickHazards(dt);
            TickTemporaryStructures(dt);
            CleanupUnits();

            if (W.Castle == null || W.Castle.Dead)
            {
                EndNight(false);
                return;
            }
            if (Plan.AllSpawned && !AnyEnemyAlive()) EndNight(true);
        }

        private bool AnyEnemyAlive()
        {
            foreach (var u in W.Units) if (!u.Dead && u.Team == Team.Enemy) return true;
            return false;
        }

        private void CleanupUnits()
        {
            for (int i = W.Units.Count - 1; i >= 0; i--)
            {
                var u = W.Units[i];
                if (!u.Dead) continue;
                W.Units.RemoveAt(i);
                if (u.Team == Team.Enemy && W.DeadEnemies.Contains(u)) continue; // keep id for revive
                W.ById.Remove(u.Id);
                if (u.OwnerBuildingId >= 0 && W.Get(u.OwnerBuildingId) is Building b) b.Squad.Remove(u.Id);
            }
            if (W.DeadEnemies.Count > 80) W.DeadEnemies.RemoveRange(0, W.DeadEnemies.Count - 80);
        }

        private void TickTemporaryStructures(float dt)
        {
            for (int i = W.Buildings.Count - 1; i >= 0; i--)
            {
                var b = W.Buildings[i];
                if (b.Lifetime < 0f) continue;
                b.Lifetime -= dt;
                if (b.Lifetime <= 0f || b.Ruined) Spawner.RemoveBuilding(b);
            }
        }

        private void SpawnScheduled()
        {
            foreach (var g in Plan.Groups)
            {
                while (g.Spawned < g.Count && NightTime >= g.Time + g.Spawned * g.Interval)
                {
                    var lane = W.Map.Lanes.Find(l => l.Id == g.LaneId) ?? W.Map.Lanes[0];
                    var tile = lane.Tiles[(g.Spawned * 7 + g.UnitId.Length) % lane.Tiles.Count];
                    var pos = tile.Center + new Vec2(W.Rng.Range(-0.3f, 0.3f), W.Rng.Range(-0.3f, 0.3f));
                    var def = Content.Unit(g.UnitId);
                    var u = Spawner.SpawnEnemy(def, pos, lane.Id, Plan.HpMult);
                    if (u.IsBoss) W.Emit(SimEventType.Announcement, pos, 0, ElementId.None, u.Id, def.Name + " xuất hiện!");
                    g.Spawned++;
                }
            }
        }

        private Unit SpawnSummon(string unitId, Vec2 pos, string lane)
        {
            var def = Content.Unit(unitId);
            var u = Spawner.SpawnEnemy(def, pos, lane, Plan != null ? Plan.HpMult : 1f);
            u.Summoned = true;
            return u;
        }

        private Unit SpawnAllySummon(string unitId, Vec2 pos, float lifetime)
        {
            var def = Content.Unit(unitId);
            var u = new Unit { Id = W.AllocId(), Def = def, Team = Team.Player, Role = UnitRole.Summon, kind = ActorKind.Summon, Position = pos, PrevPosition = pos,
                Radius = def.Radius, MaxHp = def.MaxHp, Hp = def.MaxHp, BaseArmor = def.Armor, Attack = def.Attack.Clone(), Speed = def.Speed, Home = pos, Leash = 6f, LifeTimer = lifetime };
            W.Units.Add(u);
            W.ById[u.Id] = u;
            return u;
        }

        private Building SpawnBarrier(string levelId, Vec2 pos)
        {
            var t = Int2.FromPosition(pos);
            if (!W.Map.InBounds(t.X, t.Y) || !MapDefinition.Buildable(W.Map.Get(t.X, t.Y)) || W.BuildingAt(t.X, t.Y) != null) return null;
            foreach (var u in W.Units) if (!u.Dead && Int2.FromPosition(u.Position) == t) return null;
            var b = Spawner.CreateBuilding(levelId, t);
            b.Invested = Cost.Free;
            return b;
        }

        private void NightRespawns(float dt)
        {
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Level.NightRespawnInterval <= 0f) continue;
                if (b.Squad.Count >= Spawner.SquadSize(b)) { b.RespawnTimer = b.Level.NightRespawnInterval; continue; }
                b.RespawnTimer -= dt;
                if (b.RespawnTimer > 0f) continue;
                b.RespawnTimer = b.Level.NightRespawnInterval;
                Spawner.SpawnSoldier(b, b.Center);
            }
        }

        private void OnKilled(Actor a, DamageInfo info)
        {
            if (a == W.Castle) DefeatCause = info.SourceName;
        }

        // ------------------------------------------------------------------ phase flow
        public bool StartNight()
        {
            if (Phase != RunPhase.Day) return false;
            Phase = RunPhase.Night;
            W.IsNight = true;
            W.Night = Night;
            NightTime = 0f;
            _enrageAt = 240f + Night * 6f;
            UndoStack.Clear();
            W.Analytics.ResetNight();
            W.PendingGold = 0;
            W.PendingMana = 0;
            Enemies.ManaDrained = 0;
            W.DeadEnemies.Clear();
            foreach (var g in Plan.Groups) g.Spawned = 0;
            PlaceAIGenerals();
            W.Emit(SimEventType.NightStart, W.Castle.Center, Night);
            return true;
        }

        private void EndNight(bool victory)
        {
            W.IsNight = false;
            W.Projectiles.Clear();
            W.Zones.Clear();
            W.Telegraphs.Clear();
            var rep = new NightReport { Night = Night, Victory = victory, Duration = NightTime, Kills = W.Analytics.NightKills, CastleDamage = W.Analytics.NightCastleDamage,
                SoldiersLost = W.Analytics.NightSoldiersLost, BuildingsLost = W.Analytics.NightBuildingsLost };
            W.Analytics.Nights.Add(new NightRecord { Night = Night, Duration = NightTime, Kills = rep.Kills, CastleDamage = rep.CastleDamage, Gold = Economy.Gold, Mana = Economy.Mana,
                SoldiersLost = rep.SoldiersLost, BuildingsLost = rep.BuildingsLost, Lanes = string.Join(",", Plan.Lanes) });
            LastReport = rep;
            W.Emit(SimEventType.NightEnd, W.Castle.Center, victory ? 1 : 0);
            if (!victory)
            {
                // remove remaining enemies so the defeat screen is calm
                foreach (var u in W.Units) if (u.Team == Team.Enemy) u.Dead = true;
                CleanupUnits();
                Phase = RunPhase.Defeat;
                Score = ComputeScore(false);
                return;
            }
            Dawn(rep);
            Phase = RunPhase.Report;
        }

        /// <summary>Income is paid at dawn (after a won night): Houses/Wells/Castle income + night reward + bounties.</summary>
        private void Dawn(NightReport rep)
        {
            int gold = 0, mana = 0;
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Level.IncomePerDawn.IsFree) continue;
                var sc = World.ScopeOf(b);
                gold += (int)Math.Round(W.Mods.Apply(b.Level.IncomePerDawn.Gold, Stat.GoldIncome, sc));
                mana += (int)Math.Round(W.Mods.Apply(b.Level.IncomePerDawn.Mana, Stat.ManaIncome, sc));
            }
            rep.Income = new Cost(gold, mana);
            float rewardMult = 1f + W.Mods.Percent(Stat.NightReward, Scope.Any);
            rep.Reward = new Cost((int)Math.Round((24 + Night * 4) * rewardMult), (int)Math.Round((8 + Night) * rewardMult));
            rep.Bounty = new Cost(W.PendingGold, W.PendingMana);
            Economy.Add(rep.Income, "income");
            Economy.Add(rep.Reward, "night_reward");
            Economy.Add(rep.Bounty, "bounty");
            if (W.Mods.Has(Hook.CursedGold)) Economy.TrySpend(new Cost(0, Math.Min(Economy.Mana, (int)W.Mods.HookValue(Hook.CursedGold))), "curse");
            rep.ManaDrained = Math.Min(Economy.Mana, Enemies.ManaDrained);
            if (rep.ManaDrained > 0) Economy.TrySpend(new Cost(0, rep.ManaDrained), "mana_drain");

            // repairs: castle full, surviving structures full (free); ruins stay ruined until rebuilt
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined) continue;
                b.Hp = b.MaxHp;
                b.ShieldHp = 0f;
                b.Status.Clear();
                b.DisabledTimer = 0f;
            }
            W.CastleGuardTimer = 0f;
            // soldiers heal; fallen soldiers return for a Gold fee (auto-paid while affordable)
            int perSoldier = Math.Max(0, (int)Math.Round(W.Mods.Apply(ContentDatabase.ReinforceGoldPerSoldier, Stat.ReinforceCost, Scope.Any)));
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Def.Family != BuildingFamily.Barracks) continue;
                foreach (var id in b.Squad) if (W.Get(id) is Unit s) { s.Hp = s.MaxHp; s.Status.Clear(); s.State = UnitState.Idle; }
                int missing = Spawner.SquadSize(b) - b.Squad.Count;
                for (int i = 0; i < missing; i++)
                {
                    if (!Economy.TrySpend(new Cost(perSoldier), "reinforce")) { rep.Notes.Add("Không đủ Vàng để bổ sung lính"); break; }
                    rep.ReinforceCost += perSoldier;
                    rep.Reinforced++;
                    Spawner.SpawnSoldier(b, b.Center);
                }
            }
            foreach (var g in AllGenerals())
            {
                g.State = UnitState.Idle; g.Hp = g.MaxHp; g.Status.Clear(); g.DownedTimer = 0f;
            }
            Score = ComputeScore(false);
        }

        public IEnumerable<Unit> AllGenerals()
        {
            if (W.PlayerGeneral != null) yield return W.PlayerGeneral;
            foreach (var g in W.AIGenerals) yield return g;
        }

        /// <summary>Report acknowledged → draft / evolutions / next day (or victory).</summary>
        public void ContinueFromReport()
        {
            if (Phase != RunPhase.Report) return;
            if (Night >= TotalNights && !Setup.Endless)
            {
                Phase = RunPhase.Victory;
                Score = ComputeScore(true);
                return;
            }
            AdvanceAfterReport();
        }

        private void AdvanceAfterReport()
        {
            if (IsDraftNight(Night) && DraftIndex < DraftsDueBy(Night))
            {
                GenerateDraft();
                if (DecreeOffer.Count > 0) { Phase = RunPhase.DecreeDraft; return; }
                DraftIndex++;
            }
            if (Night >= GeneralEvolutionNight && GeneralEvolutionId == null && W.PlayerGeneral.General.Evolutions.Length == 2) { Phase = RunPhase.GeneralEvolution; return; }
            if (Night >= KingdomEvolutionNight && KingdomEvolution == null && KingdomEvolutionsAvailable) { Phase = RunPhase.KingdomEvolution; return; }
            BeginDay();
        }

        public const int GeneralEvolutionNight = 6;
        public const int KingdomEvolutionNight = 7;
        public static bool IsDraftNight(int night) => night % 2 == 0;
        public static int DraftsDueBy(int night) => night / 2;
        public bool KingdomEvolutionsAvailable => KingdomEvolutions.Count > 0;

        private void BeginDay()
        {
            Night++;
            W.Night = Night;
            Phase = RunPhase.Day;
            RerollsUsed = 0;
            Plan = Waves.Plan(Night, Math.Max(TotalNights, Night), BossOverride);
            OnDayStart();
            AutosaveRequested = true;
        }

        partial void OnDayStartImpl();
        private void OnDayStart() => OnDayStartImpl();

        // ------------------------------------------------------------------ input (night)
        public void SetMoveInput(Vec2 v)
        {
            if (W.PlayerGeneral != null) W.PlayerGeneral.MoveInput = v.ClampMagnitude(1f);
        }

        public void RequestUltimate() => Generals.UltimateRequested = true;
        public void RequestDash() => Generals.DashRequested = true;

        public int ComputeScore(bool victory)
        {
            int nights = Phase == RunPhase.Defeat ? Night - 1 : Night;
            return nights * 1000 + W.Analytics.TotalKills * 5 + (victory ? 5000 : 0) + Setup.Ascension * 1500 + (int)(W.Castle != null ? W.Castle.HpFraction * 500 : 0);
        }
    }
}
