using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>One Ascension tier. Tiers are cumulative: Ascension N applies tiers 1..N.</summary>
    public sealed class AscensionTier
    {
        public int Tier;
        public string Name;
        public string Description;
    }

    /// <summary>Hand-made run modifier set (Challenge runs, also used by the daily seed).</summary>
    public sealed class ChallengeDefinition
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Extra General mastery points for a victory.</summary>
        public int MasteryBonus;
        public BuildingFamily[] Banned = Array.Empty<BuildingFamily>();
        public List<Modifier> Modifiers = new List<Modifier>();
        public int StartGoldDelta;
        public float BudgetMult = 1f;
        public int TotalNights;
        public int CursedDecrees;
    }

    /// <summary>Endless mode: every night after the campaign adds one seeded mutator (stacking).</summary>
    public sealed class EndlessMutator
    {
        public string Id;
        public string Name;
        public List<Modifier> Modifiers = new List<Modifier>();
        public float BudgetBonus;
    }

    public static class RunRules
    {
        public const int MaxAscension = 10;
        /// <summary>Baseline escalation per endless night (additive percents), applied before the night's mutator.</summary>
        public const float EndlessHpPerNight = 0.15f, EndlessDamagePerNight = 0.08f, EndlessBudgetPerNight = 0.06f;

        public static readonly AscensionTier[] Ascension =
        {
            new AscensionTier { Tier = 1, Name = "Quân Tinh Nhuệ", Description = "Kẻ địch +10% máu." },
            new AscensionTier { Tier = 2, Name = "Ngân Khố Hao Hụt", Description = "Khởi đầu −40 Vàng." },
            new AscensionTier { Tier = 3, Name = "Tinh Anh Biến Dị", Description = "Tinh anh và boss mang 1 đặc tính ngẫu nhiên (Thần Tốc / Thành Lũy / Cuồng Nộ / Hộ Thuẫn)." },
            new AscensionTier { Tier = 4, Name = "Bầy Đàn Đông Hơn", Description = "Ngân sách đợt địch +12%." },
            new AscensionTier { Tier = 5, Name = "Triều Đình Do Dự", Description = "Mỗi lượt Sắc Lệnh chỉ còn 2 lựa chọn, không đổi lại." },
            new AscensionTier { Tier = 6, Name = "Đất Hẹp", Description = "Tối đa 22 công trình (không tính tường/cổng)." },
            new AscensionTier { Tier = 7, Name = "Phần Thưởng Ít Ỏi", Description = "Thưởng chiến thắng mỗi đêm −25%." },
            new AscensionTier { Tier = 8, Name = "Boss Hung Tợn", Description = "Boss +20% máu, +15% sát thương." },
            new AscensionTier { Tier = 9, Name = "Thợ Xây Kiệt Sức", Description = "Bình minh chỉ sửa công trình còn đứng tới 60% máu." },
            new AscensionTier { Tier = 10, Name = "Đêm Tận Thế", Description = "Đêm cuối có thêm boss đêm 10 của bản đồ." },
        };

        public static readonly ChallengeDefinition[] Challenges =
        {
            new ChallengeDefinition { Id = "no_walls", Name = "Không Tường Thành", Description = "Không được xây tường hay cổng. Kẻ địch +10% tốc độ.", MasteryBonus = 15,
                Banned = new[] { BuildingFamily.Wall }, Modifiers = { new Modifier(Stat.MoveSpeed, Scope.Enemies, 0.10f) } },
            new ChallengeDefinition { Id = "arcane_only", Name = "Chỉ Có Phép Thuật", Description = "Không có Tháp Cung. Tháp Phép rẻ hơn 30% Vàng và 25% Mana; Giếng Mana +25%.", MasteryBonus = 10,
                Banned = new[] { BuildingFamily.ArcherTower }, Modifiers = { new Modifier(Stat.CostGold, Scope.MagicTowers, -0.3f), new Modifier(Stat.CostMana, Scope.MagicTowers, -0.25f), new Modifier(Stat.ManaIncome, Scope.Any, 0.25f) } },
            new ChallengeDefinition { Id = "lone_general", Name = "Tướng Đơn Độc", Description = "Không có Doanh Trại. Tướng +25% sát thương, +20% máu; kẻ địch +10% máu.", MasteryBonus = 15,
                Banned = new[] { BuildingFamily.Barracks }, Modifiers = { new Modifier(Stat.Damage, Scope.Generals, 0.25f), new Modifier(Stat.MaxHp, Scope.Generals, 0.2f), new Modifier(Stat.MaxHp, Scope.Enemies, 0.1f) } },
            new ChallengeDefinition { Id = "pauper", Name = "Vương Quốc Nghèo", Description = "Khởi đầu −80 Vàng, thu nhập Vàng −25%; tiền thưởng hạ gục +50%.", MasteryBonus = 10,
                StartGoldDelta = -80, Modifiers = { new Modifier(Stat.GoldIncome, Scope.Any, -0.25f), new Modifier(Stat.Bounty, Scope.Any, 0.5f) } },
            new ChallengeDefinition { Id = "glass_castle", Name = "Lâu Đài Pha Lê", Description = "Lâu Đài −50% máu; mọi tháp +25% sát thương.", MasteryBonus = 10,
                Modifiers = { new Modifier(Stat.MaxHp, Scope.Castle, -0.5f), new Modifier(Stat.Damage, Scope.Towers, 0.25f) } },
            new ChallengeDefinition { Id = "horde", Name = "Đại Quân", Description = "Đợt địch +50%; thưởng chiến thắng +25%.", MasteryBonus = 10,
                BudgetMult = 1.5f, Modifiers = { new Modifier(Stat.NightReward, Scope.Any, 0.25f) } },
            new ChallengeDefinition { Id = "cursed_crown", Name = "Vương Miện Bị Nguyền", Description = "Bắt đầu với 2 Sắc Lệnh Nguyền ngẫu nhiên (cố định theo hạt giống).", MasteryBonus = 15,
                CursedDecrees = 2 },
            new ChallengeDefinition { Id = "blitz", Name = "Chiến Dịch Chớp Nhoáng", Description = "Chỉ 10 đêm, boss cuối xuất hiện ở đêm 10; kẻ địch +20% máu.", MasteryBonus = 5,
                TotalNights = 10, Modifiers = { new Modifier(Stat.MaxHp, Scope.Enemies, 0.2f) } },
        };

        public static ChallengeDefinition Challenge(string id)
        {
            if (id == null) return null;
            foreach (var c in Challenges) if (c.Id == id) return c;
            return null;
        }

        public static readonly EndlessMutator[] Mutators =
        {
            new EndlessMutator { Id = "thick_hide", Name = "Da Dày (+2 giáp kẻ địch)", Modifiers = { new Modifier(Stat.Armor, Scope.Enemies, 0f, 2f) } },
            new EndlessMutator { Id = "frenzy", Name = "Cuồng Bạo (+8% sát thương kẻ địch)", Modifiers = { new Modifier(Stat.Damage, Scope.Enemies, 0.08f) } },
            new EndlessMutator { Id = "vigor", Name = "Sinh Lực (+8% máu kẻ địch)", Modifiers = { new Modifier(Stat.MaxHp, Scope.Enemies, 0.08f) } },
            new EndlessMutator { Id = "haste", Name = "Hành Quân Gấp (+5% tốc độ kẻ địch)", Modifiers = { new Modifier(Stat.MoveSpeed, Scope.Enemies, 0.05f) } },
            new EndlessMutator { Id = "swarm", Name = "Bầy Đàn (+8% quân số)", BudgetBonus = 0.08f },
            new EndlessMutator { Id = "titans", Name = "Khổng Lồ (+15% máu boss)", Modifiers = { new Modifier(Stat.MaxHp, Scope.Bosses, 0.15f) } },
        };

        /// <summary>Deterministic mutator for an endless night (night > campaign length).</summary>
        public static EndlessMutator MutatorFor(ulong seed, int night)
        {
            var rng = Rng.Derive(seed, 7000 + (ulong)night);
            return Mutators[rng.Range(0, Mutators.Length)];
        }

        /// <summary>Deterministic boss for an endless boss night (every 5th night), rotating through every boss.</summary>
        public static string EndlessBoss(ContentDatabase db, ulong seed, int night)
        {
            var bodies = new List<string>();
            foreach (var u in db.Units.Values) if (u.BossId != null) bodies.Add(u.Id);
            bodies.Sort(string.CompareOrdinal);
            var rng = Rng.Derive(seed, 7500 + (ulong)(night / 5));
            return bodies[rng.Range(0, bodies.Count)];
        }

        /// <summary>Offline daily run: same map / General / challenge / seed for everyone on a given UTC date (ignores unlocks).</summary>
        public static RunSetup Daily(ContentDatabase db, int year, int month, int day)
        {
            ulong seed = (ulong)(year * 10000 + month * 100 + day) * 2654435761UL;
            var rng = Rng.Derive(seed, 99);
            var setup = new RunSetup
            {
                Seed = seed, IsDaily = true,
                MapId = db.MapOrder[rng.Range(0, db.MapOrder.Count)],
                GeneralId = db.GeneralOrder[rng.Range(0, db.GeneralOrder.Count)],
            };
            if (rng.Chance(0.6f))
            {
                var c = Challenges[rng.Range(0, Challenges.Length)];
                if (c.TotalNights == 0) setup.ChallengeId = c.Id;
            }
            setup.UnlockedGenerals.AddRange(db.GeneralOrder);
            return setup;
        }
    }

    /// <summary>Compact, checksummed run code ("RV1-XXXX-...") so players can share a seed + setup offline.</summary>
    public static class ShareCode
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/O/0/1

        public static string Encode(ContentDatabase db, RunSetup s)
        {
            var b = new byte[13];
            b[0] = (byte)Math.Max(0, db.MapOrder.IndexOf(s.MapId));
            b[1] = (byte)Math.Max(0, db.GeneralOrder.IndexOf(s.GeneralId));
            b[2] = (byte)Math.Min(RunRules.MaxAscension, Math.Max(0, s.Ascension));
            int ch = 0;
            for (int i = 0; i < RunRules.Challenges.Length; i++) if (RunRules.Challenges[i].Id == s.ChallengeId) ch = i + 1;
            b[3] = (byte)((s.Endless ? 1 : 0) | (ch << 1));
            for (int i = 0; i < 8; i++) b[4 + i] = (byte)(s.Seed >> (8 * i));
            int sum = 0;
            for (int i = 0; i < 12; i++) sum = (sum * 31 + b[i]) & 0xFF;
            b[12] = (byte)sum;
            // base32
            var chars = new List<char>();
            int buffer = 0, bits = 0;
            foreach (var x in b)
            {
                buffer = (buffer << 8) | x; bits += 8;
                while (bits >= 5) { chars.Add(Alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
            }
            if (bits > 0) chars.Add(Alphabet[(buffer << (5 - bits)) & 31]);
            var sb = new System.Text.StringBuilder("RV1");
            for (int i = 0; i < chars.Count; i++) { if (i % 5 == 0) sb.Append('-'); sb.Append(chars[i]); }
            return sb.ToString();
        }

        /// <summary>Returns null on a malformed / tampered code.</summary>
        public static RunSetup Decode(ContentDatabase db, string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            code = code.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
            if (!code.StartsWith("RV1")) return null;
            code = code.Substring(3);
            var bytes = new List<byte>();
            int buffer = 0, bits = 0;
            foreach (var c in code)
            {
                int v = Alphabet.IndexOf(c);
                if (v < 0) return null;
                buffer = (buffer << 5) | v; bits += 5;
                if (bits >= 8) { bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF)); bits -= 8; }
            }
            if (bytes.Count < 13) return null;
            int sum = 0;
            for (int i = 0; i < 12; i++) sum = (sum * 31 + bytes[i]) & 0xFF;
            if (bytes[12] != sum) return null;
            if (bytes[0] >= db.MapOrder.Count || bytes[1] >= db.GeneralOrder.Count || bytes[2] > RunRules.MaxAscension) return null;
            int ch = bytes[3] >> 1;
            if (ch > RunRules.Challenges.Length) return null;
            ulong seed = 0;
            for (int i = 0; i < 8; i++) seed |= (ulong)bytes[4 + i] << (8 * i);
            var s = new RunSetup
            {
                MapId = db.MapOrder[bytes[0]], GeneralId = db.GeneralOrder[bytes[1]], Ascension = bytes[2], Endless = (bytes[3] & 1) != 0,
                ChallengeId = ch > 0 ? RunRules.Challenges[ch - 1].Id : null, Seed = seed,
            };
            var cdef = RunRules.Challenge(s.ChallengeId);
            if (cdef != null && cdef.TotalNights > 0) s.TotalNights = cdef.TotalNights;
            return s;
        }
    }
}
