using System;
using System.Collections.Generic;
using System.Linq;

namespace Runavelle.Core
{
    public enum BreachOrder { Plug, FallBack, GeneralGuard, GeneralInner }

    /// <summary>One autonomous decision taken when a wall/gate fell (kept for the report, Chronicle and tests).</summary>
    public sealed class BreachDecision
    {
        public int Night;
        public float Time;
        public int BreachIndex;
        public int ActorId; // barracks id (squad) or AI general unit id
        public BreachOrder Order;
        public float Power, Threat;
        public Vec2 Post;
    }

    /// <summary>
    /// P3 Breach Crisis: when a wall breaks, nearby squads weigh their strength against the enemies at the gap and
    /// either plug it or fall back to an inner line by the Castle; AI generals guard the gap or retreat inside when weak.
    /// Decisions are deterministic (state only, no RNG), bounded in distance, and never teleport anyone — they only move
    /// the post that DefenderAI/GeneralSystem already walk to (flow field and A* handle the new topology).
    /// </summary>
    public sealed partial class GameSession
    {
        public readonly List<BreachDecision> BreachDecisions = new List<BreachDecision>();
        private int _breachesSeen;

        private void TickBreachResponse()
        {
            var br = W.Analytics.Breaches;
            if (_breachesSeen > br.Count) _breachesSeen = 0; // analytics reset (new run state)
            while (_breachesSeen < br.Count)
            {
                var rec = br[_breachesSeen];
                if (rec.Night == Night) RespondToBreach(new Vec2(rec.X, rec.Y), _breachesSeen);
                _breachesSeen++;
            }
        }

        private void RespondToBreach(Vec2 gap, int index)
        {
            if (W.Castle == null) return;
            var outward = (gap - W.Castle.Center).Normalized;
            if (outward.SqrMagnitude < 0.01f) outward = new Vec2(1, 0);
            float threat = 0f;
            foreach (var e in W.Units)
                if (!e.Dead && e.Team == Team.Enemy && Vec2.Distance(e.Position, gap) <= AiTuning.BreachThreatRadius) threat += e.Hp;
            var inner = InnerLine(gap);
            var plugAt = Nav.NearestFriendlyPassable(Int2.FromPosition(gap - outward * 0.6f)).Center;

            foreach (var b in W.Buildings)
            {
                if (b.Dead || b.Ruined || b.Family != BuildingFamily.Barracks) continue;
                // the home barracks or tonight's post: a squad that marched forward still answers a gap by its barracks
                float near = Math.Min(Vec2.Distance(b.Center, gap), b.RallyPost.HasValue ? Vec2.Distance(b.RallyPost.Value, gap) : float.MaxValue);
                if (near > AiTuning.BreachResponseRadius) continue;
                float power = 0f, hpFrac = 0f;
                int alive = 0;
                foreach (var id in b.Squad)
                    if (W.Get(id) is Unit s && !s.Dead) { power += s.Hp; hpFrac += s.HpFraction; alive++; }
                if (alive == 0) continue;
                hpFrac /= alive;
                if (AiTuning.BreachKeepEngaged && b.Squad.Any(id => W.Get(id) is Unit f && !f.Dead && f.State == UnitState.Fighting)) continue;
                bool plug = hpFrac >= AiTuning.BreachPlugMinHp && threat <= power * AiTuning.BreachPlugRatio;
                b.RallyPost = plug ? plugAt : inner;
                b.RallyDir = outward;
                foreach (var id in b.Squad)
                    if (W.Get(id) is Unit s && !s.Dead) { s.TargetId = -1; s.AiDecisionTimer = 0f; s.AiReason = plug ? "Bịt lỗ hổng" : "Lui về tuyến trong"; }
                Record(index, b.Id, plug ? BreachOrder.Plug : BreachOrder.FallBack, power, threat, b.RallyPost.Value);
            }

            foreach (var g in W.AIGenerals)
            {
                if (g.Dead || g.State == UnitState.Downed || !Generals.Posts.TryGetValue(g.AiSlot, out var post)) continue;
                if (Vec2.Distance(post, gap) > AiTuning.BreachGeneralSector) continue;
                bool weak = g.HpFraction < AiTuning.BreachGeneralMinHp || threat > g.Hp * AiTuning.BreachGeneralRatio;
                var p = weak ? inner : plugAt;
                Generals.Posts[g.AiSlot] = p;
                g.AiReason = weak ? "Giữ tuyến trong" : "Trấn giữ lỗ hổng";
                Record(index, g.Id, weak ? BreachOrder.GeneralInner : BreachOrder.GeneralGuard, g.Hp, threat, p);
            }
        }

        /// <summary>Inner defence line: just outside the Castle, on the side of the breach.</summary>
        public Vec2 InnerLine(Vec2 gap)
        {
            var c = W.Castle.Center;
            var dir = (gap - c).Normalized;
            if (dir.SqrMagnitude < 0.01f) dir = new Vec2(1, 0);
            // never past the gap itself: tight rings put the inner line halfway between the Castle edge and the gap
            float room = Math.Max(0.5f, Vec2.Distance(gap, c) - W.Castle.Size * 0.5f);
            var p = c + dir * (W.Castle.Size * 0.5f + Math.Min(AiTuning.InnerLineOffset, room * 0.5f));
            return Nav.NearestFriendlyPassable(Int2.FromPosition(p)).Center;
        }

        private void Record(int index, int actor, BreachOrder order, float power, float threat, Vec2 post)
        {
            BreachDecisions.Add(new BreachDecision { Night = Night, Time = NightTime, BreachIndex = index, ActorId = actor, Order = order, Power = power, Threat = threat, Post = post });
            W.Emit(SimEventType.BreachResponse, post, (float)order, ElementId.None, actor,
                order == BreachOrder.Plug ? "Đội quân bịt lỗ hổng" : order == BreachOrder.FallBack ? "Đội quân lui về tuyến trong" : order == BreachOrder.GeneralGuard ? "Tướng trấn giữ lỗ hổng" : "Tướng lui về tuyến trong");
        }
    }
}
