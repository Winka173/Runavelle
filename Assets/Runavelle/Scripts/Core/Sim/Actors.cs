using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum ActorKind { Building, Soldier, Enemy, General, Summon }

    /// <summary>Who caused damage (for attribution / analytics). Reactions keep the source of the triggering hit.</summary>
    public enum SourceKind { None, Castle, ArcherTower, MagicTower, Soldier, PlayerGeneral, AIGeneral, Wall, Zone, Reaction, Enemy, Hazard }

    public abstract class Actor
    {
        public int Id;
        public Team Team;
        public Vec2 Position;
        public Vec2 PrevPosition;
        public float Hp;
        public float MaxHp;
        public float BaseArmor;
        public float Radius = 0.3f;
        public bool Dead;
        public readonly StatusState Status = new StatusState();
        public readonly AuraBuffs Buffs = new AuraBuffs();
        public abstract ActorKind Kind { get; }
        public virtual bool IsStructure => false;
        public virtual bool IsFlying => false;
        public virtual bool Targetable => !Dead;
        public float HpFraction => MaxHp > 0 ? Hp / MaxHp : 0f;
        /// <summary>Damage multiplier taken per element (enemy resistances).</summary>
        public virtual float TakenMult(ElementId e) => 1f;
        public virtual bool IsBoss => false;
        public virtual bool IsElite => false;
        public virtual bool IsLarge => false;
        public float LastHitTime = -999f;
    }

    public sealed class Building : Actor
    {
        public BuildingDefinition Def;
        public BuildingLevel Level;
        public Int2 Origin;
        public int Size;
        public bool Ruined;
        public int DayBuilt;
        public int TickBuilt;
        public Cost Invested;
        public ElementId Primary = ElementId.None;
        public ElementId Secondary = ElementId.None;
        public float AttackCooldown;
        public int ShotCounter;
        public float RespawnTimer;
        public readonly List<int> Squad = new List<int>();
        public float DisabledTimer;
        public float ShieldHp;
        /// <summary>Temporary structures (General barriers) expire after this many seconds; &lt;0 = permanent.</summary>
        public float Lifetime = -1f;
        public override ActorKind Kind => ActorKind.Building;
        public override bool IsStructure => true;
        public override bool Targetable => !Dead && !Ruined;
        public BuildingFamily Family => Def.Family;
        public bool IsWallLike => Def.Family == BuildingFamily.Wall;
        public bool IsGate => Level.IsGate;
        public Vec2 Center => new Vec2(Origin.X + Size * 0.5f, Origin.Y + Size * 0.5f);

        public ElementId AttackElement(bool secondaryShot)
        {
            if (secondaryShot && Secondary != ElementId.None) return Secondary;
            if (Primary != ElementId.None) return Primary;
            return Level.Attack != null ? Level.Attack.Element : ElementId.None;
        }
    }

    public enum UnitState { Idle, Moving, Fighting, Returning, Retreating, Downed, Burrowed, Fleeing }

    public sealed class Unit : Actor
    {
        public UnitDefinition Def;
        public UnitRole Role;
        public ActorKind kind;
        public override ActorKind Kind => kind;
        public override bool IsFlying => Def.Move == MoveKind.Flying;
        public override bool IsBoss => Def.Behavior == EnemyBehavior.Boss && Role == UnitRole.Enemy;
        public override bool IsElite => Def.Behavior == EnemyBehavior.Elite || IsBoss;
        public override bool IsLarge => Def.Large;
        public override bool Targetable => !Dead && State != UnitState.Downed && !(State == UnitState.Burrowed) && !Stealthed;
        public override float TakenMult(ElementId e) => Def.TakenMult(e) * (ShiftedResist != ElementId.None && e == ShiftedResist ? 0.5f : 1f);

        public AttackSpec Attack;
        public AttackSpec SecondaryAttack;
        public float Speed;
        public float AttackCooldown;
        public float SecondaryCooldown;
        public int TargetId = -1;
        public UnitState State;
        public Vec2 Home;
        public float Leash = 6f;
        public int OwnerBuildingId = -1;
        public ElementId SecondaryElement = ElementId.None;
        public SoldierBranch Branch;
        public readonly List<Int2> Path = new List<Int2>();
        public int PathIndex;
        public float RepathTimer;
        public string LaneId;
        public float SpawnTime;
        public float StuckTimer;
        public Vec2 LastProgressPos;
        // abilities
        public float SummonTimer;
        public float ShiftTimer;
        public ElementId ShiftedResist = ElementId.None;
        public bool Stealthed;
        /// <summary>Reveal radius while stealthed (unit definition or map Mist).</summary>
        public float StealthRange;
        /// <summary>Ascension elite affix name (null = none).</summary>
        public string Affix;
        public float BurrowTimer;
        public float DashCooldown;
        public float UltCooldown;
        public float DownedTimer;
        public float IFrames;
        public float AiDecisionTimer;
        public string AiReason = "";
        /// <summary>Seconds spent chasing the current target without reaching it (pursuit cap for fast soldiers).</summary>
        public float ChaseTimer;
        public int IgnoreTargetId = -1;
        public float IgnoreTimer;
        public int ReviveCount;
        public bool Summoned;
        public float LifeTimer = -1f;
        public float ExhaustedTimer;
        public float FacingX = 1f;
        public float AttackAnim;
        // generals
        public GeneralDefinition General;
        public bool IsPlayerControlled;
        public int AiSlot = -1;
        public Vec2 MoveInput;
        public Vec2 Velocity;
        public GeneralEvolution Evolution;
        public BossRuntime Boss;
        /// <summary>Veteran Core decree: nights survived (0..3).</summary>
        public int Veterancy;
    }

    public sealed class BossRuntime
    {
        public BossDefinition Def;
        public int PhaseIndex;
        public readonly List<float> Cooldowns = new List<float>();
        public int PendingTelegraph = -1;
    }

    public sealed class Projectile
    {
        public int Id;
        public Team Team;
        public Vec2 Position;
        public Vec2 PrevPosition;
        public int TargetId;
        public Vec2 TargetPos;
        public float Speed;
        public DamageInfo Payload;
        public float Splash;
        public bool Dead;
        public ElementId Element;
        public int Visual; // 0 arrow, 1 orb, 2 rock, 3 bolt
        public ElementId GrantElement;
        public float GrantStatus;
    }

    public sealed class Zone
    {
        public int Id;
        public ZoneKind Kind;
        public Team Team;
        public Vec2 Position;
        public float Radius;
        public float Remaining;
        public float Duration;
        public float Damage;
        public ElementId Element;
        public float Status;
        public float TickTimer;
        public SourceKind Source;
        public int SourceActorId;
        public string SourceName;
        /// <summary>Elemental Battlefield: zone left behind by a reaction (bounded count, refresh instead of stacking).</summary>
        public bool FromReaction;
    }

    /// <summary>A visible warning that resolves into an effect after Delay seconds (boss attacks, hazards).</summary>
    public sealed class Telegraph
    {
        public int Id;
        public Vec2 Position;
        public float Radius;
        public float Remaining;
        public float Total;
        public Action OnResolve;
        public string Label;
        public bool Line;
        public Vec2 LineEnd;
        public ElementId Element;
    }

    /// <summary>Damage payload carried by attacks/projectiles/effects.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public ElementId Element;
        public float Status;
        public SourceKind Source;
        public int SourceActorId;
        public string SourceName;
        public bool IsReaction;
        public bool IsDot;
        public float ArmorPierce;
        public float StructureMult;
        public float GateMult;
        public float LargeMult;
        public float FlyerMult;
        public float Knockback;
        public Vec2 Origin;
        public float SlowOnHit;
        public float StunChance;
        public int ChainCount;
        public float ChainRange;
        /// <summary>Reaction generation: 0 for direct hits; reaction-spawned damage cannot trigger more reactions.</summary>
        public int Generation;

        public static DamageInfo Simple(float amount, ElementId e, float status, SourceKind src, int srcId, string srcName)
            => new DamageInfo { Amount = amount, Element = e, Status = status, Source = src, SourceActorId = srcId, SourceName = srcName, StructureMult = 1f, GateMult = 1f, LargeMult = 1f, FlyerMult = 1f };
    }

    public enum SimEventType
    {
        Hit, Kill, Reaction, BuildingDestroyed, WallBreached, Spawned, UltimateCast, Dash, BossPhase, Announcement, Heal,
        ProjectileFired, MeleeSwing, CastleHit, GeneralDowned, GeneralRevived, StatusApplied, Telegraph, Freeze, Summon, Building, Upgrade, Sell,
        NightStart, NightEnd, Hazard, Stealth, Explosion
    }

    public struct SimEvent
    {
        public SimEventType Type;
        public Vec2 Position;
        public float Value;
        public ElementId Element;
        public int ActorId;
        public int OtherId;
        public string Text;
        public ReactionId Reaction;
    }
}
