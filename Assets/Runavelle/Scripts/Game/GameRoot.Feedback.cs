using System.Collections.Generic;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>
    /// Simulation events → combat feedback. Every event is classified into an ImpactTier (Core.FeedbackRules); a
    /// per-tier token budget keeps large fights readable; shake is priority-aggregated and player-scaled; hit-stop is
    /// local to the struck sprite only (the fixed-step simulation never pauses). Purely presentational.
    /// </summary>
    public sealed partial class GameRoot
    {
        private readonly FeedbackBudget _budget = new FeedbackBudget();

        /// <summary>Short on-screen captions for audio-only cues (accessibility).</summary>
        public readonly List<(string Text, float Until)> Captions = new List<(string, float)>();

        public void Caption(string text)
        {
            if (!Settings.Captions) return;
            float now = Time.unscaledTime;
            Captions.RemoveAll(c => c.Until < now || c.Text == text);
            Captions.Add((text, now + 2.5f));
            if (Captions.Count > 3) Captions.RemoveAt(0);
        }

        private void Hit(Vector2 at, SimEvent e, ImpactTier tier, float now)
        {
            var w = Session.W;
            var vfx = View.Vfx;
            if (e.Value <= 0f) { if (e.Text == "block") vfx.Burst(at, new Color(0.8f, 0.9f, 1f), 4, 1.5f, 0.25f); return; }
            var target = w.Get(e.ActorId);
            bool enemy = target != null && target.Team == Team.Enemy;
            var uv = View.UnitViewOf(e.ActorId);
            if (uv != null)
            {
                uv.Flash = 0.1f;
                float hold = FeedbackRules.LocalHoldFor(tier);
                if (hold > 0f && !Settings.ReducedMotion) uv.Hold = Mathf.Max(uv.Hold, hold);
            }
            var bv = View.BuildingViewOf(e.ActorId);
            if (bv != null) bv.Flash = 0.12f;
            var kind = DamageTextKind.Normal;
            if (!enemy) kind = DamageTextKind.Incoming;
            else if (target.IsBoss) kind = DamageTextKind.Boss;
            else if (target.IsElite) kind = DamageTextKind.Elite;
            else if (e.Element != ElementId.None) kind = DamageTextKind.Elemental;
            if (enemy || e.Value >= 8f) vfx.Damage.Add(e.ActorId, e.Value, kind, e.Element, e.Position);
            if (e.Element != ElementId.None && Settings.VfxLevel >= 1 && _budget.Allow(ImpactTier.Light, now)) vfx.Element(at, e.Element, tier >= ImpactTier.Medium ? 0.9f : 0.55f);
            if (target == w.Castle) { Rig.Shake(FeedbackRules.ShakeFor(tier)); AudioService.Play("castle_hit", 0.6f, 2, 0.3f, category: SoundCategory.Combat); }
            else AudioService.Play(e.Element == ElementId.None ? "hit" : ElementGrammar.For(e.Element).Sound, 0.32f, 3, 0.05f, category: SoundCategory.Combat);
        }

        private void NearestAiBark(BarkTrigger t, Vec2 at, float maxDist)
        {
            Unit best = null;
            float bd = maxDist;
            foreach (var g in Session.W.AIGenerals)
            {
                if (g.Dead || g.State == UnitState.Downed) continue;
                float d = Vec2.Distance(g.Position, at);
                if (d < bd) { bd = d; best = g; }
            }
            if (best != null) Barks.Say(t, best, Session.NightTime);
        }

        private void ProcessEvents()
        {
            var w = Session.W;
            var vfx = View.Vfx;
            float now = Time.unscaledTime;
            for (int i = 0; i < w.Events.Count; i++)
            {
                var e = w.Events[i];
                var at = WorldView.V(e.Position);
                var tier = FeedbackRules.Classify(e, w);
                Highlights?.Observe(e, w, Session.NightTime);
                switch (e.Type)
                {
                    case SimEventType.Hit:
                        Hit(at, e, tier, now);
                        break;
                    case SimEventType.Kill:
                    {
                        if (tier == ImpactTier.Epic)
                        {
                            // boss defeat: decisive, readable, never a global freeze
                            vfx.Element(at, ElementId.Light, 2.5f);
                            vfx.Ring(at, Gui.Gold, 3f, 0.8f, "fx/ring_thick");
                            vfx.Smoke(at, 14, new Color(0.9f, 0.85f, 0.8f, 0.8f));
                            Rig.Shake(FeedbackRules.ShakeFor(tier));
                            Rig.Punch(1f);
                            ShowBanner("Đã hạ " + (Db.Units.TryGetValue(e.Text ?? "", out var bd) ? bd.Name : "boss") + "!");
                            AudioService.Play("boss_down", 1f, 1, 1f, category: SoundCategory.Stinger);
                            Music.Relief();
                            NearestAiBark(BarkTrigger.BossDefeat, e.Position, 99f);
                            break;
                        }
                        // AI General "timely save": it killed something that was hitting a badly hurt ally
                        if (w.Get(e.OtherId) is Unit killer && killer.Role == UnitRole.General && !killer.IsPlayerControlled && w.Get(e.ActorId) is Unit victim
                            && w.Get(victim.TargetId) is Actor endangered && endangered.Team == Team.Player && endangered.HpFraction < 0.35f)
                            Barks.Say(BarkTrigger.TimelySave, killer, Session.NightTime);
                        if (!_budget.Allow(tier, now)) break;
                        if (tier == ImpactTier.Heavy)
                        {
                            vfx.Ring(at, new Color(1f, 0.8f, 0.4f), 1.2f, 0.45f, "fx/ring_thick");
                            vfx.Burst(at, new Color(1f, 0.9f, 0.6f), 12, 3f, 0.35f);
                            vfx.Label(at, "Hạ tinh anh!", new Color(1f, 0.85f, 0.4f), 28f, 1.1f);
                            Rig.Shake(FeedbackRules.ShakeFor(tier) * 0.6f);
                            AudioService.Play("victory_small", 0.6f, 1, 0.5f, category: SoundCategory.Stinger);
                        }
                        vfx.Smoke(at, 4);
                        vfx.Burst(at, new Color(1f, 0.9f, 0.6f), 5, 2f, 0.25f);
                        AudioService.Play("death", 0.5f, 3, 0.05f, category: SoundCategory.Combat);
                        break;
                    }
                    case SimEventType.Reaction:
                    {
                        Vector2? other = w.Get(e.OtherId) is Actor oa ? WorldView.V(oa.Position) : (Vector2?)null;
                        if (e.Text == "arc" || e.Text == "chain")
                        {
                            if (other.HasValue && _budget.Allow(ImpactTier.Light, now)) vfx.Arc(other.Value, at, ArtLibrary.ElementColor(e.Element == ElementId.None ? ElementId.Lightning : e.Element));
                            AudioService.Play("zap", 0.35f, 3, 0.05f, category: SoundCategory.Combat);
                            break;
                        }
                        if (_budget.Allow(tier, now))
                        {
                            vfx.Reaction(at, e.Reaction, e.Element, other);
                            if (ElementGrammar.For(e.Reaction).Weight >= 2) Rig.Shake(FeedbackRules.ShakeFor(ImpactTier.Medium));
                            vfx.Label(at, e.Text, ArtLibrary.ElementColor(e.Element), 30f, 1.1f);
                            vfx.Damage.Add(e.ActorId, 0f, DamageTextKind.Reaction, e.Element, e.Position);
                        }
                        else vfx.Element(at, e.Element, 0.6f);
                        AudioService.Play("reaction", 0.7f, 3, 0.08f, category: SoundCategory.Combat);
                        break;
                    }
                    case SimEventType.WallBreached:
                        vfx.Smoke(at, 12, new Color(0.7f, 0.65f, 0.6f, 0.8f));
                        vfx.Element(at, ElementId.Earth, 1.6f);
                        vfx.Decal("fx/rubble_" + (e.ActorId % 3), at, 1.1f, 0f, Color.white);
                        Rig.Shake(FeedbackRules.ShakeFor(tier));
                        vfx.Label(at, "Tường bị phá!", new Color(1f, 0.5f, 0.3f), 30f, 1.6f);
                        AudioService.Play("breach", 1f, 2, 0.2f, category: SoundCategory.Stinger);
                        Caption("[Tường đổ sập]");
                        Music.Breach();
                        NearestAiBark(BarkTrigger.WallBreach, e.Position, 14f);
                        break;
                    case SimEventType.BreachResponse:
                    {
                        // autonomous squad/general decision after a breach: say it where they are heading
                        var order = (BreachOrder)(int)e.Value;
                        bool hold = order == BreachOrder.Plug || order == BreachOrder.GeneralGuard;
                        var col = hold ? new Color(0.45f, 0.75f, 1f) : new Color(1f, 0.8f, 0.35f);
                        vfx.Ring(at, col, 1.2f, 0.5f, "fx/ring_thick");
                        vfx.Label(at, hold ? "Bịt lỗ hổng!" : "Lui về tuyến trong!", col, 24f, 1.4f);
                        break;
                    }
                    case SimEventType.BuildingDestroyed:
                        vfx.Smoke(at, 12, new Color(0.6f, 0.55f, 0.55f, 0.85f));
                        vfx.Decal("fx/rubble_" + (e.ActorId % 3), at, 1.4f, 0f, Color.white);
                        Rig.Shake(FeedbackRules.ShakeFor(tier));
                        AudioService.Play("breach", 0.9f, 2, 0.2f, category: SoundCategory.Stinger);
                        break;
                    case SimEventType.UltimateCast:
                    {
                        var col = ArtLibrary.ElementColor(e.Element);
                        vfx.Ring(at, Color.white, e.Value * 0.4f, 0.25f, "fx/ring_thick"); // anticipation flash
                        vfx.Ring(at, col, e.Value, 0.6f, "fx/ring_thick");
                        vfx.Element(at, e.Element, 2.2f);
                        vfx.Label(at, e.Text, Gui.Gold, 34f, 1.4f);
                        Rig.Shake(FeedbackRules.ShakeFor(tier));
                        if (w.Get(e.ActorId) is Unit caster && caster.IsPlayerControlled)
                        {
                            Rig.Punch(0.6f);
                            foreach (var u in w.Units)
                                if (!u.Dead && u.Team == Team.Player && u.Role == UnitRole.Soldier && Vector2.Distance(WorldView.V(u.Position), at) <= e.Value + 2f)
                                    View.UnitViewOf(u.Id)?.SetCheer(0.6f);
                        }
                        AudioService.Play("ultimate", 1f, 2, 0.2f, category: SoundCategory.Stinger);
                        if (w.Get(e.ActorId) is Unit ai && !ai.IsPlayerControlled) Barks.Say(BarkTrigger.Ultimate, ai, Session.NightTime);
                        break;
                    }
                    case SimEventType.Dash:
                        vfx.Smoke(at, 3, new Color(0.9f, 0.95f, 1f, 0.6f));
                        AudioService.Play("dash", 0.6f, 2, 0.1f, category: SoundCategory.Combat);
                        break;
                    case SimEventType.MeleeSwing:
                    {
                        var a = w.Get(e.ActorId) as Unit;
                        if (a != null && Settings.VfxLevel >= 1 && (a.Role == UnitRole.General || _budget.Allow(ImpactTier.Light, now)))
                            vfx.Slash(at, a.FacingX, e.Element == ElementId.None ? new Color(1, 1, 1, 0.8f) : ArtLibrary.ElementColor(e.Element, 0.9f), a.Role == UnitRole.General ? 1.3f : 1f);
                        AudioService.Play("swing", a != null && a.Role == UnitRole.General ? 0.45f : 0.22f, 4, 0.04f, category: SoundCategory.Combat);
                        break;
                    }
                    case SimEventType.ProjectileFired:
                        AudioService.Play(e.Element == ElementId.None ? "arrow" : "magic", 0.25f, 3, 0.07f, category: SoundCategory.Combat);
                        break;
                    case SimEventType.Freeze:
                        vfx.Element(at, ElementId.Ice, 1.2f);
                        break;
                    case SimEventType.Heal:
                        if (e.Value >= 6f) vfx.Damage.Add(e.ActorId, e.Value, DamageTextKind.Heal, ElementId.Light, e.Position);
                        break;
                    case SimEventType.Summon:
                        vfx.Ring(at, new Color(0.7f, 0.4f, 1f), 1.4f, 0.6f);
                        vfx.Element(at, ElementId.Shadow, 1.2f);
                        AudioService.Play("summon", 0.7f, 2, 0.2f, category: SoundCategory.Combat);
                        break;
                    case SimEventType.BossPhase:
                        ShowBanner(e.Text);
                        Rig.Shake(FeedbackRules.ShakeFor(tier) * 0.7f);
                        AudioService.Play("horn", 0.8f, 1, 1f, category: SoundCategory.Stinger);
                        Caption("[Tù và: boss đổi giai đoạn]");
                        break;
                    case SimEventType.Announcement:
                        ShowBanner(e.Text);
                        AudioService.Play("warning", 0.7f, 1, 0.8f, category: SoundCategory.Ui);
                        break;
                    case SimEventType.GeneralDowned:
                        ShowBanner(e.Text + " gục ngã! Hồi sinh sau 12 giây.");
                        if (w.Get(e.ActorId) is Unit dg && !dg.IsPlayerControlled) Barks.Say(BarkTrigger.Injured, dg, Session.NightTime);
                        break;
                    case SimEventType.Building:
                        View.BuildingViewOf(e.ActorId)?.StartRise(Settings.ReducedMotion);
                        vfx.Smoke(at, 5);
                        AudioService.Play("build", 0.7f, 2, 0.1f, category: SoundCategory.Ui);
                        break;
                    case SimEventType.Upgrade:
                        View.BuildingViewOf(e.ActorId)?.StartUpgradePulse(Settings.ReducedMotion);
                        vfx.Burst(at, Gui.Gold, 10, 2.2f, 0.3f);
                        vfx.Element(at, ElementId.Light, 0.8f);
                        AudioService.Play("upgrade", 0.8f, 2, 0.1f, category: SoundCategory.Ui);
                        break;
                    case SimEventType.Telegraph:
                        AudioService.Play("warning", 0.6f, 2, 0.3f, category: SoundCategory.Ui);
                        if (!string.IsNullOrEmpty(e.Text) && e.ActorId >= 0) vfx.Label(at, e.Text, new Color(1f, 0.4f, 0.3f), 26f, 1f);
                        break;
                    case SimEventType.Hazard:
                        if (e.Text == "lightning")
                        {
                            vfx.Element(at, ElementId.Lightning, 1.6f);
                            vfx.Ring(at, ArtLibrary.ElementColor(ElementId.Lightning), e.Value, 0.4f);
                            vfx.Decal("fx/scorch", at, 0.8f, 6f, new Color(1, 1, 1, 0.6f));
                            Rig.Shake(FeedbackRules.ShakeFor(tier));
                            AudioService.Play("zap", 0.9f, 2, 0.1f, category: SoundCategory.Combat);
                            Caption("[Sét đánh]");
                        }
                        else if (e.Text == "vent")
                        {
                            vfx.Element(at, ElementId.Fire, 1.6f);
                            vfx.Smoke(at, 8, new Color(0.35f, 0.3f, 0.3f, 0.8f));
                            vfx.Decal("fx/scorch", at, 1.1f, 10f, new Color(1, 1, 1, 0.7f));
                            AudioService.Play("breach", 0.6f, 2, 0.15f, category: SoundCategory.Combat);
                        }
                        else if (e.Text == "ice_crack")
                        {
                            vfx.Element(at, ElementId.Ice, 1.2f);
                            vfx.Label(at, "Băng vỡ!", new Color(0.7f, 0.9f, 1f), 24f, 1f);
                            AudioService.Play("fx_ice", 0.7f, 2, 0.15f, category: SoundCategory.Combat);
                        }
                        break;
                    case SimEventType.Explosion:
                        vfx.Ring(at, ArtLibrary.ElementColor(e.Element == ElementId.None ? ElementId.Fire : e.Element), Mathf.Max(1f, e.Value), 0.45f, "fx/ring_thick");
                        vfx.Element(at, ElementId.Fire, 1.4f);
                        vfx.Smoke(at, 6);
                        vfx.Decal("fx/scorch", at, 0.9f, 10f, new Color(1, 1, 1, 0.7f));
                        Rig.Shake(FeedbackRules.ShakeFor(tier));
                        AudioService.Play("breach", 0.7f, 3, 0.1f, category: SoundCategory.Combat);
                        break;
                    case SimEventType.Stealth:
                        vfx.Element(at, ElementId.Shadow, 0.8f);
                        break;
                    case SimEventType.StatusApplied:
                        if (e.Text == "exhausted") vfx.Label(at, "Kiệt sức! (+50% sát thương)", new Color(1f, 0.9f, 0.4f), 26f, 1.4f);
                        else if (e.Text == "bastion")
                        {
                            vfx.Ring(at, Gui.Gold, 3.5f, 0.9f, "fx/ring_thick");
                            vfx.Element(at, ElementId.Light, 2.5f);
                            AudioService.Play("bastion", 1f, 1, 1f, category: SoundCategory.Stinger);
                            Caption("[Thành Lũy Cuối Cùng]");
                            Rig.Punch(0.5f);
                        }
                        else if (e.Text == "possessed") vfx.Label(at, "Tháp bị khống chế!", new Color(0.8f, 0.5f, 1f), 26f, 1.4f);
                        break;
                }
                Milestones?.Observe(e, Session);
            }
            w.Events.Clear();
            if (ToastTime > 0) ToastTime -= Time.unscaledDeltaTime;
            if (BannerTime > 0) BannerTime -= Time.unscaledDeltaTime;
        }
    }
}
