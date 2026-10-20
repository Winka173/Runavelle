using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Shared continuous movement: speed modifiers, soft separation, axis-sliding against blocked tiles.</summary>
    public sealed class Movement
    {
        private readonly World _w;
        private readonly CombatSystem _combat;
        private readonly List<Unit> _tmp = new List<Unit>(16);
        public float EnrageMult = 1f;

        public Movement(World w, CombatSystem c) { _w = w; _combat = c; }

        public float SpeedOf(Unit u)
        {
            float chill = _w.Mods.Has(Hook.Permafrost) && u.Team == Team.Enemy ? 0.65f : 0.5f;
            float s = u.Speed * u.Status.MoveMult(chill) * u.Buffs.MoveMult;
            if (u.Team == Team.Enemy) s *= EnrageMult;
            if (!u.IsFlying)
            {
                var t = Int2.FromPosition(u.Position);
                if (_w.Map.InBounds(t.X, t.Y) && _w.Map.Get(t.X, t.Y) == TileType.Ice) s *= 1.15f;
            }
            return s;
        }

        /// <summary>Moves toward target; returns true when within arriveDist.</summary>
        public bool MoveTo(Unit u, Vec2 target, float dt, float arriveDist = 0.08f)
        {
            Vec2 d = target - u.Position;
            float dist = d.Magnitude;
            if (dist <= arriveDist) return true;
            float step = SpeedOf(u) * dt;
            if (step <= 0f) return false;
            Vec2 dir = d / dist;
            Vec2 sep = Separation(u);
            Vec2 vel = (dir + sep * 0.6f).Normalized * Math.Min(step, dist);
            Step(u, vel);
            if (Math.Abs(dir.X) > 0.2f) u.FacingX = Math.Sign(dir.X);
            return Vec2.Distance(u.Position, target) <= arriveDist;
        }

        public void Step(Unit u, Vec2 vel)
        {
            Vec2 p = u.Position;
            // a unit standing inside a blocked tile (spawned in / built over) may always walk out
            if (!u.IsFlying && !_combat.PositionWalkable(u, p))
            {
                p += vel;
                p.X = MathUtil.Clamp(p.X, 0.05f, _w.Map.Width - 0.05f);
                p.Y = MathUtil.Clamp(p.Y, 0.05f, _w.Map.Height - 0.05f);
                u.Position = p;
                return;
            }
            Vec2 nx = new Vec2(p.X + vel.X, p.Y);
            if (_combat.PositionWalkable(u, nx) && ClearOfCorners(u, nx)) p = nx;
            Vec2 ny = new Vec2(p.X, p.Y + vel.Y);
            if (_combat.PositionWalkable(u, ny) && ClearOfCorners(u, ny)) p = ny;
            p.X = MathUtil.Clamp(p.X, 0.05f, _w.Map.Width - 0.05f);
            p.Y = MathUtil.Clamp(p.Y, 0.05f, _w.Map.Height - 0.05f);
            u.Position = p;
        }

        private bool ClearOfCorners(Unit u, Vec2 p)
        {
            if (u.IsFlying) return true;
            float r = Math.Min(0.22f, u.Radius);
            return _combat.PositionWalkable(u, new Vec2(p.X + r, p.Y)) && _combat.PositionWalkable(u, new Vec2(p.X - r, p.Y)) &&
                   _combat.PositionWalkable(u, new Vec2(p.X, p.Y + r)) && _combat.PositionWalkable(u, new Vec2(p.X, p.Y - r));
        }

        private Vec2 Separation(Unit u)
        {
            Vec2 push = Vec2.Zero;
            _tmp.Clear();
            (u.Team == Team.Player ? _w.Friendly : _w.Hostile).Query(u.Position, u.Radius + 0.5f, _tmp);
            int n = 0;
            for (int i = 0; i < _tmp.Count && n < 6; i++)
            {
                var o = _tmp[i];
                if (o == u || o.Dead || o.IsFlying != u.IsFlying) continue;
                Vec2 d = u.Position - o.Position;
                float m = d.Magnitude;
                float min = u.Radius + o.Radius;
                if (m >= min) continue;
                if (m < 1e-4f) d = new Vec2((u.Id % 7) - 3, (u.Id % 5) - 2);
                push += d.Normalized * ((min - m) / min);
                n++;
            }
            return push;
        }

        /// <summary>True if a straight line between a and b crosses no tile blocked for this unit.</summary>
        public bool LineWalkable(Unit u, Vec2 a, Vec2 b)
        {
            if (u.IsFlying) return true;
            float len = Vec2.Distance(a, b);
            int steps = Math.Max(1, (int)(len / 0.4f));
            for (int i = 1; i <= steps; i++)
                if (!_combat.PositionWalkable(u, Vec2.Lerp(a, b, i / (float)steps))) return false;
            return true;
        }
    }
}
