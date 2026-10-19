using System.Collections.Generic;

namespace Runavelle.Core
{
    public struct SynergyLink
    {
        public ReactionDefinition Reaction;
        public ElementId Mine, Partner;
        public string PartnerSource;
    }

    /// <summary>Quick inspect: which reactions a structure/General can trigger together with nearby allied element sources.</summary>
    public static class Synergy
    {
        public static List<SynergyLink> Near(GameSession s, Vec2 p, float radius, IEnumerable<ElementId> mine)
        {
            var sources = new List<(ElementId e, string name)>();
            foreach (var b in s.W.Buildings)
            {
                if (b.Dead || b.Ruined || Vec2.Distance(b.Center, p) > radius) continue;
                if (b.Level.Attack == null && b.Def.Family != BuildingFamily.Wall) continue;
                if (b.Primary != ElementId.None) sources.Add((b.Primary, b.Level.Name));
                else if (b.Level.Attack != null && b.Level.Attack.Element != ElementId.None) sources.Add((b.Level.Attack.Element, b.Level.Name));
                if (b.Secondary != ElementId.None) sources.Add((b.Secondary, b.Level.Name));
            }
            foreach (var u in s.W.Units)
            {
                if (u.Dead || u.Team != Team.Player || Vec2.Distance(u.Position, p) > radius) continue;
                if (u.Attack.Element != ElementId.None) sources.Add((u.Attack.Element, u.Role == UnitRole.General ? u.General.Name : u.Branch?.Name ?? u.Def.Name));
            }
            var result = new List<SynergyLink>();
            var seen = new HashSet<int>();
            foreach (var m in mine)
            {
                if (m == ElementId.None) continue;
                foreach (var (e, name) in sources)
                {
                    var r = ReactionTable.Get(m, e);
                    if (r == null || r.Id == ReactionId.Resonance || !seen.Add((int)r.Id)) continue;
                    result.Add(new SynergyLink { Reaction = r, Mine = m, Partner = e, PartnerSource = name });
                }
            }
            return result;
        }
    }
}
