using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    /// <summary>
    /// HARD CAP: exactly eight elements (+ None = physical). Stable numeric IDs are saved; never renumber, never add a ninth.
    /// Names like Poison/Arcane/Void are flavour only and must map to one of these.
    /// </summary>
    public enum ElementId { None = 0, Fire = 1, Water = 2, Lightning = 3, Ice = 4, Wind = 5, Earth = 6, Light = 7, Shadow = 8 }

    public sealed class ElementInfo
    {
        public ElementId Id;
        public string Key;
        public string Name;
        public string Status;
        public string StatusDescription;
        public float DecayPerSecond;
        public float R, G, B;
        /// <summary>Shape glyph used alongside colour (colour-blind safe): drawn by presentation.</summary>
        public string Glyph;
    }

    public static class Elements
    {
        public const int Count = 9;      // array size incl. None
        public const int ElementCap = 8; // the design lock

        public static readonly ElementId[] All =
        {
            ElementId.Fire, ElementId.Water, ElementId.Lightning, ElementId.Ice,
            ElementId.Wind, ElementId.Earth, ElementId.Light, ElementId.Shadow
        };

        private static readonly ElementInfo[] Infos = new ElementInfo[Count];

        static Elements()
        {
            Set(ElementId.None, "none", "Vật lý", "", "", 0f, 0.85f, 0.85f, 0.85f, "dot");
            Set(ElementId.Fire, "fire", "Lửa", "Bỏng", "Mất máu theo thời gian.", 12f, 1f, 0.45f, 0.15f, "flame");
            Set(ElementId.Water, "water", "Nước", "Ướt", "Dẫn Sét, đóng Băng nhanh hơn. Kéo dài hơn các trạng thái khác.", 6f, 0.25f, 0.6f, 1f, "drop");
            Set(ElementId.Lightning, "lightning", "Sét", "Tích điện", "Kích hoạt phản ứng Sét.", 14f, 0.98f, 0.9f, 0.25f, "bolt");
            Set(ElementId.Ice, "ice", "Băng", "Lạnh cóng", "Chậm theo mức tích tụ; đầy thì Đóng băng.", 10f, 0.6f, 0.92f, 1f, "flake");
            Set(ElementId.Wind, "wind", "Gió", "Chao đảo", "Bị đẩy lùi khi trúng đòn.", 14f, 0.55f, 0.95f, 0.65f, "swirl");
            Set(ElementId.Earth, "earth", "Đất", "Rung chấn", "Giảm giáp theo mức tích tụ.", 14f, 0.78f, 0.58f, 0.32f, "rock");
            Set(ElementId.Light, "light", "Ánh Sáng", "Rạng rỡ", "Nhận thêm 10% sát thương; đồng minh gần đó được hồi máu khi nó trúng đòn.", 12f, 1f, 0.93f, 0.55f, "star");
            Set(ElementId.Shadow, "shadow", "Bóng Tối", "Ấn ký", "Nhận thêm 20% sát thương; chết khi đang bị Ấn ký sẽ trả Mana.", 8f, 0.66f, 0.36f, 0.95f, "eye");
        }

        private static void Set(ElementId id, string key, string name, string status, string desc, float decay, float r, float g, float b, string glyph)
        {
            Infos[(int)id] = new ElementInfo { Id = id, Key = key, Name = name, Status = status, StatusDescription = desc, DecayPerSecond = decay, R = r, G = g, B = b, Glyph = glyph };
        }

        public static ElementInfo Info(ElementId e) => Infos[(int)e];
        public static string Name(ElementId e) => Infos[(int)e].Name;
        public static string StatusName(ElementId e) => Infos[(int)e].Status;
        public static float DecayPerSecond(ElementId e) => Infos[(int)e].DecayPerSecond;

        /// <summary>Allow-list check used by content validation and save loading.</summary>
        public static bool IsValid(ElementId e) => (int)e >= 0 && (int)e <= 8;
        public static bool IsValid(int raw) => raw >= 0 && raw <= 8;
    }

    /// <summary>Stable reaction IDs (saved in analytics; never renumber). 21 bespoke reactions + generic Resonance.</summary>
    public enum ReactionId
    {
        None = 0,
        SteamBurst = 1, Overload = 2, Melt = 3, Firestorm = 4, Magma = 5,
        Electrocharge = 6, FlashFreeze = 7, Rainfall = 8, Mire = 9,
        Superconduct = 10, StaticStorm = 11, Magnetize = 12,
        Blizzard = 13, IceSpikes = 14, Sandstorm = 15,
        Eclipse = 16, Fortify = 17, HauntedArc = 18, SolarFlare = 19, SoulFrost = 20, Purify = 21,
        Resonance = 30,
    }

    public sealed class ReactionDefinition
    {
        public ReactionId Id;
        public string Name;
        public ElementId A;
        public ElementId B;
        public string Description;
        /// <summary>Gauge consumed from the existing aura and the incoming application.</summary>
        public float Consume = 40f;
        /// <summary>Per-target internal cooldown in seconds.</summary>
        public float Cooldown = 0.6f;
    }

    /// <summary>Reaction matrix. Every pair of the 8 elements resolves to a bespoke reaction or generic Resonance.</summary>
    public static class ReactionTable
    {
        private static readonly Dictionary<int, ReactionDefinition> ByPair = new Dictionary<int, ReactionDefinition>();
        private static readonly Dictionary<ReactionId, ReactionDefinition> ById = new Dictionary<ReactionId, ReactionDefinition>();
        private static readonly List<ReactionDefinition> Bespoke = new List<ReactionDefinition>();

        public static IReadOnlyList<ReactionDefinition> AllBespoke => Bespoke;
        public static readonly ReactionDefinition ResonanceDef = new ReactionDefinition
        {
            Id = ReactionId.Resonance, Name = "Cộng Hưởng", Description = "Hai nguyên tố không có phản ứng riêng: nổ nhỏ +30% sát thương đòn đánh.", Consume = 30f, Cooldown = 0.8f
        };

        static ReactionTable()
        {
            Add(ReactionId.SteamBurst, "Hơi Bỏng", ElementId.Fire, ElementId.Water, "Nổ hơi nước: sát thương vùng, kẻ địch gần đó đánh chậm 30% trong 3 giây.");
            Add(ReactionId.Overload, "Quá Tải", ElementId.Fire, ElementId.Lightning, "Vụ nổ lớn: sát thương vùng cao và đẩy lùi.");
            Add(ReactionId.Melt, "Tan Chảy", ElementId.Fire, ElementId.Ice, "Gấp đôi sát thương lên mục tiêu; xoá Lạnh cóng.");
            Add(ReactionId.Firestorm, "Bão Lửa", ElementId.Fire, ElementId.Wind, "Bỏng lan sang kẻ địch xung quanh.");
            Add(ReactionId.Magma, "Dung Nham", ElementId.Fire, ElementId.Earth, "Để lại vũng dung nham đốt kẻ địch đứng trong đó.");
            Add(ReactionId.Electrocharge, "Điện Giật", ElementId.Water, ElementId.Lightning, "Sét nhảy qua tối đa 4 kẻ địch Ướt gần đó.");
            Add(ReactionId.FlashFreeze, "Đóng Băng Tức Thì", ElementId.Water, ElementId.Ice, "Đóng băng mục tiêu ngay (boss kháng dần nếu bị lặp lại).");
            Add(ReactionId.Rainfall, "Mưa Rào", ElementId.Water, ElementId.Wind, "Tạo vùng mưa làm kẻ địch bên trong bị Ướt.");
            Add(ReactionId.Mire, "Bùn Lầy", ElementId.Water, ElementId.Earth, "Vùng bùn: kẻ địch bên trong chậm 50%.");
            Add(ReactionId.Superconduct, "Siêu Dẫn", ElementId.Lightning, ElementId.Ice, "Giáp của mục tiêu và xung quanh giảm một nửa trong 5 giây.");
            Add(ReactionId.StaticStorm, "Bão Tĩnh Điện", ElementId.Lightning, ElementId.Wind, "Sét nhảy sang 3 kẻ địch gần đó.");
            Add(ReactionId.Magnetize, "Từ Hoá", ElementId.Lightning, ElementId.Earth, "Hút kẻ địch xung quanh lại và làm choáng ngắn.");
            Add(ReactionId.Blizzard, "Bão Tuyết", ElementId.Ice, ElementId.Wind, "Vùng bão tuyết làm Lạnh cóng kẻ địch bên trong.");
            Add(ReactionId.IceSpikes, "Gai Băng", ElementId.Ice, ElementId.Earth, "Gai trồi lên: sát thương vùng và giữ chân; gấp đôi lên mục tiêu đang Đóng băng.");
            Add(ReactionId.Sandstorm, "Bão Cát", ElementId.Wind, ElementId.Earth, "Vùng cát: kẻ địch bên trong đánh chậm 40% và mất máu nhẹ.");
            Add(ReactionId.Eclipse, "Nhật Thực", ElementId.Light, ElementId.Shadow, "Nguyền phơi bày: mục tiêu và xung quanh nhận thêm 40% sát thương trong 5 giây.");
            Add(ReactionId.Fortify, "Kiên Cố", ElementId.Light, ElementId.Earth, "Tường, cổng và lính gần mục tiêu nhận lá chắn bằng 12% máu tối đa.");
            Add(ReactionId.HauntedArc, "Sét Ma", ElementId.Shadow, ElementId.Lightning, "Sét nhảy tới mọi kẻ địch đang bị Ấn ký trong 5 ô.");
            Add(ReactionId.SolarFlare, "Lửa Mặt Trời", ElementId.Light, ElementId.Fire, "Bùng sáng: sát thương vùng, kẻ địch bị loá (đánh trượt 50%) 2 giây.");
            Add(ReactionId.SoulFrost, "Hồn Băng", ElementId.Shadow, ElementId.Ice, "Kết liễu kẻ thường dưới 18% máu; boss/tinh anh nhận sát thương lớn thay vào đó.");
            Add(ReactionId.Purify, "Thanh Tẩy", ElementId.Light, ElementId.Water, "Hồi 8% máu cho đồng minh và công trình gần mục tiêu, xoá hiệu ứng xấu của họ.");
        }

        private static void Add(ReactionId id, string name, ElementId a, ElementId b, string desc)
        {
            var def = new ReactionDefinition { Id = id, Name = name, A = a, B = b, Description = desc };
            ByPair[Key(a, b)] = def;
            ById[id] = def;
            Bespoke.Add(def);
        }

        private static int Key(ElementId a, ElementId b)
        {
            int x = (int)a, y = (int)b;
            return x < y ? x * 16 + y : y * 16 + x;
        }

        /// <summary>Returns the reaction for two different elements (bespoke or Resonance); null for same element / None.</summary>
        public static ReactionDefinition Get(ElementId a, ElementId b)
        {
            if (a == b || a == ElementId.None || b == ElementId.None) return null;
            return ByPair.TryGetValue(Key(a, b), out var def) ? def : ResonanceDef;
        }

        public static ReactionDefinition Get(ReactionId id)
        {
            if (id == ReactionId.Resonance) return ResonanceDef;
            ById.TryGetValue(id, out var def);
            return def;
        }
    }
}
