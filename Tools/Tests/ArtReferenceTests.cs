using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Runavelle.Core;

namespace Runavelle.Tests
{
    /// <summary>Every sprite key the game computes at runtime (ArtKeys) must exist in Resources/Art/manifest.json.</summary>
    public class ArtReferenceTests
    {
        private static HashSet<string> Manifest()
        {
            var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Assets", "Runavelle", "Resources", "Art", "manifest.json"));
            var text = File.ReadAllText(path);
            return new HashSet<string>(Regex.Matches(text, "\"([a-z]+/[A-Za-z0-9_]+)\"\\s*:").Select(m => m.Groups[1].Value));
        }

        [Test]
        public void EveryContentSpriteKeyExists()
        {
            var m = Manifest();
            var missing = new List<string>();
            void Need(string k) { if (!m.Contains(k)) missing.Add(k); }
            foreach (var l in T.Db.Levels.Values) Need(ArtKeys.LevelPreview(l, T.Db));
            foreach (var u in T.Db.Units.Values)
            {
                if (u.Role == UnitRole.Enemy)
                    foreach (bool atk in new[] { false, true }) Need(ArtKeys.Unit(new Unit { Def = u, Team = Team.Enemy, Role = UnitRole.Enemy }, atk));
                else if (u.Role == UnitRole.Soldier)
                {
                    foreach (bool atk in new[] { false, true }) Need(ArtKeys.Unit(new Unit { Def = u, Team = Team.Player, Role = UnitRole.Soldier }, atk));
                    foreach (var br in T.Db.SoldierBranches.Values.Where(b => b.Class == u.Class))
                        foreach (bool atk in new[] { false, true }) Need(ArtKeys.Unit(new Unit { Def = u, Team = Team.Player, Role = UnitRole.Soldier, Branch = br }, atk));
                }
            }
            foreach (var g in T.Db.Generals.Values)
            {
                foreach (bool atk in new[] { false, true }) Need(ArtKeys.Unit(new Unit { Def = g.Body, Team = Team.Player, Role = UnitRole.General, General = g }, atk));
                Need("portrait/" + g.Id);
            }
            foreach (var mat in new[] { "wood", "stone", "bastion", "rune" })
            {
                for (int mask = 0; mask < 16; mask++) Need($"building/wall_{mat}_{mask}");
                Need($"building/gate_{mat}_h"); Need($"building/gate_{mat}_v");
            }
            foreach (var e in Elements.All) Need(ElementGrammar.For(e).Icon);
            foreach (var k in new[] { "fortress", "legion", "arcanum" }) { Need("building/emblem_" + k); Need("building/prop_" + k); }
            Assert.IsEmpty(missing, string.Join(", ", missing.Distinct().Take(40)));
        }
    }
}
