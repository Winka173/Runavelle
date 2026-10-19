using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// All gameplay content as data tables with stable string IDs (saves reference these IDs; never rename an ID —
    /// add an entry to <see cref="IdMigrations"/> instead). Numbers are starting values tuned by the headless balance
    /// harness (Docs/Balance_Spec.md), never treated as validated by hand.
    /// </summary>
    public sealed partial class ContentDatabase
    {
        public readonly Dictionary<string, BuildingDefinition> Buildings = new Dictionary<string, BuildingDefinition>();
        public readonly Dictionary<string, BuildingLevel> Levels = new Dictionary<string, BuildingLevel>();
        public readonly Dictionary<string, string> LevelOwner = new Dictionary<string, string>();
        public readonly Dictionary<string, UnitDefinition> Units = new Dictionary<string, UnitDefinition>();
        public readonly Dictionary<string, SoldierBranch> SoldierBranches = new Dictionary<string, SoldierBranch>();
        public readonly Dictionary<string, GeneralDefinition> Generals = new Dictionary<string, GeneralDefinition>();
        public readonly Dictionary<string, BossDefinition> Bosses = new Dictionary<string, BossDefinition>();
        public readonly Dictionary<string, DecreeDefinition> Decrees = new Dictionary<string, DecreeDefinition>();
        public readonly Dictionary<string, EquipmentDefinition> Equipment = new Dictionary<string, EquipmentDefinition>();
        public readonly Dictionary<string, MapDefinition> Maps = new Dictionary<string, MapDefinition>();
        public readonly List<string> GeneralOrder = new List<string>();
        public readonly List<string> BuildingOrder = new List<string>();
        public readonly List<string> MapOrder = new List<string>();
        public readonly List<string> DecreeOrder = new List<string>();
        public readonly List<SoldierClass> SoldierOrder = new List<SoldierClass>();
        /// <summary>Old ID -> new ID, applied when loading saves.</summary>
        public readonly Dictionary<string, string> IdMigrations = new Dictionary<string, string>();

        public const int StartingGold = 140;
        public const int StartingMana = 30;
        public const int TotalNights = 15;
        public static readonly Cost ImbuePrimaryCost = new Cost(10, 25);
        public static readonly Cost ImbueSecondaryCost = new Cost(10, 40);
        public const int ReinforceGoldPerSoldier = 4;

        private static ContentDatabase _default;
        public static ContentDatabase Default => _default ?? (_default = Create());

        public static ContentDatabase Create()
        {
            var db = new ContentDatabase();
            db.AddUnits();
            db.AddBuildings();
            db.AddGenerals();
            db.AddBosses();
            db.AddDecrees();
            db.AddEquipment();
            db.AddMaps();
            db.Validate();
            return db;
        }

        public BuildingLevel Level(string id)
        {
            if (!Levels.TryGetValue(Migrate(id), out var l)) throw new KeyNotFoundException("Unknown building level: " + id);
            return l;
        }

        public UnitDefinition Unit(string id)
        {
            if (!Units.TryGetValue(Migrate(id), out var u)) throw new KeyNotFoundException("Unknown unit: " + id);
            return u;
        }

        public string Migrate(string id) => id != null && IdMigrations.TryGetValue(id, out var n) ? n : id;

        public BuildingDefinition BuildingOfLevel(string levelId) => Buildings[LevelOwner[Migrate(levelId)]];

        public string SoldierUnitId(SoldierClass c) => c.ToString().ToLowerInvariant();

        public SoldierBranch Branch(string id) => id != null && SoldierBranches.TryGetValue(id, out var b) ? b : null;

        // ------------------------------------------------------------------ validation
        /// <summary>Throws if any content breaks a design lock or references a missing ID. Run at startup and in tests.</summary>
        public void Validate()
        {
            var errors = new List<string>();
            var families = new HashSet<BuildingFamily>();
            foreach (var b in Buildings.Values)
            {
                families.Add(b.Family);
                foreach (var r in b.RootLevels)
                    if (!Levels.ContainsKey(r)) errors.Add($"Building {b.Id} root level {r} missing");
            }
            if (families.Count != 8 || Buildings.Count != 8) errors.Add($"Design lock: expected exactly 8 building families, found {families.Count} families / {Buildings.Count} definitions");
            foreach (var l in Levels.Values)
            {
                foreach (var n in l.Next)
                    if (!Levels.ContainsKey(n)) errors.Add($"Level {l.Id} next {n} missing");
                    else if (Levels[n].Tier <= l.Tier) errors.Add($"Level {l.Id} -> {n} does not increase tier");
                    else if (LevelOwner[n] != LevelOwner[l.Id]) errors.Add($"Level {l.Id} -> {n} crosses families");
                if (l.Tier > 3) errors.Add($"Level {l.Id} exceeds tier 3");
                if (l.Soldier != SoldierClass.None && !Units.ContainsKey(SoldierUnitId(l.Soldier))) errors.Add($"Level {l.Id} soldier {l.Soldier} missing");
                if (l.SoldierBranchId != null && !SoldierBranches.ContainsKey(l.SoldierBranchId)) errors.Add($"Level {l.Id} branch {l.SoldierBranchId} missing");
                if (l.Cost.Gold < 0 || l.Cost.Mana < 0) errors.Add($"Level {l.Id} has negative cost");
                if (!Elements.IsValid(l.Element)) errors.Add($"Level {l.Id} invalid element");
                if (l.Attack != null && !Elements.IsValid(l.Attack.Element)) errors.Add($"Level {l.Id} attack invalid element");
            }
            foreach (var u in Units.Values)
            {
                if (u.SummonId != null && !Units.ContainsKey(u.SummonId)) errors.Add($"Unit {u.Id} summon {u.SummonId} missing");
                if (u.BossId != null && !Bosses.ContainsKey(u.BossId)) errors.Add($"Unit {u.Id} boss {u.BossId} missing");
                if (!Elements.IsValid(u.Attack.Element)) errors.Add($"Unit {u.Id} invalid element");
                if (u.ElementTaken != null && u.ElementTaken.Length != Elements.Count) errors.Add($"Unit {u.Id} ElementTaken size");
                if (u.ElementTaken != null)
                    foreach (var t in u.ElementTaken) if (t <= 0f) errors.Add($"Unit {u.Id} has full elemental immunity (forbidden)");
            }
            foreach (var g in Generals.Values)
            {
                if (g.Body == null || g.Ultimate == null || g.Aura == null) errors.Add($"General {g.Id} incomplete");
                if (!Elements.IsValid(g.Element) || g.Element == ElementId.None) errors.Add($"General {g.Id} must have one of the 8 elements");
                if (g.Evolutions.Length != 0 && g.Evolutions.Length != 2) errors.Add($"General {g.Id} must have 0 or 2 evolutions");
            }
            foreach (var b in Bosses.Values)
                foreach (var p in b.Phases)
                    foreach (var a in p.Abilities)
                        if (a.SummonId != null && !Units.ContainsKey(a.SummonId)) errors.Add($"Boss {b.Id} summon {a.SummonId} missing");
            foreach (var d in Decrees.Values)
                foreach (var x in d.Excludes)
                    if (!Decrees.ContainsKey(x)) errors.Add($"Decree {d.Id} excludes unknown {x}");
            foreach (var m in Maps.Values)
            {
                string err = m.Validate();
                if (err != null) errors.Add($"Map {m.Id}: {err}");
            }
            if (errors.Count > 0) throw new InvalidOperationException("Content validation failed:\n" + string.Join("\n", errors));
        }
    }
}
