using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>Phase V: achievements, General mastery levels and the cosmetics they unlock (never raw power).</summary>
    public static partial class Mastery
    {
        public static readonly int[] LevelThresholds = { 0, 20, 50, 100, 180 };
        public static readonly string[] LevelNames = { "Tân binh", "Đồng", "Bạc", "Vàng", "Huyền thoại" };

        public static int Level(int points)
        {
            int lvl = 0;
            for (int i = 0; i < LevelThresholds.Length; i++) if (points >= LevelThresholds[i]) lvl = i;
            return lvl;
        }

        public static int Level(Profile p, string generalId) => Level(p.GeneralMastery.TryGetValue(generalId, out int v) ? v : 0);

        /// <summary>Cosmetic id → (name, how to unlock). Granted automatically by achievements / mastery.</summary>
        public static readonly (string Id, string Name, string Unlock)[] CosmeticList =
        {
            ("badge_silver", "Huy hiệu Bạc dưới chân Tướng", "Một Tướng đạt Thông thạo Bạc"),
            ("badge_gold", "Huy hiệu Vàng dưới chân Tướng", "Một Tướng đạt Thông thạo Vàng"),
            ("banner_crimson", "Cờ Lâu Đài Đỏ Thẫm", "Thắng ở Thăng Cấp 5"),
            ("banner_void", "Cờ Lâu Đài Hư Không", "Sống tới đêm 25 chế độ Vô tận"),
            ("banner_sun", "Cờ Lâu Đài Mặt Trời", "Thắng Thử thách hằng ngày"),
        };

        public static readonly (string Id, string Name)[] AchievementList =
        {
            ("first_victory", "Chiến thắng đầu tiên"), ("night_5", "Qua đêm 5"), ("night_10", "Qua đêm 10"),
            ("asc_1", "Thăng Cấp 1 – chiến thắng"), ("asc_5", "Thăng Cấp 5 – chiến thắng"), ("asc_10", "Thăng Cấp 10 – chiến thắng"),
            ("endless_20", "Vô tận: đêm 20"), ("endless_25", "Vô tận: đêm 25"),
            ("daily_win", "Thắng Thử thách hằng ngày"), ("challenge_win", "Thắng một Thử thách"), ("challenge_all", "Thắng cả 8 Thử thách"),
            ("conqueror_4", "Chinh phạt 4 bản đồ"), ("conqueror_8", "Chinh phạt cả 8 bản đồ"),
            ("reactions_150", "Bậc thầy phản ứng (150 phản ứng/ván)"), ("flawless", "Thành bất khả xâm phạm"),
            ("emperor_slayer", "Hạ Hoàng Đế Hư Vô"), ("dawn_dusk", "Bình minh & Hoàng hôn (thắng với Tướng Ánh Sáng hoặc Bóng Tối)"),
            ("kingdom_fortress", "Thắng với Pháo Đài"), ("kingdom_legion", "Thắng với Quân Đoàn"), ("kingdom_arcanum", "Thắng với Huyền Bí"),
            ("generals_8", "Thông thạo 8 Tướng (≥ Đồng)"), ("objectives_8", "Hoàn thành 8 nhiệm vụ phụ trong 1 ván"),
        };

        static partial void NameMoreImpl(string id, ref string name)
        {
            foreach (var a in AchievementList) if (a.Id == id) { name = a.Name; return; }
            foreach (var c in CosmeticList) if (c.Id == id) { name = c.Name; return; }
        }

        static partial void EvaluateMoreImpl(Profile p, GameSession s, bool victory, Action<string> grant)
        {
            var a = s.W.Analytics;
            bool cleared = victory || (s.Setup.Endless && s.Night - 1 >= s.TotalNights);
            if (cleared)
            {
                if (s.Setup.Ascension >= 1) grant("asc_1");
                if (s.Setup.Ascension >= 5) grant("asc_5");
                if (s.Setup.Ascension >= 10) grant("asc_10");
                if (s.Setup.IsDaily) grant("daily_win");
                if (s.Setup.ChallengeId != null) grant("challenge_win");
                if (p.ChallengesWon.Count >= RunRules.Challenges.Length) grant("challenge_all");
                if (p.MapsWon.Count >= 4) grant("conqueror_4");
                if (p.MapsWon.Count >= 8) grant("conqueror_8");
                var el = s.W.PlayerGeneral?.General.Element ?? ElementId.None;
                if (el == ElementId.Light || el == ElementId.Shadow) grant("dawn_dusk");
                if (s.KingdomEvolution != null) grant("kingdom_" + s.KingdomEvolution);
                float castleLost = 0f;
                foreach (var n in a.Nights) castleLost += n.CastleDamage;
                if (s.W.Castle != null && castleLost < s.W.Castle.MaxHp * 0.25f) grant("flawless");
            }
            int lastNight = s.Night - 1;
            if (s.Setup.Endless && lastNight >= 20) grant("endless_20");
            if (s.Setup.Endless && lastNight >= 25) grant("endless_25");
            int reactions = 0;
            foreach (var v in a.Reactions.Values) reactions += v;
            if (reactions >= 150) grant("reactions_150");
            if (a.KillsByEnemy.ContainsKey("boss_hollow_emperor")) grant("emperor_slayer");
            int objectives = 0;
            foreach (var line in a.BuildLog) if (line.Contains(" objective ")) objectives++;
            if (objectives >= 8) grant("objectives_8");
            int bronze = 0;
            foreach (var v in p.GeneralMastery.Values) if (Level(v) >= 1) bronze++;
            if (bronze >= 8) grant("generals_8");
            GrantCosmetics(p);
        }

        /// <summary>Cosmetics follow from achievements / mastery (idempotent; also run on profile load).</summary>
        public static void GrantCosmetics(Profile p)
        {
            int best = 0;
            foreach (var v in p.GeneralMastery.Values) best = Math.Max(best, Level(v));
            if (best >= 2) p.Cosmetics.Add("badge_silver");
            if (best >= 3) p.Cosmetics.Add("badge_gold");
            if (p.Achievements.Contains("asc_5")) p.Cosmetics.Add("banner_crimson");
            if (p.Achievements.Contains("endless_25")) p.Cosmetics.Add("banner_void");
            if (p.Achievements.Contains("daily_win")) p.Cosmetics.Add("banner_sun");
        }
    }
}
