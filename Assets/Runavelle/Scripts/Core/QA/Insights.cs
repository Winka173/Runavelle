using System;
using System.Collections.Generic;
using System.Linq;

namespace Runavelle.Core
{
    /// <summary>P6 readable costs: what a level gives for its price (numbers straight from content data).</summary>
    public static class BuildInsight
    {
        /// <summary>Mana is scarcer than gold; 1 mana is valued as 1.3 gold for comparisons (same weight the AutoPlayer uses).</summary>
        public const float ManaWeight = 1.3f;

        public static float GoldValue(Cost c) => c.Gold + c.Mana * ManaWeight;

        /// <summary>Nights until an income building repays its price (null when it produces nothing).</summary>
        public static int? PaybackNights(BuildingLevel l, Cost paid)
        {
            float inc = GoldValue(l.IncomePerDawn);
            if (inc <= 0f) return null;
            return (int)Math.Ceiling(GoldValue(paid) / inc);
        }

        public static float Dps(BuildingLevel l) => l.Attack == null || l.Attack.Interval <= 0f ? 0f : l.Attack.Damage / l.Attack.Interval;

        /// <summary>Short lines for the build tooltip / selection panel.</summary>
        public static List<string> Lines(BuildingLevel l, Cost price)
        {
            var o = new List<string>();
            var pay = PaybackNights(l, price);
            if (pay.HasValue)
                o.Add($"Thu nhập mỗi bình minh: {l.IncomePerDawn.Gold} vàng, {l.IncomePerDawn.Mana} mana — hoàn vốn sau ~{pay} đêm");
            float dps = Dps(l);
            if (dps > 0f)
            {
                float per = GoldValue(price) > 0f ? dps / GoldValue(price) * 100f : 0f;
                o.Add($"Sát thương/giây {dps:0.#} · tầm {l.Attack.Range:0.#}" + (per > 0f ? $" · {per:0.#} DPS mỗi 100 vàng" : "") + (l.Attack.HitsAir ? " · bắn kẻ bay" : ""));
            }
            if (l.SquadSize > 0) o.Add($"Đội {l.SquadSize} lính tự chiến đấu");
            if (l.IsGate || (dps <= 0f && !pay.HasValue && l.MaxHp > 0f && l.SquadSize == 0))
                o.Add($"Máu {l.MaxHp:0} · giáp {l.Armor:0}" + (GoldValue(price) > 0f ? $" · {l.MaxHp / GoldValue(price):0.#} máu mỗi vàng" : ""));
            return o;
        }
    }

    /// <summary>
    /// P6 post-night insights from measured data only (castle damage by attacker this night, breaches, losses, mana drain).
    /// Every line cites the number it is based on; nothing is guessed.
    /// </summary>
    public static class NightInsights
    {
        public static List<string> For(GameSession s, NightReport rep, Dictionary<string, float> castleDamageThisNight)
        {
            var tips = new List<string>();
            var an = s.W.Analytics;
            var breaches = an.Breaches.Where(b => b.Night == rep.Night).OrderBy(b => b.Time).ToList();
            float castleMax = s.W.Castle?.MaxHp ?? 1f;
            if (rep.CastleDamage >= castleMax * 0.05f && castleDamageThisNight.Count > 0)
            {
                var top = castleDamageThisNight.OrderByDescending(k => k.Value).First();
                var def = s.Content.Units.Values.FirstOrDefault(u => top.Key.StartsWith(u.Name));
                string why = def == null ? "" : def.Flying ? " — kẻ bay: cần tháp/lính bắn được trên không gần Lâu Đài"
                    : def.Behavior == EnemyBehavior.Siege || def.Behavior == EnemyBehavior.GateBreaker ? " — công thành: chặn sớm bằng lính Giáo/tháp bắn tỉa"
                    : def.Behavior == EnemyBehavior.Boss ? " — trùm: dành tối thượng cho lúc nó tới gần" : "";
                tips.Add($"Lâu Đài mất {rep.CastleDamage:0} máu; nhiều nhất từ {top.Key} ({top.Value:0}){why}.");
            }
            if (breaches.Count > 0)
            {
                var first = breaches[0];
                var lvl = s.Content.Levels.TryGetValue(first.LevelId, out var l) ? l.Name : first.LevelId;
                tips.Add($"Tường thủng {breaches.Count} lần; lần đầu ở giây {first.Time:0}: {lvl} tại ({first.X:0},{first.Y:0}) bởi {first.Cause} — nâng cấp đoạn đó hoặc đặt tháp phía sau.");
            }
            int soldiersAtEnd = s.W.Units.Count(u => !u.Dead && u.Role == UnitRole.Soldier);
            if (rep.SoldiersLost > 0 && rep.SoldiersLost >= Math.Max(3, (soldiersAtEnd + rep.SoldiersLost) / 2))
                tips.Add($"Mất {rep.SoldiersLost} lính (còn {soldiersAtEnd}). Đội nhỏ đứng một mình dễ vỡ — đặt doanh trại gần tháp hoặc gần Tướng.");
            if (rep.ManaDrained > 0) tips.Add($"Bị hút {rep.ManaDrained} mana. Đặt tháp gần Giếng Mana hoặc chặn kẻ hút trước khi tới nơi.");
            if (rep.BuildingsLost > 0 && breaches.Count == 0) tips.Add($"{rep.BuildingsLost} công trình bị phá hủy. Bình minh có thể sửa (chi phí rẻ hơn xây mới).");
            if (tips.Count == 0)
                tips.Add($"Phòng thủ vững: {rep.Kills} hạ gục trong {rep.Duration:0} giây, Lâu Đài mất {rep.CastleDamage:0} máu.");
            return tips.Take(3).ToList();
        }
    }
}
