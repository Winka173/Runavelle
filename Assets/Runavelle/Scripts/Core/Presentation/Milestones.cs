using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// Run milestones (first elemental squad, first evolved tower/wall, specialisation, kingdom path, large siege,
    /// final boss night…). Each fires once per run; Prime() marks what a resumed save already achieved so a reload
    /// does not replay celebrations.
    /// </summary>
    public sealed class MilestoneTracker
    {
        public readonly HashSet<string> Reached = new HashSet<string>();
        public readonly List<(string Id, string Text)> Pending = new List<(string, string)>();

        public static readonly (string Id, string Text)[] All =
        {
            ("settlement", "Làng đầu tiên: 3 Nhà Dân đã dựng"),
            ("elemental_squad", "Đội quân nguyên tố đầu tiên"),
            ("tower_t3", "Tháp tiến hoá đầu tiên (bậc 3)"),
            ("wall_stone", "Tường đá đầu tiên"),
            ("specialized", "Chuyên môn hoá: nhánh cuối đầu tiên"),
            ("first_reaction", "Phản ứng nguyên tố đầu tiên"),
            ("kingdom_path", "Vương quốc đã chọn con đường"),
            ("large_siege", "Đại công thành: 30 kẻ địch cùng lúc"),
            ("final_night", "Đêm cuối cùng – trận quyết định"),
        };

        public static string TextOf(string id)
        {
            foreach (var m in All) if (m.Id == id) return m.Text;
            return id;
        }

        private void Hit(string id)
        {
            if (Reached.Add(id)) Pending.Add((id, TextOf(id)));
        }

        /// <summary>Marks every milestone the current state already satisfies, without announcing it.</summary>
        public void Prime(GameSession s)
        {
            Check(s);
            Pending.Clear();
        }

        public void Observe(in SimEvent e, GameSession s)
        {
            if (e.Type == SimEventType.Reaction && e.Text != "arc" && e.Text != "chain") Hit("first_reaction");
            if (e.Type == SimEventType.Building || e.Type == SimEventType.Upgrade || e.Type == SimEventType.NightStart) Check(s);
        }

        public void Check(GameSession s)
        {
            var w = s.W;
            int houses = 0, alive = 0;
            foreach (var b in w.Buildings)
            {
                if (b.Dead || b.Ruined) continue;
                var l = b.Level;
                if (b.Def.Family == BuildingFamily.House) houses++;
                if ((b.Def.Family == BuildingFamily.ArcherTower || b.Def.Family == BuildingFamily.MagicTower) && l.Tier >= 3) Hit("tower_t3");
                if (b.IsWallLike && (l.Id.EndsWith("stone") || l.Tier >= 2)) Hit("wall_stone");
                if (!string.IsNullOrEmpty(l.Branch)) Hit("specialized");
                if (b.Def.Family == BuildingFamily.Barracks && (b.Primary != ElementId.None || l.Element != ElementId.None)) Hit("elemental_squad");
            }
            foreach (var u in w.Units)
                if (!u.Dead && u.Team == Team.Player && u.Role == UnitRole.Soldier && u.Attack != null && u.Attack.Element != ElementId.None) { Hit("elemental_squad"); break; }
            foreach (var u in w.Units) if (!u.Dead && u.Team == Team.Enemy) alive++;
            if (houses >= 3) Hit("settlement");
            if (s.KingdomEvolution != null) Hit("kingdom_path");
            if (w.IsNight && alive >= 30) Hit("large_siege");
            if (w.IsNight && s.Night == s.TotalNights) Hit("final_night");
        }

        public bool TryPop(out (string Id, string Text) m)
        {
            if (Pending.Count == 0) { m = default; return false; }
            m = Pending[0];
            Pending.RemoveAt(0);
            return true;
        }
    }
}
