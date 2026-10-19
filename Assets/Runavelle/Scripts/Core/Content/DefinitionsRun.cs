using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum BossAbilityKind { Slam, Summon, Meteor, Charge, FreezeTowers, DisruptTower, Teleport, Revive, Gust, Breath, ShieldSelf, TidalWave, CurseZone }

    public sealed class BossAbility
    {
        public BossAbilityKind Kind;
        public string Name;
        public float Cooldown = 10f;
        public float InitialDelay = 4f;
        /// <summary>Warning time between the visible telegraph and the effect.</summary>
        public float Telegraph = 1.5f;
        public float Radius = 2f;
        public float Damage;
        public ElementId Element;
        public float Status;
        public int Count = 1;
        public string SummonId;
        public float Value;
        /// <summary>After the ability resolves, the boss is Exhausted (takes +50% damage) for this long.</summary>
        public float ExhaustAfter;
    }

    public sealed class BossPhase
    {
        public string Name;
        /// <summary>Phase becomes active when HP fraction drops to or below this value (first phase uses 1).</summary>
        public float StartsAtHp = 1f;
        public float SpeedMult = 1f;
        public float AttackSpeedMult = 1f;
        public float DamageTakenMult = 1f;
        public string Announcement;
        public List<BossAbility> Abilities = new List<BossAbility>();
    }

    public sealed class BossDefinition
    {
        public string Id;
        public string Name;
        public string Counterplay;
        public List<BossPhase> Phases = new List<BossPhase>();
    }

    public enum DecreeCategory { Economy, Fortress, Army, General, Elemental, Cursed }
    public enum Rarity { Common, Rare, Epic }

    public sealed class DecreeDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        public DecreeCategory Category;
        public Rarity Rarity;
        public float Weight = 1f;
        public int MaxStacks = 1;
        public int MinNight;
        /// <summary>Offered only if the kingdom owns at least one building of this family (-1 = no requirement).</summary>
        public int RequireFamily = -1;
        public SoldierClass RequireClass = SoldierClass.None;
        public ElementId RequireElement = ElementId.None;
        public string[] Excludes = Array.Empty<string>();
        public List<Modifier> Modifiers = new List<Modifier>();
        public List<HookValue> Hooks = new List<HookValue>();
        public Cost Grant;
    }

    public enum EquipmentSlot { Weapon, Relic }

    public sealed class EquipmentDefinition
    {
        public string Id;
        public EquipmentSlot Slot;
        public string Name;
        public string Description;
        public List<Modifier> Modifiers = new List<Modifier>();
        public List<HookValue> Hooks = new List<HookValue>();
        public UnlockRule Unlock;
        public int UnlockValue;
        /// <summary>Weapon: General basic attacks also apply this element (the one allowed secondary element).</summary>
        public ElementId SecondaryElement = ElementId.None;
    }
}
