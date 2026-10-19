using System;
using System.Collections.Generic;
using System.Linq;

namespace Runavelle.Core
{
    /// <summary>Defeat suggestions derived only from what was actually logged in this run (no fabricated advice).</summary>
    public static class DefeatAdvice
    {
        public static List<string> Suggest(GameSession s)
        {
            var tips = new List<string>();
            var an = s.W.Analytics;
            float total = an.CastleDamageBy.Values.Sum();
            if (total <= 0f) return tips;
            float Share(Func<UnitDefinition, bool> pred)
            {
                float v = 0f;
                foreach (var kv in an.CastleDamageBy)
                {
                    var u = s.Content.Units.Values.FirstOrDefault(x => kv.Key.StartsWith(x.Name));
                    if (u != null && pred(u)) v += kv.Value;
                }
                return v / total;
            }
            int towers = s.W.CountFamily(BuildingFamily.ArcherTower) + s.W.CountFamily(BuildingFamily.MagicTower);
            float flyer = Share(u => u.Flying);
            if (flyer > 0.25f) tips.Add($"Kẻ bay gây {flyer:P0} sát thương lên Lâu Đài. Tháp Cung, Cung Thủ và Kỵ Sĩ Sư Ưng bắn được kẻ bay – đặt chúng gần Lâu Đài.");
            float siege = Share(u => u.Behavior == EnemyBehavior.Siege || u.Behavior == EnemyBehavior.GateBreaker || u.Behavior == EnemyBehavior.Boss);
            if (siege > 0.35f) tips.Add($"Công thành/boss gây {siege:P0} sát thương. Lính Giáo (thưởng vs to lớn), Tháp Bắn Tỉa và tướng tung tối thượng vào lúc chúng tụ lực sẽ hiệu quả.");
            if (an.Breaches.Count >= 4)
            {
                var night = an.Breaches.GroupBy(b => b.Night).OrderByDescending(g => g.Count()).First();
                tips.Add($"Tường bị phá {an.Breaches.Count} lần (nhiều nhất đêm {night.Key}). Nâng lên Tường Đá/Thành Lũy và đặt tháp ngay sau đoạn tường đó.");
            }
            if (towers < Math.Max(2, s.Night / 2)) tips.Add($"Bạn chỉ có {towers} tháp ở đêm {s.Night}. Tháp tự bắn cả khi tướng ở mặt trận khác.");
            int rx = an.Reactions.Values.Sum();
            if (rx < s.Night * 3) tips.Add("Rất ít phản ứng nguyên tố. Thử Tháp Nước + tháp/lính Sét (Điện Giật) hoặc Băng + Đất (Gai Băng).");
            if (s.W.CountFamily(BuildingFamily.Barracks) == 0) tips.Add("Không có Doanh Trại: lính tự động chặn kẻ địch trước khi chúng tới tường.");
            var lastNight = an.Nights.LastOrDefault();
            if (s.Plan?.BossUnitId != null)
            {
                var b = s.Content.Unit(s.Plan.BossUnitId);
                if (b.BossId != null && s.Content.Bosses.TryGetValue(b.BossId, out var bd)) tips.Add("Mẹo boss: " + bd.Counterplay);
            }
            return tips.Take(3).ToList();
        }
    }
}
