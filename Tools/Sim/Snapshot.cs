using System.Collections.Generic;
using System.IO;
using Runavelle.Core;

/// <summary>Plays a real seeded run with the AutoPlayer and dumps the world state mid-night as JSON for
/// Tools/Art/render_snapshot.py (a faithful headless preview built from the same sprites; NOT a Unity capture).</summary>
public static class Snapshot
{
    public static void Write(string map, string general, int night, float seconds, string path, string strategy = "Balanced")
    {
        var db = ContentDatabase.Create();
        var s = new GameSession(db, new RunSetup { MapId = map, GeneralId = general, Seed = 4242 });
        var bot = new AutoPlayer(s, System.Enum.Parse<AutoPlayer.Strategy>(strategy), 4242);
        while (true)
        {
            while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat && s.Phase != RunPhase.Victory) bot.HandleChoices();
            if (s.Phase != RunPhase.Day) break;
            s.Economy.Add(new Cost(60, 30), "snapshot-showcase"); // richer kingdom for a representative picture
            bot.PlayDay();
            for (int i = 0; i < 40; i++) s.Tick();
            if (s.Night == night && seconds == 0f) break;
            s.StartNight();
            while (s.Phase == RunPhase.Night)
            {
                bot.PlayNightTick(); s.Tick();
                if (s.Night == night && seconds > 0f && s.NightTime >= seconds) goto dump;
                // seconds < 0: siege evidence — 2 s after the first wall breach on/after that night, enemies still on the field
                if (seconds < 0f && s.Night >= night && s.W.Analytics.Breaches.Count > 0)
                {
                    var last = s.W.Analytics.Breaches[s.W.Analytics.Breaches.Count - 1];
                    int alive = 0;
                    foreach (var u in s.W.Units) if (!u.Dead && u.Team == Team.Enemy) alive++;
                    if (last.Night == s.Night && s.W.Time - s.W.NightStartTime >= last.Time + 2f && alive >= 6) goto dump;
                }
            }
        }
        dump:
        var w = s.W;
        var tiles = new List<object>();
        for (int y = 0; y < w.Map.Height; y++) for (int x = 0; x < w.Map.Width; x++) tiles.Add(w.Map.Get(x, y).ToString());
        var bl = new List<object>();
        foreach (var b in w.Buildings) if (!b.Dead) bl.Add(new Dictionary<string, object> { ["key"] = b.Def.Family == BuildingFamily.Castle && !b.Ruined && s.KingdomEvolution != null ? ArtKeys.CastleVariant(ArtKeys.Building(b, 0), s.KingdomEvolution) : ArtKeys.Building(b, ArtKeys.WallMask(w, b)), ["x"] = b.Center.X, ["y"] = b.Center.Y, ["oy"] = (float)b.Origin.Y, ["hp"] = b.HpFraction, ["el"] = (int)b.Primary });
        var ul = new List<object>();
        foreach (var u in w.Units) if (!u.Dead) ul.Add(new Dictionary<string, object> { ["key"] = ArtKeys.Unit(u, u.AttackAnim > 0.05f), ["x"] = u.Position.X, ["y"] = u.Position.Y, ["flip"] = u.FacingX < 0, ["team"] = (int)u.Team, ["hp"] = u.HpFraction, ["fly"] = u.IsFlying, ["aura"] = (int)u.Status.DominantAura(), ["gen"] = u.Role == UnitRole.General });
        var pl = new List<object>();
        foreach (var p in w.Projectiles) pl.Add(new Dictionary<string, object> { ["x"] = p.Position.X, ["y"] = p.Position.Y, ["tx"] = p.TargetPos.X, ["ty"] = p.TargetPos.Y, ["v"] = p.Visual, ["el"] = (int)p.Element });
        var zl = new List<object>();
        foreach (var z in w.Zones) zl.Add(new Dictionary<string, object> { ["x"] = z.Position.X, ["y"] = z.Position.Y, ["r"] = z.Radius, ["el"] = (int)z.Element, ["k"] = z.Kind.ToString() });
        var tl = new List<object>();
        foreach (var t in w.Telegraphs) tl.Add(new Dictionary<string, object> { ["x"] = t.Position.X, ["y"] = t.Position.Y, ["r"] = t.Radius });
        var root = new Dictionary<string, object> { ["map"] = map, ["w"] = w.Map.Width, ["h"] = w.Map.Height, ["night"] = s.Night, ["isNight"] = w.IsNight, ["t"] = s.NightTime,
            ["tiles"] = tiles, ["buildings"] = bl, ["units"] = ul, ["projectiles"] = pl, ["zones"] = zl, ["telegraphs"] = tl,
            ["gold"] = s.Economy.Gold, ["mana"] = s.Economy.Mana, ["castle"] = w.Castle.HpFraction, ["general"] = general };
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, MiniJson.Write(root));
    }
}
