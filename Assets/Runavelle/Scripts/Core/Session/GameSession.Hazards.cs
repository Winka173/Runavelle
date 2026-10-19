using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>One optional goal per night; completing it pays a small bonus at dawn.</summary>
    public sealed class SideObjective
    {
        public string Id;
        public string Text;
        public Cost Reward;
    }

    /// <summary>Map hazards (Phase IV), per-map boss rotation and nightly side objectives.</summary>
    public sealed partial class GameSession
    {
        public const float LightningInterval = 9f;
        public const float VentInterval = 13f;
        private float _hazardTimer;
        private float _iceScan;
        private List<Vec2> _vents;
        public SideObjective Objective { get; private set; }

        partial void BossOverrideImpl(int night, ref string boss)
        {
            var rule = RulesBoss(night);
            if (rule != null) { boss = rule; return; }
            int idx = night == 5 ? 0 : night == 10 ? 1 : night == 15 ? 2 : -1;
            if (idx < 0 || W.Map.Bosses == null || idx >= W.Map.Bosses.Length) return;
            var id = W.Map.Bosses[idx];
            if (id != null && Content.Units.ContainsKey(id)) boss = id;
        }

        partial void OnDayStartImpl()
        {
            ApplyDayHazards();
            ChooseObjective();
        }

        /// <summary>Daytime hazard state for the upcoming night (also called at run start / load).</summary>
        public void ApplyDayHazards()
        {
            bool tide = W.Map.Hazard == MapHazard.Tide && Night % 2 == 0;
            if (tide != W.TideHigh || W.CrackedIce.Count > 0) W.TopologyVersion++;
            W.TideHigh = tide;
            W.CrackedIce.Clear();
        }

        private void OnNightStart()
        {
            _hazardTimer = W.Map.Hazard == MapHazard.LightningStrikes ? LightningInterval * 0.6f : VentInterval * 0.6f;
            _iceScan = 0f;
            _playerDowns = 0;
            if (W.Map.Hazard == MapHazard.Tide && W.TideHigh)
                W.Emit(SimEventType.Announcement, W.Castle.Center, 0, ElementId.Water, -1, "Thủy triều lên: bãi cạn bị nhấn chìm, đường đi thay đổi!");
            if (W.Map.Hazard == MapHazard.Mist)
                W.Emit(SimEventType.Announcement, W.Castle.Center, 0, ElementId.Shadow, -1, "Sương mù dày: kẻ địch ẩn mình tới khi lại gần!");
        }

        partial void TickHazardsImpl(float dt)
        {
            switch (W.Map.Hazard)
            {
                case MapHazard.LightningStrikes: TickLightning(dt); break;
                case MapHazard.LavaVents: TickVents(dt); break;
                case MapHazard.ThinIce: TickThinIce(dt); break;
            }
        }

        // ---------------------------------------------------------------- Stormcliff: lightning hits both sides
        private void TickLightning(float dt)
        {
            if ((_hazardTimer -= dt) > 0f) return;
            _hazardTimer = LightningInterval;
            Vec2? at = null;
            var enemies = new List<Unit>();
            foreach (var u in W.Units) if (!u.Dead && u.Team == Team.Enemy && !u.IsBoss) enemies.Add(u);
            if (enemies.Count > 0 && W.Rng.Chance(0.65f)) at = enemies[W.Rng.Range(0, enemies.Count)].Position;
            else
            {
                var towers = new List<Building>();
                foreach (var b in W.Buildings) if (!b.Dead && !b.Ruined && b.Level.Attack != null && b.Def.Family != BuildingFamily.Castle) towers.Add(b);
                if (towers.Count > 0) at = towers[W.Rng.Range(0, towers.Count)].Center + new Vec2(W.Rng.Range(-1.5f, 1.5f), W.Rng.Range(-1.5f, 1.5f));
            }
            if (at == null) return;
            Vec2 p = at.Value;
            Fx.AddTelegraph(p, 1.3f, 1.5f, "Sét Bão", ElementId.Lightning, () =>
            {
                Fx.AreaDamage(Team.Enemy, p, 1.3f, DamageInfo.Simple(70f, ElementId.Lightning, 35f, SourceKind.Hazard, -1, "Sét Bão"), false);
                Fx.AreaDamage(Team.Player, p, 1.3f, DamageInfo.Simple(45f, ElementId.Lightning, 25f, SourceKind.Enemy, -1, "Sét Bão"), false);
                W.Emit(SimEventType.Hazard, p, 1.3f, ElementId.Lightning, -1, "lightning");
            });
        }

        // ---------------------------------------------------------------- Ember: lava vents erupt near lava pools
        private void TickVents(float dt)
        {
            if (_vents == null)
            {
                _vents = new List<Vec2>();
                var m = W.Map;
                for (int y = 1; y < m.Height - 1; y++)
                    for (int x = 1; x < m.Width - 1; x++)
                    {
                        if (!MapDefinition.GroundPassable(m.Get(x, y))) continue;
                        if (m.Get(x + 1, y) == TileType.Lava || m.Get(x - 1, y) == TileType.Lava || m.Get(x, y + 1) == TileType.Lava || m.Get(x, y - 1) == TileType.Lava)
                            _vents.Add(new Int2(x, y).Center);
                    }
            }
            if (_vents.Count == 0 || (_hazardTimer -= dt) > 0f) return;
            _hazardTimer = VentInterval;
            int n = Night >= 8 ? 3 : 2;
            for (int i = 0; i < n; i++)
            {
                Vec2 p = _vents[W.Rng.Range(0, _vents.Count)];
                Fx.AddTelegraph(p, 1.6f, 2.0f, "Miệng Dung Nham", ElementId.Fire, () =>
                {
                    Fx.AreaDamage(Team.Enemy, p, 1.6f, DamageInfo.Simple(60f, ElementId.Fire, 40f, SourceKind.Hazard, -1, "Miệng Dung Nham"), false);
                    Fx.AreaDamage(Team.Player, p, 1.6f, DamageInfo.Simple(40f, ElementId.Fire, 30f, SourceKind.Enemy, -1, "Miệng Dung Nham"), true);
                    W.Emit(SimEventType.Hazard, p, 1.6f, ElementId.Fire, -1, "vent");
                });
            }
        }

        // ---------------------------------------------------------------- Frosthaven: Fire cracks the frozen lake
        private void TickThinIce(float dt)
        {
            if ((_iceScan -= dt) > 0f) return;
            _iceScan = 0.5f;
            var m = W.Map;
            List<Int2> crack = null;
            foreach (var u in W.Units)
            {
                if (u.Dead || u.IsFlying || u.State == UnitState.Burrowed) continue;
                var t = Int2.FromPosition(u.Position);
                if (!m.InBounds(t.X, t.Y) || m.Get(t.X, t.Y) != TileType.Ice || W.CrackedIce.Contains(t.Y * m.Width + t.X)) continue;
                bool burning = u.Status.Gauge[(int)ElementId.Fire] >= StatusState.ReactThreshold;
                if (!burning) continue;
                (crack ?? (crack = new List<Int2>())).Add(t);
            }
            if (crack == null) return;
            foreach (var t in crack)
            {
                if (!W.CrackedIce.Add(t.Y * m.Width + t.X)) continue;
                W.Emit(SimEventType.Hazard, t.Center, 0.5f, ElementId.Water, -1, "ice_crack");
                foreach (var u in W.Units.ToArray())
                {
                    if (u.Dead || u.IsFlying || Int2.FromPosition(u.Position) != t) continue;
                    if (u.Team == Team.Enemy && !u.IsBoss)
                        Combat.Deal(u, DamageInfo.Simple(u.MaxHp * 0.35f, ElementId.Water, 30f, SourceKind.Hazard, -1, "Băng Mỏng"));
                    if (!u.Dead) { var to = Nav.NearestFriendlyPassable(t).Center; u.Position = to; u.PrevPosition = to; }
                }
            }
            W.TopologyVersion++;
        }

        // ---------------------------------------------------------------- side objectives
        private static readonly string[] ObjectiveIds = { "castle_safe", "few_losses", "swift", "no_ruins", "general_alive" };

        public void ChooseObjective()
        {
            var rng = Rng.Derive(Setup.Seed, 9000 + (ulong)Night);
            string id = ObjectiveIds[rng.Range(0, ObjectiveIds.Length)];
            int scale = 1 + Night / 5;
            switch (id)
            {
                case "castle_safe": Objective = new SideObjective { Id = id, Text = "Lâu Đài mất dưới 5% máu", Reward = new Cost(15 * scale, 0) }; break;
                case "few_losses": Objective = new SideObjective { Id = id, Text = "Mất tối đa 2 lính", Reward = new Cost(0, 8 * scale) }; break;
                case "swift": Objective = new SideObjective { Id = id, Text = "Thắng trong " + SwiftLimit + " giây", Reward = new Cost(12 * scale, 4 * scale) }; break;
                case "no_ruins": Objective = new SideObjective { Id = id, Text = "Không công trình nào bị phá", Reward = new Cost(10 * scale, 5 * scale) }; break;
                default: Objective = new SideObjective { Id = id, Text = "Tướng của bạn không gục lần nào", Reward = new Cost(8 * scale, 6 * scale) }; break;
            }
        }

        public int SwiftLimit => 90 + Night * 8;
        private int _playerDowns;

        public bool ObjectiveMet(NightReport rep)
        {
            if (Objective == null) return false;
            switch (Objective.Id)
            {
                case "castle_safe": return rep.CastleDamage < W.Castle.MaxHp * 0.05f;
                case "few_losses": return rep.SoldiersLost <= 2;
                case "swift": return rep.Duration <= SwiftLimit;
                case "no_ruins": return rep.BuildingsLost == 0;
                default: return _playerDowns == 0;
            }
        }

        private void OnNightWon(NightReport rep)
        {
            if (Objective == null) return;
            rep.Objective = Objective.Text;
            rep.ObjectiveReward = Objective.Reward;
            rep.ObjectiveDone = ObjectiveMet(rep);
            if (rep.ObjectiveDone)
            {
                Economy.Add(Objective.Reward, "objective");
                W.Analytics.BuildLog.Add($"N{Night} objective {Objective.Id}");
            }
        }

        private void CountDowns(Actor a)
        {
            if (a is Unit u && u.Role == UnitRole.General && u.IsPlayerControlled) _playerDowns++;
        }
    }
}
