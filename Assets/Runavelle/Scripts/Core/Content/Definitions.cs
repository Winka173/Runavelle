using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum Team { Player = 0, Enemy = 1 }

    /// <summary>The eight (and only eight) base building families. Never add a ninth.</summary>
    public enum BuildingFamily { Castle, House, ManaWell, Wall, ArcherTower, MagicTower, Barracks, Shrine }

    public enum UnitRole { Soldier, Enemy, General, Summon }

    /// <summary>Twelve base soldier types (all trained by the Barracks family).</summary>
    public enum SoldierClass
    {
        None, Swordsman, Spearman, Archer, Mage, Knight, Cavalry,
        ShieldGuardian, Crossbowman, Battlemage, ElementalKnight, GriffinRider, RuneGolem
    }

    public enum EnemyBehavior
    {
        Melee, Ranged, Siege, GateBreaker, Flyer, Buffer, Summoner, Elite, Boss,
        Burrower, Assassin, ManaDrainer, Necromancer, Shifter, Artillery, ShieldCarrier, Healer, Skirmisher, Bomber
    }

    public enum MoveKind { Ground, Flying, Burrow }

    public enum Faction { None, Marauders, UndeadLegion, SiegeConstructs, Ashborn, Frostbound, ShadowCourt, WildTempest }

    public enum GeneralGroup { Vanguard, Battlemage, Commander, Specialist }

    /// <summary>Attack parameters shared by towers, soldiers, generals, enemies.</summary>
    public sealed class AttackSpec
    {
        public float Damage = 10f;
        public float Interval = 1f;
        public float Range = 1.2f;
        public float MinRange;
        public ElementId Element = ElementId.None;
        public float StatusAmount;
        public float Splash;
        public int Targets = 1;
        public bool HitsAir;
        /// <summary>0 = instant (melee / hitscan). Otherwise tiles per second.</summary>
        public float ProjectileSpeed;
        public float StructureMult = 1f;
        public float GateMult = 1f;
        public float LargeMult = 1f;
        public float FlyerMult = 1f;
        public float Knockback;
        /// <summary>Fraction of target armor ignored (0..1).</summary>
        public float ArmorPierce;
        public int ChainCount;
        public float ChainRange = 2.5f;
        public float SlowOnHit;
        public float StunChance;

        public AttackSpec Clone() => (AttackSpec)MemberwiseClone();
        public float Dps => Interval > 0f ? Damage * Targets / Interval : 0f;
    }

    /// <summary>Passive area effect emitted by a structure (shrines, resonance wells, catalyst towers...).</summary>
    public sealed class StructureAura
    {
        public float Radius = 5f;
        public float DamageMult = 1f;
        public float ArmorBonus;
        public float RegenFraction;
        public float StatusMult = 1f;
        public float AttackSpeedMult = 1f;
        public float ReactionMult = 1f;
        public bool AffectsSoldiers = true;
        public bool AffectsStructures = true;
        public bool OnlyMagicTowers;
        /// <summary>Allies inside take this fraction less damage.</summary>
        public float DamageReduction;
        /// <summary>Night pulse: cleanses enemy-applied statuses from allies every few seconds.</summary>
        public bool Cleanse;
    }

    /// <summary>One level of a building. Build = choose a root level; upgrade = choose among <see cref="Next"/>.</summary>
    public sealed class BuildingLevel
    {
        public string Id;
        public string Name;
        public string Description;
        public int Tier = 1;
        /// <summary>"A"/"B" for the final-tier specialization choice, empty otherwise.</summary>
        public string Branch = "";
        public Cost Cost;
        public float MaxHp = 200f;
        public float Armor;
        public Cost IncomePerDawn;
        public AttackSpec Attack;
        public StructureAura Aura;
        public bool IsGate;
        public ElementId Element = ElementId.None;
        /// <summary>Player may imbue a primary element (Gold/Mana cost) on this level.</summary>
        public bool CanImbue;
        /// <summary>Level unlocks a secondary element slot (max one).</summary>
        public bool SecondarySlot;
        public float ReflectFraction;
        public float RegenFraction;
        // Barracks
        public SoldierClass Soldier = SoldierClass.None;
        public string SoldierBranchId;
        public int SquadSize;
        public float SoldierHpMult = 1f;
        public float SoldierDmgMult = 1f;
        public float SoldierArmorBonus;
        /// <summary>Seconds between free reinforcements during the night (0 = none).</summary>
        public float NightRespawnInterval;
        public string[] Next = Array.Empty<string>();
        public string RequiresUnlock;
    }

    public sealed class BuildingDefinition
    {
        public string Id;
        public BuildingFamily Family;
        public string Name;
        public string Description;
        public int Size = 2;
        public string[] RootLevels = Array.Empty<string>();
        public bool PlayerBuildable = true;
        /// <summary>Extra path cost an enemy assigns to breaking through one tile of this structure.</summary>
        public float BreachCost = 8f;
        public int MaxCount = 99;
        public bool IsLinear;
    }

    /// <summary>Per-soldier-type evolution branch chosen at Barracks tier 2.</summary>
    public sealed class SoldierBranch
    {
        public string Id;
        public SoldierClass Class;
        public string Name;
        public string Description;
        public ElementId Element;
        public float HpMult = 1f;
        public float DmgMult = 1f;
        public float ArmorBonus;
        public float RangeBonus;
        public float SpeedMult = 1f;
        public float IntervalMult = 1f;
        public float SplashBonus;
        public int ChainBonus;
        public float SlowOnHit;
        public float StunChance;
        public float ArmorPierce;
        public float LargeMult = 1f;
        public float FlyerMult = 1f;
        public int SquadBonus;
        public float StatusBonus;
    }

    public sealed class UnitDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public UnitRole Role;
        public SoldierClass Class;
        public EnemyBehavior Behavior;
        public Faction Faction;
        public MoveKind Move = MoveKind.Ground;
        public string BossId;
        public float MaxHp = 50f;
        public float Armor;
        public float Speed = 2f;
        public float Radius = 0.3f;
        public bool Large;
        public AttackSpec Attack = new AttackSpec();
        /// <summary>Optional secondary (spell) attack, e.g. Battlemage, Elemental Shifter.</summary>
        public AttackSpec Secondary;
        public float AggroRange = 3f;
        /// <summary>Damage multiplier taken per element (index = ElementId). 1 = normal.</summary>
        public float[] ElementTaken;
        public float CcResist;
        public int ThreatCost = 1;
        public int UnlockNight = 1;
        public int Bounty = 1;
        public string[] Tags = Array.Empty<string>();
        // Behaviour parameters (interpreted per EnemyBehavior / SoldierClass)
        public string SummonId;
        public float SummonInterval;
        public int SummonCount;
        public float AuraRadius;
        public float AuraSpeedMult = 1f;
        public float AuraArmorBonus;
        public float HealPerSecond;
        public float StealthRange;
        public float ManaDrain;
        public float InterceptChance;
        public float TauntRadius;
        public float ShiftInterval;

        public float TakenMult(ElementId e) => ElementTaken == null ? 1f : ElementTaken[(int)e];
        public bool HasTag(string t) => Array.IndexOf(Tags, t) >= 0;
        public bool Flying => Move == MoveKind.Flying;
    }

    public sealed class GeneralAura
    {
        public string Description;
        public float Radius = 4.5f;
        public float DamageMult = 1f;
        public float ArmorBonus;
        public float AttackSpeedMult = 1f;
        public float RangeBonus;
        public float StatusMult = 1f;
        public float RegenFraction;
        public float MoveSpeedMult = 1f;
        public ElementId GrantElement = ElementId.None;
        public float GrantStatus;
        /// <summary>Enemies inside the aura take this much extra damage (0.15 = +15%).</summary>
        public float EnemyVulnerability;
        public float EnemySlow;
        public bool AffectsTowers;
        public bool AffectsWalls;
        public SoldierClass OnlyClass = SoldierClass.None;
        public SoldierClass OnlyClass2 = SoldierClass.None;
    }

    public enum AbilityEffectKind
    {
        AoeDamage, ChainDamage, Zone, AllyShield, AllyHeal, Volley, Mark, CastleShield,
        Barrier, LineStrike, Summon, Pull, Knockwave, FreezeArea, Execute, TroopFrenzy, RepairStructures, SelfBuff
    }

    public enum ZoneKind { None, Burning, Lava, Rain, Mud, Blizzard, Sand, Radiance, Gloom, Gust }

    public sealed class AbilityEffect
    {
        public AbilityEffectKind Kind;
        public float Radius;
        public float Damage;
        public ElementId Element;
        public float Status;
        public int Count;
        public float Duration;
        public float Value;
        public ZoneKind Zone;
        public string SummonId;
    }

    public sealed class UltimateSpec
    {
        public string Name;
        public string Description;
        public float Cooldown = 30f;
        /// <summary>Radius shown as preview ring; also used by AI to decide when to cast.</summary>
        public float CastRadius = 3f;
        public List<AbilityEffect> Effects = new List<AbilityEffect>();
    }

    /// <summary>In-run General evolution (one of two), chosen once per run.</summary>
    public sealed class GeneralEvolution
    {
        public string Id;
        public string Name;
        public string Description;
        public float HpMult = 1f;
        public float DmgMult = 1f;
        public float AuraMult = 1f;
        public float UltCooldownMult = 1f;
        public float UltPowerMult = 1f;
        public List<AbilityEffect> ExtraUltEffects = new List<AbilityEffect>();
        public GeneralAura AuraOverride;
        public AttackSpec AttackOverride;
    }

    public enum UnlockRule { Default, ReachNight5, ReachNight10, FirstVictory, Victories2, Victories3, ReachNight12, MasteryPoints }

    public enum AiStyle { Frontline, Ranged, Support }

    public sealed class GeneralDefinition
    {
        public string Id;
        public string Name;
        public string Title;
        public string Description;
        public GeneralGroup Group;
        public ElementId Element;
        public UnitDefinition Body;
        public GeneralAura Aura;
        public UltimateSpec Ultimate;
        public GeneralEvolution[] Evolutions = Array.Empty<GeneralEvolution>();
        public UnlockRule Unlock;
        public int UnlockValue;
        public AiStyle Ai = AiStyle.Frontline;
        public float DashDistance = 3.5f;
        public float DashCooldown = 6f;
        /// <summary>RGB 0..1 signature colour for presentation.</summary>
        public float R, G, B;
    }
}
