using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public sealed class KingdomEvolutionDef
    {
        public string Id;
        public string Name;
        public string Description;
        public List<Modifier> Modifiers = new List<Modifier>();
        public List<HookValue> Hooks = new List<HookValue>();
    }

    /// <summary>Run-level choices: Royal Decrees, AI Generals, evolutions, run rules (ascension/challenges/equipment), hazards.</summary>
    public sealed partial class GameSession
    {
        /// <summary>Ascension rule: max non-wall buildings (0 = unlimited).</summary>
        public int LimitBuildingSlots;
        public readonly List<KingdomEvolutionDef> KingdomEvolutions = new List<KingdomEvolutionDef>();

        partial void ApplyRunRulesImpl();
        private void ApplyRunRules()
        {
            W.Mods.Clear();
            ApplyEquipment();
            ApplyRunRulesImpl();
        }

        private void ApplyEquipment()
        {
            Spawner.EquippedWeapon = null;
            foreach (var id in new[] { Setup.WeaponId, Setup.RelicId })
            {
                if (id == null || !Content.Equipment.TryGetValue(id, out var eq)) continue;
                W.Mods.AddRange(eq.Modifiers);
                foreach (var h in eq.Hooks) W.Mods.AddHook(h.Hook, h.Value);
                if (eq.Slot == EquipmentSlot.Weapon) Spawner.EquippedWeapon = eq;
            }
        }

        /// <summary>Rebuilds all run modifiers from scratch (rules, equipment, decrees, kingdom) — used when the loadout changes.</summary>
        public void RebuildModifiers()
        {
            ApplyRunRules();
            foreach (var id in TakenDecrees)
            {
                var d = Content.Decrees[id];
                W.Mods.AddRange(d.Modifiers);
                foreach (var h in d.Hooks) W.Mods.AddHook(h.Hook, h.Value);
            }
            var k = KingdomEvolutions.Find(x => x.Id == KingdomEvolution);
            if (k != null) { W.Mods.AddRange(k.Modifiers); foreach (var h in k.Hooks) W.Mods.AddHook(h.Hook, h.Value); }
            RecomputeAllStats();
        }

        /// <summary>Pre-night loadout change (day only). Slot = Weapon or Relic; null id unequips.</summary>
        public bool SetEquipment(EquipmentSlot slot, string id)
        {
            if (Phase != RunPhase.Day) return false;
            if (id != null && (!Content.Equipment.TryGetValue(id, out var eq) || eq.Slot != slot)) return false;
            if (slot == EquipmentSlot.Weapon) Setup.WeaponId = id; else Setup.RelicId = id;
            RebuildModifiers();
            return true;
        }

        public float BaseBudgetMult = 1f;

        private void ConfigureWaves()
        {
            Waves.BudgetMult = BaseBudgetMult * (1f + (W.Mods.Has(Hook.Insomnia) ? W.Mods.HookValue(Hook.Insomnia) : 0f));
        }

        partial void BossOverrideImpl(int night, ref string boss);
        private string BossOverride(int night)
        {
            string boss = null;
            BossOverrideImpl(night, ref boss);
            return boss;
        }

        partial void TickHazardsImpl(float dt);
        private void TickHazards(float dt) => TickHazardsImpl(dt);

        // ------------------------------------------------------------------ AI Generals
        public int AiSlotsAvailable => Night >= 9 ? 2 : Night >= 4 ? 1 : 0;
        public static Cost CommissionCost(int slot) => slot == 0 ? new Cost(60, 30) : new Cost(90, 50);

        public List<string> AvailableAiGenerals()
        {
            var list = new List<string>();
            foreach (var id in Content.GeneralOrder)
            {
                if (id == W.PlayerGeneral.General.Id) continue;
                if (Setup.UnlockedGenerals.Count > 0 && !Setup.UnlockedGenerals.Contains(id)) continue;
                if (Array.IndexOf(AiGeneralIds, id) >= 0) continue;
                list.Add(id);
            }
            return list;
        }

        public bool CommissionAiGeneral(int slot, string generalId)
        {
            if (Phase != RunPhase.Day || slot < 0 || slot >= AiSlotsAvailable || AiSlotCommissioned[slot]) return false;
            if (!AvailableAiGenerals().Contains(generalId)) return false;
            if (!Economy.TrySpend(CommissionCost(slot), "ai_general")) return false;
            AiSlotCommissioned[slot] = true;
            AiGeneralIds[slot] = generalId;
            AiGeneralLanes[slot] = AiGeneralLanes[slot] ?? DefaultLaneFor(slot);
            var g = Spawner.SpawnGeneral(Content.Generals[generalId], Generals_RespawnPoint(), false, slot, null);
            Generals.Posts[slot] = Generals.PostForLane(LaneById(AiGeneralLanes[slot]));
            W.Analytics.BuildLog.Add($"N{Night} ai_general {generalId} slot{slot}");
            return true;
        }

        /// <summary>Single-tap sector assignment, daytime only.</summary>
        public bool AssignAiLane(int slot, string laneId)
        {
            if (Phase != RunPhase.Day || !AiSlotCommissioned[slot] || LaneById(laneId) == null) return false;
            AiGeneralLanes[slot] = laneId;
            Generals.Posts[slot] = Generals.PostForLane(LaneById(laneId));
            return true;
        }

        public SpawnLane LaneById(string id) => W.Map.Lanes.Find(l => l.Id == id);

        /// <summary>Usable default: the slot covers the most threatened front of the next night not covered by the other slot.</summary>
        public string DefaultLaneFor(int slot)
        {
            var other = AiGeneralLanes[1 - slot];
            foreach (var l in Plan.Lanes) if (l != other) return l;
            foreach (var l in W.Map.Lanes) if (!l.FlyerOnly && l.Id != other) return l.Id;
            return W.Map.Lanes[0].Id;
        }

        private void PlaceAIGenerals()
        {
            foreach (var g in W.AIGenerals)
            {
                if (AiGeneralLanes[g.AiSlot] == null) AiGeneralLanes[g.AiSlot] = DefaultLaneFor(g.AiSlot);
                var post = Generals.PostForLane(LaneById(AiGeneralLanes[g.AiSlot]));
                Generals.Posts[g.AiSlot] = post;
            }
        }

        // ------------------------------------------------------------------ Royal Decrees
        public int DecreeStacks(string id)
        {
            int n = 0;
            foreach (var t in TakenDecrees) if (t == id) n++;
            return n;
        }

        public bool DecreeEligible(DecreeDefinition d)
        {
            if (DecreeStacks(d.Id) >= d.MaxStacks) return false;
            if (Night < d.MinNight) return false;
            if (d.RequireFamily >= 0 && W.CountFamily((BuildingFamily)d.RequireFamily) == 0) return false;
            if (d.RequireClass != SoldierClass.None)
            {
                bool has = false;
                foreach (var b in W.Buildings) if (!b.Dead && b.Level.Soldier == d.RequireClass) has = true;
                if (!has) return false;
            }
            if (d.RequireElement != ElementId.None && !KingdomUsesElement(d.RequireElement)) return false;
            foreach (var x in d.Excludes) if (TakenDecrees.Contains(x)) return false;
            foreach (var t in TakenDecrees)
                if (Content.Decrees.TryGetValue(t, out var td) && Array.IndexOf(td.Excludes, d.Id) >= 0) return false;
            return true;
        }

        public bool KingdomUsesElement(ElementId e)
        {
            if (W.PlayerGeneral.General.Element == e) return true;
            foreach (var g in W.AIGenerals) if (g.General.Element == e) return true;
            foreach (var b in W.Buildings)
                if (!b.Dead && (b.Primary == e || b.Secondary == e || b.Level.Element == e)) return true;
            return false;
        }

        /// <summary>Seeded 1-of-3 offer. Guarantees at least one non-cursed option and distinct categories where possible.</summary>
        public void GenerateDraft()
        {
            DecreeOffer.Clear();
            var rng = Rng.Derive(Setup.Seed, 5000 + (ulong)DraftIndex * 31 + (ulong)RerollsUsed * 7);
            var pool = new List<DecreeDefinition>();
            foreach (var id in Content.DecreeOrder)
            {
                var d = Content.Decrees[id];
                if (DecreeEligible(d)) pool.Add(d);
            }
            var cats = new HashSet<DecreeCategory>();
            int guard = 0;
            while (DecreeOffer.Count < DraftSize && pool.Count > 0 && guard++ < 50)
            {
                var w = new List<float>();
                foreach (var d in pool)
                {
                    float x = d.Weight;
                    if (cats.Contains(d.Category)) x *= 0.35f;
                    if (d.Category == DecreeCategory.Cursed && DecreeOffer.Count == DraftSize - 1 && AllOfferCursed()) x = 0f;
                    w.Add(x);
                }
                int idx = rng.PickWeighted(w);
                if (idx < 0) break;
                DecreeOffer.Add(pool[idx].Id);
                cats.Add(pool[idx].Category);
                pool.RemoveAt(idx);
            }
        }

        private bool AllOfferCursed()
        {
            foreach (var id in DecreeOffer) if (Content.Decrees[id].Category != DecreeCategory.Cursed) return false;
            return true;
        }

        public static readonly Cost RerollCost = new Cost(15, 5);

        public bool RerollDraft()
        {
            if (Phase != RunPhase.DecreeDraft || RerollsUsed >= 1 || DraftSize < 3 || !Economy.TrySpend(RerollCost, "reroll")) return false;
            RerollsUsed++;
            GenerateDraft();
            return true;
        }

        public bool ChooseDecree(string id)
        {
            if (Phase != RunPhase.DecreeDraft || !DecreeOffer.Contains(id)) return false;
            ApplyDecree(Content.Decrees[id]);
            DecreeOffer.Clear();
            DraftIndex++;
            RerollsUsed = 0;
            AdvanceAfterReport();
            return true;
        }

        public void ApplyDecree(DecreeDefinition d)
        {
            TakenDecrees.Add(d.Id);
            W.Mods.AddRange(d.Modifiers);
            foreach (var h in d.Hooks) W.Mods.AddHook(h.Hook, h.Value);
            if (!d.Grant.IsFree) Economy.Add(d.Grant, "decree");
            RecomputeAllStats();
            W.Analytics.BuildLog.Add($"N{Night} decree {d.Id}");
        }

        // ------------------------------------------------------------------ evolutions
        public bool ChooseGeneralEvolution(int index)
        {
            if (Phase != RunPhase.GeneralEvolution) return false;
            var evos = W.PlayerGeneral.General.Evolutions;
            if (index < 0 || index >= evos.Length) return false;
            GeneralEvolutionId = evos[index].Id;
            W.PlayerGeneral.Evolution = evos[index];
            Spawner.ApplyGeneralStats(W.PlayerGeneral, true);
            W.Analytics.BuildLog.Add($"N{Night} general_evolution {GeneralEvolutionId}");
            AdvanceAfterReport();
            return true;
        }

        public bool ChooseKingdomEvolution(string id)
        {
            if (Phase != RunPhase.KingdomEvolution) return false;
            var k = KingdomEvolutions.Find(x => x.Id == id);
            if (k == null) return false;
            KingdomEvolution = id;
            W.Mods.AddRange(k.Modifiers);
            foreach (var h in k.Hooks) W.Mods.AddHook(h.Hook, h.Value);
            RecomputeAllStats();
            W.Analytics.BuildLog.Add($"N{Night} kingdom {id}");
            AdvanceAfterReport();
            return true;
        }

        // ------------------------------------------------------------------ Army Rally (P2)
        /// <summary>Dusk: assign posts and let squads march while the bell rings. Skippable — StartNight works without it.</summary>
        public void BeginRally()
        {
            if (Phase != RunPhase.Day || W.Rally) return;
            AssignRallyPosts();
            PlaceAIGenerals(); // AI generals get their zones at the bell too
            W.Rally = true;
        }

        /// <summary>Deterministic posts: each barracks musters toward its nearest ground front from tonight's forecast,
        /// marching in a straight line and stopping before the first wall/gate/blocked tile (squads stay inside the walls).</summary>
        public void AssignRallyPosts()
        {
            var f = Forecast;
            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Family != BuildingFamily.Barracks) continue;
                b.RallyPost = b.Ruined || f == null ? (Vec2?)null : RallyPostFor(b, f, out b.RallyDir);
            }
        }

        private Vec2? RallyPostFor(Building b, NightForecast f, out Vec2 dir)
        {
            dir = new Vec2(1, 0);
            SpawnLane best = null;
            float bestD = float.MaxValue;
            foreach (var fr in f.Fronts)
            {
                if (fr.FlyerOnly) continue;
                var lane = LaneById(fr.LaneId);
                if (lane == null) continue;
                float d = Vec2.Distance(b.Center, lane.Center);
                if (d < bestD) { bestD = d; best = lane; }
            }
            if (best == null) return null;
            dir = (best.Center - b.Center).Normalized;
            if (dir.SqrMagnitude < 0.01f) return null;
            var start = b.Center + dir * (b.Size * 0.5f + 0.9f);
            Vec2? last = null;
            for (float t = 0f; t <= AiTuning.RallyDistance; t += 0.5f)
            {
                var p = start + dir * t;
                var tile = Int2.FromPosition(p);
                if (!W.FriendlyPassable(tile.X, tile.Y)) break;
                var occ = W.BuildingAt(tile.X, tile.Y);
                if (occ != null && occ.IsWallLike && !occ.Ruined) break; // open gates count as the wall line too
                last = p;
            }
            if (last == null) return null;
            var post = last.Value - dir * 0.6f;
            return Vec2.Distance(post, b.Center) < b.Size * 0.5f + 0.5f ? (Vec2?)null : post;
        }
    }
}