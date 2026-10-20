using System;
using System.Collections.Generic;
using System.Linq;

namespace Runavelle.Core
{
    /// <summary>P5 Kingdom Chronicle recorder: reads simulation state/events only (no RNG, no side effects on the run).</summary>
    public sealed partial class GameSession
    {
        public RunChronicle Chronicle { get; private set; }
        private int _chrDecrees, _chrBreaches, _chrPlug, _chrFall;
        private string _chrCastle, _chrKingdom, _chrGenEvo;
        private bool _chrElite, _chrEmergency, _chrFinal;
        private readonly HashSet<string> _chrBosses = new HashSet<string>();

        private void ChronicleBegin()
        {
            Chronicle = new RunChronicle
            {
                Map = Setup.MapId, General = Setup.GeneralId, Seed = Setup.Seed, Ascension = Setup.Ascension, Challenge = Setup.ChallengeId, Endless = Setup.Endless,
                ContentVersion = Content.Levels.Count + Content.Units.Count * 1000, StartedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            };
            _chrCastle = W.Castle?.Level.Id;
            W.Observer = ChronicleObserve;
            Note(ChronicleKind.RunStart, $"Khởi đầu tại {Content.Maps[Setup.MapId].Name} cùng {Content.Generals[Content.Migrate(Setup.GeneralId)].Name}: {Economy.Gold} vàng, {Economy.Mana} mana, {W.Castle?.Level.Name}." +
                (Setup.Ascension > 0 ? $" Thăng Cấp {Setup.Ascension}." : ""));
        }

        private void Note(ChronicleKind k, string text) =>
            Chronicle?.Add(new ChronicleEntry { Night = Night, AtNight = Phase == RunPhase.Night, Time = Phase == RunPhase.Night ? NightTime : 0f, Kind = k, Text = text });

        /// <summary>Called after ticks and phase changes; idempotent.</summary>
        private void ChronicleTrack()
        {
            if (Chronicle == null) return;
            var castle = W.Castle;
            if (castle != null && castle.Level.Id != _chrCastle)
            {
                _chrCastle = castle.Level.Id;
                Note(ChronicleKind.CastleTier, $"Lâu Đài vươn lên {castle.Level.Name} (cấp {castle.Level.Tier}).");
            }
            if (KingdomEvolution != null && KingdomEvolution != _chrKingdom)
            {
                _chrKingdom = KingdomEvolution;
                var k = Content.Kingdoms.Find(x => x.Id == KingdomEvolution);
                Note(ChronicleKind.KingdomEvolution, $"Vương quốc tiến hoá thành {k?.Name ?? KingdomEvolution}.");
            }
            if (GeneralEvolutionId != null && GeneralEvolutionId != _chrGenEvo)
            {
                _chrGenEvo = GeneralEvolutionId;
                Note(ChronicleKind.GeneralEvolution, $"{W.PlayerGeneral.Def.Name} tiến hoá: {W.PlayerGeneral.General.Evolutions.FirstOrDefault(e => e.Id == GeneralEvolutionId)?.Name ?? GeneralEvolutionId}.");
            }
            for (; _chrDecrees < TakenDecrees.Count; _chrDecrees++)
            {
                var d = Content.Decrees[TakenDecrees[_chrDecrees]];
                if (d.Rarity != Rarity.Common || d.Category == DecreeCategory.Cursed) // "special" decrees only
                    Note(ChronicleKind.Decree, $"Ban chiếu chỉ {(d.Category == DecreeCategory.Cursed ? "nguyền rủa" : d.Rarity == Rarity.Epic ? "sử thi" : "hiếm")}: {d.Name}.");
            }
            if (!_chrFinal && (Phase == RunPhase.Victory || Phase == RunPhase.Defeat)) ChronicleFinish();
        }

