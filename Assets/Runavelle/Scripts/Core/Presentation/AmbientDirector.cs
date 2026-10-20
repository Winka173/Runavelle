using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum AmbientKind { ChimneySmoke, WellShimmer, Torch }

    public struct AmbientEmitter
    {
        public AmbientKind Kind;
        public Vec2 Position;
        /// <summary>Particles per second (smoke/shimmer) or flicker intensity 0..1 (torch).</summary>
        public float Rate;
        public int BuildingId;
    }

    /// <summary>
    /// P1 living kingdom, purely decorative: which buildings emit ambient life and how much. No simulation state is read
    /// beyond building presence/tier/ruin, nothing feeds back into gameplay, and the total is capped (mobile budget).
    /// Level 0 = off, 1 = reduced (half budget, no smoke at night), 2 = full.
    /// </summary>
    public static class AmbientDirector
    {
        public const int MaxEmitters = 24;

        public static List<AmbientEmitter> Emitters(World w, int level, List<AmbientEmitter> into = null)
        {
            var list = into ?? new List<AmbientEmitter>();
            list.Clear();
            if (level <= 0) return list;
            int cap = level >= 2 ? MaxEmitters : MaxEmitters / 2;
            foreach (var b in w.Buildings)
            {
                if (list.Count >= cap) break;
                if (b.Dead || b.Ruined) continue;
                var top = new Vec2(b.Center.X, b.Center.Y + b.Size * 0.5f);
                switch (b.Def.Family)
                {
                    case BuildingFamily.House when b.Level.Tier >= 2 && !(w.IsNight && level < 2):
                        list.Add(new AmbientEmitter { Kind = AmbientKind.ChimneySmoke, Position = top + new Vec2(0.4f, 0.9f), Rate = w.IsNight ? 0.25f : 0.5f, BuildingId = b.Id });
                        break;
                    case BuildingFamily.ManaWell:
                        list.Add(new AmbientEmitter { Kind = AmbientKind.WellShimmer, Position = b.Center, Rate = 0.6f + 0.2f * b.Level.Tier, BuildingId = b.Id });
                        break;
                    case BuildingFamily.Barracks when w.IsNight:
                    case BuildingFamily.Castle when w.IsNight:
                        list.Add(new AmbientEmitter { Kind = AmbientKind.Torch, Position = new Vec2(b.Center.X - b.Size * 0.5f, b.Origin.Y), Rate = 1f, BuildingId = b.Id });
                        if (list.Count < cap) list.Add(new AmbientEmitter { Kind = AmbientKind.Torch, Position = new Vec2(b.Center.X + b.Size * 0.5f, b.Origin.Y), Rate = 1f, BuildingId = b.Id });
                        break;
                }
            }
            return list;
        }
    }
}
