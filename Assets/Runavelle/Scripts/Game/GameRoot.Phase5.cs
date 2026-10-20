using System.Linq;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>Phase V screens: Ascension/Challenge pickers, share codes, daily run, mastery + achievements + cosmetics.</summary>
    public sealed partial class GameRoot
    {
        private bool _codeEntry;
        private string _codeText = "";
        private string _codeError;
        private int _historyTab;

        partial void DrawAscensionNoteImpl(Rect r)
        {
            if (PickAscension <= 0) { Gui.Text(r, "Thăng Cấp 0: luật gốc. Thắng để mở bậc tiếp theo cho bản đồ này.", Gui.Hint); return; }
            var t = RunRules.Ascension[PickAscension - 1];
            Gui.Text(r, $"<b>TC{t.Tier} – {t.Name}:</b> {t.Description}" + (PickAscension > 1 ? " (cộng dồn mọi bậc dưới)" : ""), Gui.LabelSmall);
        }

        /// <summary>Challenge selector (map select screen). Unlocked after the first victory.</summary>
        private void DrawChallengePicker(Rect r)
        {
            if (Profile.Victories == 0) { PickChallenge = null; return; }
            var ids = RunRules.Challenges.Select(c => c.Id).ToList();
            ids.Insert(0, null);
            int i = Mathf.Max(0, ids.IndexOf(PickChallenge));
            var ch = RunRules.Challenge(PickChallenge);
            Gui.Text(new Rect(r.x, r.y, r.width - 150, r.height), "<b>Thử thách:</b> " + (ch != null ? ch.Name + " – " + ch.Description : "(không)") , Gui.LabelSmall);
            if (Gui.Btn(new Rect(r.xMax - 140, r.y, 60, 52), "<")) i = (i + ids.Count - 1) % ids.Count;
            if (Gui.Btn(new Rect(r.xMax - 70, r.y, 60, 52), ">")) i = (i + 1) % ids.Count;
            PickChallenge = ids[i];
        }

        private void StartDaily()
        {
            var d = System.DateTime.UtcNow.Date;
            var setup = RunRules.Daily(Db, d.Year, d.Month, d.Day);
            setup.WeaponId = PickWeapon; setup.RelicId = PickRelic;
            DeleteRunSave();
            StartFromSetup(setup);
            var ch = RunRules.Challenge(setup.ChallengeId);
            ShowToast("Thử thách " + d.ToString("dd/MM") + ": " + Db.Maps[setup.MapId].Name + " · " + Db.Generals[setup.GeneralId].Name + (ch != null ? " · " + ch.Name : "") + " (giống nhau cho mọi người, offline)");
        }

        private void StartFromSetup(RunSetup setup)
        {
            PickMap = setup.MapId; PickGeneral = setup.GeneralId; PickAscension = setup.Ascension; PickEndless = setup.Endless; PickChallenge = setup.ChallengeId;
            if (setup.UnlockedGenerals.Count == 0) setup.UnlockedGenerals.AddRange(Profile.UnlockedGenerals(Db));
            ApplyCosmetics();
            BeginSession(new GameSession(Db, setup));
            Profile.RunsPlayed++;
        }

        private void ApplyCosmetics()
        {
            Mastery.GrantCosmetics(Profile);
            Profile.Settings.TryGetValue("badge", out var badge);
            Profile.Settings.TryGetValue("banner", out var banner);
            WorldView.GeneralBadge = badge != null && Profile.Cosmetics.Contains(badge) ? badge : null;
            WorldView.CastleBanner = banner != null && Profile.Cosmetics.Contains(banner) ? banner : null;
        }

        /// <summary>Modal for typing a share code (title screen).</summary>
        private void DrawCodeEntry()
        {
            if (!_codeEntry) return;
            var r = Modal(900, 360);
            Gui.Text(new Rect(r.x + 40, r.y + 24, r.width - 80, 50), "Nhập mã chia sẻ", Gui.H1);
            _codeText = GUI.TextField(new Rect(r.x + 40, r.y + 100, r.width - 80, 64), _codeText ?? "", 40, new GUIStyle(GUI.skin.textField) { fontSize = Mathf.RoundToInt(32 * Gui.Scale) });
            if (_codeError != null) Gui.Text(new Rect(r.x + 40, r.y + 172, r.width - 80, 40), _codeError, Gui.LabelSmall, new Color(0.75f, 0.2f, 0.15f));
            if (Gui.Btn(new Rect(r.x + 40, r.yMax - 100, 300, 70), "HUỶ")) { _codeEntry = false; _codeError = null; }
            if (Gui.Btn(new Rect(r.xMax - 340, r.yMax - 100, 300, 70), "CHƠI", true, Gui.ButtonBig, "green"))
            {
                var setup = ShareCode.Decode(Db, _codeText);
                if (setup == null) _codeError = "Mã không hợp lệ.";
                else
                {
                    _codeEntry = false; _codeError = null;
                    setup.WeaponId = PickWeapon; setup.RelicId = PickRelic;
                    DeleteRunSave();
                    StartFromSetup(setup);
                    ShowToast("Đang chơi mã " + ShareCode.Encode(Db, setup));
                }
            }
        }

        /// <summary>Endless: a "retire" button on the dawn report once the campaign is beaten.</summary>
        private void DrawRetireButton(Rect r)
        {
            var s = Session;
            if (!s.CanRetire) return;
            if (Gui.Btn(new Rect(r.x + 30, r.yMax - 96, 250, 72), "RÚT LUI", true, Gui.Button, "purple")) s.RetireEndless();
        }

        /// <summary>Extra end-of-run lines: rules, share code, mastery progress.</summary>
        private float DrawEndExtras(Rect r, float y)
        {
            var s = Session;
            void L(string t, float h = 36) { Gui.Text(new Rect(r.x, y, r.width / 2 - 20, h), t, Gui.LabelSmall); y += h; }
            var code = ShareCode.Encode(Db, s.Setup);
            L("Mã chia sẻ: <b>" + code + "</b>");
            if (Gui.Btn(new Rect(r.x + r.width / 2 - 200, y - 40, 170, 40), "Sao chép", true, Gui.ButtonSmall)) { GUIUtility.systemCopyBuffer = code; ShowToast("Đã sao chép mã " + code); }
            var ch = s.Challenge;
            if (ch != null) L("Thử thách: " + ch.Name + (s.ChallengeCurses.Count > 0 ? " (" + string.Join(", ", s.ChallengeCurses.Select(c => Db.Decrees[c].Name)) + ")" : ""));
            if (s.EndlessMutatorsActive.Count > 0) L("Biến thể Vô tận: " + string.Join(", ", s.EndlessMutatorsActive.GroupBy(m => m.Name).Select(g => g.Key + (g.Count() > 1 ? " ×" + g.Count() : ""))), 60);
            Profile.GeneralMastery.TryGetValue(s.Setup.GeneralId, out int pts);
            int lvl = Mastery.Level(pts);
            string next = lvl + 1 < Mastery.LevelThresholds.Length ? " / " + Mastery.LevelThresholds[lvl + 1] : "";
            L($"Thông thạo {s.W.PlayerGeneral.General.Name}: <b>{Mastery.LevelNames[lvl]}</b> ({pts}{next} điểm)");
            return y;
        }

        partial void DrawMasteryPanelImpl(Rect r) { }

        /// <summary>One-time onboarding hints for systems a player meets later (hazards, side objectives, endless, ascension).</summary>
        private void FeatureHints()
        {
            var s = Session;
            bool Once(string key, string text)
            {
                if (Profile.Settings.TryGetValue("hint_" + key, out var v) && v == "1") return false;
                Profile.Settings["hint_" + key] = "1";
                ShowBanner(text);
                return true; // one banner per frame
            }
            if (s.W.Map.Hazard != MapHazard.None && Once("hz_" + s.W.Map.Hazard, "Bản đồ có hiểm hoạ: " + HazardNote(s) + ". Xem dự báo bên phải.")) return;
            if (s.Night == 2 && s.Objective != null && Once("objective", "Mỗi đêm có một nhiệm vụ phụ (dự báo bên phải) – hoàn thành để nhận thưởng lúc bình minh.")) return;
            if (s.IsEndlessNight && Once("endless", "Vô tận: mỗi đêm kẻ địch mạnh hơn và thêm một biến thể. Rút lui sau bất kỳ đêm thắng nào.")) return;
            if (s.Setup.Ascension >= 3 && s.Night == 1) Once("affix", "Thăng Cấp 3+: tinh anh và boss mang đặc tính (tên hiện trong cảnh báo).");
        }

        /// <summary>History screen, tab 1: achievements + cosmetics (selectable).</summary>
        private void DrawAchievementsTab(Rect r)
        {
            float y = r.y;
            Gui.Text(new Rect(r.x, y, r.width, 40), $"<b>Thành tựu</b> ({Profile.Achievements.Count}/{Mastery.AchievementList.Length}) · Vô tận xa nhất: đêm {Profile.BestEndlessNight} · Thử thách đã thắng: {Profile.ChallengesWon.Count}/{RunRules.Challenges.Length}", Gui.LabelSmall);
            y += 42;
            int col = 0;
            foreach (var a in Mastery.AchievementList)
            {
                bool got = Profile.Achievements.Contains(a.Id);
                Gui.Text(new Rect(r.x + col * (r.width / 2), y, r.width / 2 - 10, 32), (got ? "<color=#2e7d32>[Đạt]</color> " : "<color=#9a9a9a>[  ]</color> ") + a.Name, new GUIStyle(Gui.LabelSmall) { fontSize = 17 });
                if (++col == 2) { col = 0; y += 32; }
            }
            if (col == 1) y += 32;
            y += 12;
            Gui.Text(new Rect(r.x, y, r.width, 36), "<b>Trang trí</b> (chỉ hình thức – bấm để dùng)", Gui.LabelSmall);
            y += 38;
            Mastery.GrantCosmetics(Profile);
            foreach (var c in Mastery.CosmeticList)
            {
                bool owned = Profile.Cosmetics.Contains(c.Id);
                string key = c.Id.StartsWith("badge") ? "badge" : "banner";
                Profile.Settings.TryGetValue(key, out var cur);
                bool on = cur == c.Id;
                string label = (on ? "[Đang dùng] " : "") + c.Name + (owned ? "" : " – " + c.Unlock);
                if (Gui.Btn(new Rect(r.x, y, r.width - 10, 44), label, owned, Gui.ButtonSmall, on ? "green" : null))
                {
                    Profile.Settings[key] = on ? null : c.Id;
                    SaveProfile();
                }
                y += 50;
            }
        }
    }
}