        private void ChronicleObserve(SimEvent e)
        {
            if (Chronicle == null) return;
            switch (e.Type)
            {
                case SimEventType.Spawned:
                    if (W.Get(e.ActorId) is Unit b && b.Def.Behavior == EnemyBehavior.Boss && _chrBosses.Add(b.Def.Id)) Note(ChronicleKind.BossArrives, $"Trùm {b.Def.Name} xuất hiện ở đêm {Night}.");
                    break;
                case SimEventType.Kill:
                    // the victim may already be cleaned up: read its definition from the event, not the world
                    if (e.Text != null && Content.Units.TryGetValue(e.Text, out var kd) && kd.Role == UnitRole.Enemy)
                    {
                        if (kd.Behavior == EnemyBehavior.Boss) Note(ChronicleKind.BossDefeat, $"Hạ gục trùm {kd.Name} sau {NightTime:0} giây.");
                        else if (kd.Behavior == EnemyBehavior.Elite && !_chrElite) { _chrElite = true; Note(ChronicleKind.FirstElite, $"Hạ tinh anh đầu tiên: {kd.Name}."); }
                    }
                    break;
                case SimEventType.WallBreached:
                    if (_chrBreaches++ == 0) Note(ChronicleKind.WallBreach, $"Tường bị phá lần đầu trong đêm: {e.Text}.");
                    break;
                case SimEventType.BreachResponse:
                    if ((BreachOrder)(int)e.Value == BreachOrder.Plug || (BreachOrder)(int)e.Value == BreachOrder.GeneralGuard) _chrPlug++; else _chrFall++;
                    break;
                case SimEventType.UltimateCast:
                    if (!_chrEmergency && W.Castle != null && W.Castle.HpFraction < 0.35f && W.Get(e.ActorId) is Unit g && g.IsPlayerControlled)
                    { _chrEmergency = true; Note(ChronicleKind.EmergencyUltimate, $"Tối thượng khẩn cấp \"{e.Text}\" khi Lâu Đài còn {W.Castle.HpFraction * 100:0}% máu."); }
                    break;
            }
        }

        private void ChronicleNightEnd(NightReport rep)
        {
            ChronicleTrack();
            if (_chrBreaches > 1 || _chrPlug + _chrFall > 0)
                Note(ChronicleKind.BreachOrders, $"Tường thủng {_chrBreaches} lần; quân tự quyết: {_chrPlug} lần bịt lỗ, {_chrFall} lần lui về tuyến trong.");
            Note(rep.Victory ? ChronicleKind.NightVictory : ChronicleKind.NightDefeat,
                rep.Victory ? $"Giữ vững đêm {Night}: {rep.Kills} hạ gục, Lâu Đài mất {rep.CastleDamage:0} máu, {rep.SoldiersLost} lính tử trận."
                            : $"Thất thủ ở đêm {Night} sau {rep.Duration:0} giây.");
            _chrBreaches = _chrPlug = _chrFall = 0;
            _chrEmergency = false;
        }

        private void ChronicleFinish()
        {
            _chrFinal = true;
            bool win = Phase == RunPhase.Victory;
            Chronicle.Result = win ? "WIN" : "LOSS";
            Chronicle.Nights = win ? Math.Max(TotalNights, Night) : Night;
            Chronicle.Decrees.Clear();
            Chronicle.Decrees.AddRange(TakenDecrees);
            Chronicle.FinalBuild.Clear();
            foreach (var b in W.Buildings)
                if (!b.Dead && b.Lifetime < 0f) Chronicle.FinalBuild[b.Level.Id] = (Chronicle.FinalBuild.TryGetValue(b.Level.Id, out int c) ? c : 0) + 1;
            int walls = W.Buildings.Count(b => !b.Dead && b.IsWallLike), towers = W.Buildings.Count(b => !b.Dead && (b.Family == BuildingFamily.ArcherTower || b.Family == BuildingFamily.MagicTower));
            Note(ChronicleKind.RunEnd, (win ? "Chiến thắng" : "Kết thúc") + $" sau {Chronicle.Nights} đêm: {W.Castle?.Level.Name}, {towers} tháp, {walls} đoạn tường/cổng, {Economy.Gold} vàng, {Economy.Mana} mana, {TakenDecrees.Count} chiếu chỉ.");
        }

        internal void RestoreChronicle(Dictionary<string, object> root)
        {
            if (root.TryGetValue("chronicle", out var o) && o is Dictionary<string, object> d)
            {
                Chronicle = RunChronicle.FromJson(d);
                _chrCastle = W.Castle?.Level.Id;
                _chrKingdom = KingdomEvolution;
                _chrGenEvo = GeneralEvolutionId;
                _chrDecrees = TakenDecrees.Count;
                _chrElite = Chronicle.Entries.Any(e => e.Kind == ChronicleKind.FirstElite);
                foreach (var e in Chronicle.Entries) if (e.Kind == ChronicleKind.BossArrives) _chrBosses.Add(e.Text);
            }
            else
            {
                // pre-Chronicle save (v1): start a fresh chronicle from the restored state, without inventing past events
                ChronicleBegin();
                Chronicle.Entries[0].Text = $"Biên niên bắt đầu khi tải lại đêm {Night} (bản lưu cũ chưa có biên niên).";
                _chrDecrees = TakenDecrees.Count;
                _chrKingdom = KingdomEvolution;
                _chrGenEvo = GeneralEvolutionId;
            }
        }
    }
}
