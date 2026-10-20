using System;
using System.Collections.Generic;

namespace Runavelle.Core
{
    public enum BarkTrigger { NightStart, WallBreach, TimelySave, Ultimate, BossDefeat, Injured }

    public struct Bark
    {
        public int SpeakerId;
        public string Speaker;
        public string Text;
        public float Until;
        public BarkTrigger Trigger;
    }

    /// <summary>
    /// Sparse AI General voice lines (subtitled; localisation hook = the line table). Global cooldown, per-trigger
    /// cooldown, one line on screen at a time, priority lets critical lines replace minor ones. Never spams.
    /// </summary>
    public sealed class BarkDirector
    {
        public const float GlobalCooldown = 7f;
        public const float TriggerCooldown = 25f;
        public const float Duration = 3.2f;
        public bool Enabled = true;
        public Bark? Current { get; private set; }
        private float _lastAny = -999f;
        private readonly Dictionary<BarkTrigger, float> _last = new Dictionary<BarkTrigger, float>();
        private int _rot;

        public static int Priority(BarkTrigger t) => t == BarkTrigger.WallBreach || t == BarkTrigger.Injured ? 3 : t == BarkTrigger.BossDefeat || t == BarkTrigger.TimelySave ? 2 : 1;

        // Vietnamese line table; {0} = element flavour word. Kept data-only for future localisation.
        public static readonly Dictionary<BarkTrigger, string[]> Lines = new Dictionary<BarkTrigger, string[]>
        {
            [BarkTrigger.NightStart] = new[] { "Giữ vững trận tuyến!", "Đêm nay ta canh {0}.", "Chúng tới rồi – vào vị trí!" },
            [BarkTrigger.WallBreach] = new[] { "Tường thủng! Ta sẽ chặn lối!", "Lỗ hổng ở phía này – dồn quân!", "Đừng để chúng lọt qua!" },
            [BarkTrigger.TimelySave] = new[] { "Kịp lúc lắm!", "Đứng sau ta!", "Không ai bị bỏ lại!" },
            [BarkTrigger.Ultimate] = new[] { "Nếm sức mạnh {0} đi!", "Giờ là lúc!", "Lùi lại – {0} đây!" },
            [BarkTrigger.BossDefeat] = new[] { "Kẻ khổng lồ đã ngã!", "Vương quốc vẫn đứng vững!", "Chiến thắng thuộc về ta!" },
            [BarkTrigger.Injured] = new[] { "Ta bị thương – rút về hồi sức!", "Che chắn cho ta!", "Chưa xong đâu…" },
        };

        public static string Flavor(ElementId e)
        {
            switch (e)
            {
                case ElementId.Fire: return "lửa"; case ElementId.Water: return "thủy triều"; case ElementId.Lightning: return "sấm sét";
                case ElementId.Ice: return "băng giá"; case ElementId.Wind: return "cuồng phong"; case ElementId.Earth: return "đại địa";
                case ElementId.Light: return "ánh sáng"; case ElementId.Shadow: return "bóng tối"; default: return "thép";
            }
        }

        /// <summary>Returns true when the line is shown.</summary>
        public bool Say(BarkTrigger t, Unit speaker, float now)
        {
            if (!Enabled || speaker == null || speaker.IsPlayerControlled || speaker.General == null) return false;
            if (Current.HasValue && now < Current.Value.Until && Priority(Current.Value.Trigger) >= Priority(t)) return false;
            if (now - _lastAny < GlobalCooldown && Priority(t) < 3) return false;
            if (_last.TryGetValue(t, out var lt) && now - lt < TriggerCooldown) return false;
            var lines = Lines[t];
            string text = string.Format(lines[(_rot++ + speaker.Id) % lines.Length], Flavor(speaker.General.Element));
            Current = new Bark { SpeakerId = speaker.Id, Speaker = speaker.General.Name, Text = text, Until = now + Duration, Trigger = t };
            _lastAny = now;
            _last[t] = now;
            return true;
        }

        public void Update(float now)
        {
            if (Current.HasValue && now >= Current.Value.Until) Current = null;
        }

        public void ResetClock() { _lastAny = -999f; _last.Clear(); Current = null; }
    }
}
