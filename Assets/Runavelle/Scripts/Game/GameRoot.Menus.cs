using System.Collections.Generic;
using System.Linq;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>Front-end screens: title, General/map selection, settings, history/codex.</summary>
    public sealed partial class GameRoot
    {
        private Vector2 _scroll;

        private void OnGUI()
        {
            Gui.Begin();
            switch (Screen)
            {
                case AppScreen.Title: DrawTitle(); break;
                case AppScreen.GeneralSelect: DrawGeneralSelect(); break;
                case AppScreen.MapSelect: DrawMapSelect(); break;
                case AppScreen.Settings: DrawSettings(); break;
                case AppScreen.History: DrawHistory(); break;
                case AppScreen.Codex: DrawCodex(); break;
                case AppScreen.Run: DrawRun(); break;
            }
            DrawToast();
        }

        private void Backdrop()
        {
            var bg = ArtLibrary.Tex("ui/title_bg");
            if (bg != null) GUI.DrawTexture(new Rect(0, 0, Gui.VW, Gui.VH), bg, ScaleMode.ScaleAndCrop);
        }

        private void DrawToast()
        {
            if (ToastTime > 0 && !string.IsNullOrEmpty(Toast))
            {
                var r = new Rect(Gui.VW / 2 - 420, Gui.Safe.yMax - 210, 840, 64);
                var c = GUI.color;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(ToastTime));
                GUI.Box(r, GUIContent.none, Gui.Tooltip);
                Gui.Text(r, Toast, Gui.Hint);
                GUI.color = c;
            }
        }

        // ------------------------------------------------------------------ title
        private void DrawTitle()
        {
            Backdrop();
            var logo = ArtLibrary.Tex("ui/logo");
            float lx = Gui.Safe.x + 60;
            if (logo != null) GUI.DrawTexture(new Rect(lx - 20, Gui.Safe.y + 40, 760, 190), logo, ScaleMode.ScaleToFit);
            Gui.Text(new Rect(lx + 30, Gui.Safe.y + 220, 700, 40), "Xây ban ngày · Giữ thành ban đêm · Ma pháp nguyên tố", Gui.Hint, Gui.Light, true);
            float y = Gui.Safe.y + 300, w = 460, h = 76;
            if (HasSavedRun && Gui.Btn(new Rect(lx, y, w, h), "TIẾP TỤC", true, Gui.ButtonBig)) ContinueRun();
            if (HasSavedRun) y += h + 16;
            if (Gui.Btn(new Rect(lx, y, w, h), "CHƠI MỚI", true, Gui.ButtonBig)) Screen = AppScreen.GeneralSelect;
            y += h + 16;
            if (Gui.Btn(new Rect(lx, y, w, 64), "THỬ THÁCH HẰNG NGÀY", true, Gui.Button, "blue"))
            {
                var rng = new Rng(DailySeed());
                var gens = Profile.UnlockedGenerals(Db);
                PickGeneral = gens[rng.Range(0, gens.Count)];
                var maps = Db.MapOrder.Where(m => Profile.MapUnlocked(Db.Maps[m])).ToList();
                PickMap = maps[rng.Range(0, maps.Count)];
                PickAscension = 0; PickEndless = false;
                StartNewRun(daily: true);
                ShowToast("Thử thách " + System.DateTime.UtcNow.ToString("dd/MM") + ": hạt giống chung cho mọi người chơi trong ngày (offline).");
            }
            y += 64 + 14;
            float hw = (w - 16) / 2;
            if (Gui.Btn(new Rect(lx, y, hw, 60), "HỒ SƠ", true, Gui.Button)) Screen = AppScreen.History;
            if (Gui.Btn(new Rect(lx + hw + 16, y, hw, 60), "CẨM NANG", true, Gui.Button)) Screen = AppScreen.Codex;
            y += 76;
            if (Gui.Btn(new Rect(lx, y, hw, 60), "CÀI ĐẶT", true, Gui.Button)) { _settingsReturn = AppScreen.Title; Screen = AppScreen.Settings; }
            if (!Application.isMobilePlatform && Gui.Btn(new Rect(lx + hw + 16, y, hw, 60), "THOÁT", true, Gui.Button)) Application.Quit();
            Gui.Text(new Rect(Gui.Safe.xMax - 620, Gui.Safe.yMax - 50, 600, 40), $"Thắng: {Profile.Victories}   Đêm xa nhất: {Profile.BestNight}   Ván: {Profile.RunsPlayed}", new GUIStyle(Gui.Hint) { alignment = TextAnchor.MiddleRight }, Gui.Light, true);
        }

        // ------------------------------------------------------------------ general select
        private void DrawGeneralSelect()
        {
            Backdrop();
            Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.05f, 0.04f, 0.1f, 0.55f));
            Gui.Text(new Rect(0, Gui.Safe.y + 16, Gui.VW, 70), "Chọn Tướng", Gui.Title, null, true);
            var ids = Db.GeneralOrder;
            int cols = 8;
            float cell = 132, gap = 14;
            float gx = Gui.VW / 2 - (cols * (cell + gap)) / 2;
            float gy = Gui.Safe.y + 110;
            for (int i = 0; i < ids.Count; i++)
            {
                var g = Db.Generals[ids[i]];
                bool unlocked = Profile.IsUnlocked(g.Unlock, g.UnlockValue);
                var r = new Rect(gx + (i % cols) * (cell + gap), gy + (i / cols) * (cell + gap), cell, cell);
                Gui.Block(r);
                GUI.DrawTexture(r, ArtLibrary.Tex("portrait/" + g.Id), ScaleMode.ScaleToFit);
                if (!unlocked) { Gui.Rect(r, new Color(0, 0, 0, 0.6f)); Gui.Icon(new Rect(r.center.x - 24, r.center.y - 24, 48, 48), "icon/lock"); }
                if (PickGeneral == g.Id) GUI.Box(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), GUIContent.none, new GUIStyle { normal = { background = ArtLibrary.Tex("ui/button_hover") }, border = new RectOffset(20, 20, 20, 20) });
                if (PickGeneral == g.Id) GUI.DrawTexture(r, ArtLibrary.Tex("portrait/" + g.Id), ScaleMode.ScaleToFit);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { PickGeneral = g.Id; AudioService.Ui(); }
            }
            var sel = Db.Generals[PickGeneral];
            bool selUnlocked = Profile.IsUnlocked(sel.Unlock, sel.UnlockValue);
            var info = new Rect(Gui.VW / 2 - 760, gy + 2 * (cell + gap) + 20, 1520, 330);
            Gui.DrawPanel(info);
            GUI.DrawTexture(new Rect(info.x + 24, info.y + 24, 200, 200), ArtLibrary.Tex("portrait/" + sel.Id), ScaleMode.ScaleToFit);
            Gui.Icon(new Rect(info.x + 24, info.y + 236, 56, 56), ArtLibrary.ElementIcon(sel.Element));
            Gui.Text(new Rect(info.x + 88, info.y + 240, 200, 50), Elements.Name(sel.Element), Gui.H2);
            float tx = info.x + 250;
            Gui.Text(new Rect(tx, info.y + 18, 1200, 50), $"{sel.Name} · {sel.Title}  <size=22>({GroupName(sel.Group)})</size>", Gui.H1);
            Gui.Text(new Rect(tx, info.y + 70, 1240, 60), sel.Description, Gui.Label);
            Gui.Text(new Rect(tx, info.y + 132, 1240, 40), "<b>Hào quang:</b> " + sel.Aura.Description, Gui.LabelSmall);
            Gui.Text(new Rect(tx, info.y + 170, 1240, 70), $"<b>Tối thượng – {sel.Ultimate.Name}</b> (hồi {sel.Ultimate.Cooldown:0}s): {sel.Ultimate.Description}", Gui.LabelSmall);
            if (sel.Evolutions.Length == 2)
                Gui.Text(new Rect(tx, info.y + 240, 1240, 70), $"<b>Tiến hoá (đêm {GameSession.GeneralEvolutionNight}):</b> {sel.Evolutions[0].Name} / {sel.Evolutions[1].Name}", Gui.LabelSmall);
            if (!selUnlocked) Gui.Text(new Rect(tx, info.y + 280, 1240, 40), "<color=#c0392b>Khoá: " + Profile.UnlockText(sel.Unlock, sel.UnlockValue) + "</color>", Gui.Label);
            float by = Gui.Safe.yMax - 100;
            if (Gui.Btn(new Rect(Gui.Safe.x + 40, by, 260, 72), "QUAY LẠI")) Screen = AppScreen.Title;
            if (Gui.Btn(new Rect(Gui.Safe.xMax - 340, by, 300, 72), "TIẾP →", selUnlocked, Gui.ButtonBig)) Screen = AppScreen.MapSelect;
        }

        public static string GroupName(GeneralGroup g)
        {
            switch (g)
            {
                case GeneralGroup.Vanguard: return "Tiên phong";
                case GeneralGroup.Battlemage: return "Chiến pháp sư";
                case GeneralGroup.Commander: return "Chỉ huy";
                default: return "Chuyên gia";
            }
        }

        // ------------------------------------------------------------------ map select
        private void DrawMapSelect()
        {
            Backdrop();
            Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.05f, 0.04f, 0.1f, 0.55f));
            Gui.Text(new Rect(0, Gui.Safe.y + 16, Gui.VW, 70), "Chọn Chiến Trường", Gui.Title, null, true);
            var maps = Db.MapOrder;
            float w = 560, h = 132, gap = 14;
            int cols = maps.Count > 4 ? 2 : 1;
            float x0 = Gui.VW / 2 - (cols * (w + gap)) / 2 - (cols == 1 ? 300 : 0);
            float y0 = Gui.Safe.y + 104;
            for (int i = 0; i < maps.Count; i++)
            {
                var m = Db.Maps[maps[i]];
                bool unlocked = Profile.MapUnlocked(m);
                var r = new Rect(x0 + (i % cols) * (w + gap), y0 + (i / cols) * (h + gap), w, h);
                Gui.DrawPanel(r, PickMap != m.Id);
                var st = new GUIStyle(Gui.H2) { normal = { textColor = PickMap == m.Id ? Gui.Ink : Gui.Light } };
                Gui.Text(new Rect(r.x + 24, r.y + 14, w - 48, 44), (unlocked ? "" : "[Khoá] ") + m.Name + "  " + new string('★', m.Difficulty), st);
                var ls = new GUIStyle(Gui.LabelSmall) { normal = { textColor = PickMap == m.Id ? Gui.Ink : Gui.Light } };
                Gui.Text(new Rect(r.x + 24, r.y + 58, w - 48, 70), unlocked ? "Được che chắn: " + m.ProtectedSides : "Mở khoá: sống tới đêm " + (3 + m.UnlockOrder * 2) + " hoặc thắng " + m.UnlockOrder + " ván", ls);
                if (unlocked && GUI.Button(r, GUIContent.none, GUIStyle.none)) { PickMap = m.Id; AudioService.Ui(); }
            }
            var sel = Db.Maps[PickMap];
            var info = new Rect(cols == 1 ? x0 + w + 30 : Gui.VW / 2 - 560, cols == 1 ? y0 : Gui.Safe.yMax - 330, cols == 1 ? 620 : 1120, 220);
            Gui.DrawPanel(info);
            Gui.Text(new Rect(info.x + 20, info.y + 16, info.width - 40, 110), sel.Description, Gui.Label);
            // ascension + endless
            int maxAsc = Mathf.Min(10, Profile.MaxAscension(sel.Id) + 1);
            if (Profile.Victories == 0) maxAsc = 0;
            PickAscension = Mathf.Clamp(PickAscension, 0, maxAsc);
            float ay = info.y + 130;
            Gui.Text(new Rect(info.x + 20, ay, 300, 44), "Thăng Cấp: " + PickAscension, Gui.H2);
            if (Gui.Btn(new Rect(info.x + 300, ay, 60, 52), "–", PickAscension > 0)) PickAscension--;
            if (Gui.Btn(new Rect(info.x + 370, ay, 60, 52), "+", PickAscension < maxAsc)) PickAscension++;
            if (Profile.EndlessUnlocked && Gui.Btn(new Rect(info.x + 450, ay, 150, 52), PickEndless ? "Vô tận: BẬT" : "Vô tận: TẮT", true, Gui.ButtonSmall, PickEndless ? "purple" : null)) PickEndless = !PickEndless;
            DrawAscensionNote(new Rect(info.x + 20, ay + 56, info.width - 40, 40));
            float by = Gui.Safe.yMax - 100;
            if (Gui.Btn(new Rect(Gui.Safe.x + 40, by, 260, 72), "← QUAY LẠI")) Screen = AppScreen.GeneralSelect;
            if (Gui.Btn(new Rect(Gui.Safe.xMax - 380, by, 340, 72), "BẮT ĐẦU", true, Gui.ButtonBig, "green")) { DeleteRunSave(); StartNewRun(); }
        }

        partial void DrawAscensionNoteImpl(Rect r);
        private void DrawAscensionNote(Rect r) => DrawAscensionNoteImpl(r);

        // ------------------------------------------------------------------ settings
        private void DrawSettings()
        {
            if (Session == null) Backdrop();
            Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.05f, 0.04f, 0.1f, 0.6f));
            var r = new Rect(Gui.VW / 2 - 560, Gui.Safe.y + 60, 1120, Gui.Safe.height - 120);
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x, r.y + 16, r.width, 60), "Cài Đặt", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            float y = r.y + 100, lx = r.x + 60, bx = r.x + 640, bw = 380, bh = 58, step = 70;
            string[] vfx = { "Thấp", "Vừa", "Cao" };
            Row("Hiệu ứng hình ảnh", vfx[Settings.VfxLevel], () => Settings.VfxLevel = (Settings.VfxLevel + 1) % 3);
            Row("Số sát thương", Settings.DamageNumbers ? "Bật" : "Tắt", () => Settings.DamageNumbers = !Settings.DamageNumbers);
            Row("Rung màn hình", Settings.ScreenShake ? "Bật" : "Tắt", () => Settings.ScreenShake = !Settings.ScreenShake);
            Row("Giảm nháy sáng", Settings.ReducedFlashes ? "Bật" : "Tắt", () => Settings.ReducedFlashes = !Settings.ReducedFlashes);
            Row("Biểu tượng nguyên tố (mù màu)", Settings.ElementGlyphs ? "Bật" : "Tắt", () => Settings.ElementGlyphs = !Settings.ElementGlyphs);
            Row("Điều khiển cảm ứng", Settings.TouchControls ? "Bật" : "Tắt", () => Settings.TouchControls = !Settings.TouchControls);
            Row("Ban ngày nhanh (x2)", Settings.FastDay ? "Bật" : "Tắt", () => Settings.FastDay = !Settings.FastDay);
            Row("Chất lượng / FPS", Settings.Quality == 0 ? "Tiết kiệm (30)" : Settings.Quality == 1 ? "Cân bằng (60)" : "Cao (60)", () => Settings.Quality = (Settings.Quality + 1) % 3);
            Row("Cỡ giao diện", Mathf.RoundToInt(Settings.UiScale * 100) + "%", () => Settings.UiScale = Settings.UiScale >= 1.3f ? 0.8f : Settings.UiScale + 0.1f);
            Row("Âm lượng tổng", Mathf.RoundToInt(Settings.Master * 100) + "%", () => Settings.Master = Settings.Master >= 1f ? 0f : Mathf.Min(1f, Settings.Master + 0.1f));
            Row("Nhạc", Mathf.RoundToInt(Settings.Music * 100) + "%", () => Settings.Music = Settings.Music >= 1f ? 0f : Mathf.Min(1f, Settings.Music + 0.1f));
            Row("Hiệu ứng âm thanh", Mathf.RoundToInt(Settings.Sfx * 100) + "%", () => Settings.Sfx = Settings.Sfx >= 1f ? 0f : Mathf.Min(1f, Settings.Sfx + 0.1f));
            if (Gui.Btn(new Rect(r.center.x - 160, r.yMax - 90, 320, 66), "XONG", true, Gui.ButtonBig))
            {
                Settings.Apply();
                SaveProfile();
                Screen = _settingsReturn;
            }

            void Row(string label, string value, System.Action click)
            {
                if (y + bh > r.yMax - 100) return;
                Gui.Text(new Rect(lx, y + 8, 560, 50), label, Gui.Label);
                if (Gui.Btn(new Rect(bx, y, bw, bh), value, true, Gui.Button)) { click(); Settings.Apply(); }
                y += step;
            }
        }

        public void OpenSettingsFromRun()
        {
            _settingsReturn = AppScreen.Run;
            Screen = AppScreen.Settings;
        }

        // ------------------------------------------------------------------ history / profile
        private void DrawHistory()
        {
            Backdrop();
            Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.05f, 0.04f, 0.1f, 0.6f));
            var r = new Rect(Gui.VW / 2 - 800, Gui.Safe.y + 40, 1600, Gui.Safe.height - 160);
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x + 30, r.y + 16, 900, 56), "Hồ Sơ & Thông Thạo", Gui.H1);
            Gui.Text(new Rect(r.x + 30, r.y + 76, 1500, 40), $"Thắng {Profile.Victories} · Đêm xa nhất {Profile.BestNight} · Thành tựu {Profile.Achievements.Count} · Điểm thông thạo {Profile.TotalMastery()}", Gui.Label);
            DrawMasteryPanel(new Rect(r.x + 30, r.y + 120, 700, r.height - 140));
            float y = r.y + 120;
            Gui.Text(new Rect(r.x + 760, y, 800, 40), "<b>Các ván gần đây</b> (hạt giống có thể chia sẻ)", Gui.Label);
            y += 46;
            foreach (var h in Profile.History.Take(12))
            {
                string gname = Db.Generals.TryGetValue(h.General ?? "", out var gd) ? gd.Name : h.General;
                string mname = Db.Maps.TryGetValue(h.Map ?? "", out var md) ? md.Name : h.Map;
                Gui.Text(new Rect(r.x + 760, y, 820, 36), $"{h.Date} · {(h.Result == "WIN" ? "<color=#2e7d32>THẮNG</color>" : "<color=#c0392b>THUA</color>")} đêm {h.Nights} · {gname} · {mname}{(h.Ascension > 0 ? " · TC" + h.Ascension : "")}{(h.Daily ? " · Hằng ngày" : "")} · mã {h.Seed}", Gui.LabelSmall);
                y += 38;
            }
            if (Gui.Btn(new Rect(Gui.Safe.x + 40, Gui.Safe.yMax - 100, 260, 72), "QUAY LẠI")) Screen = AppScreen.Title;
        }

        partial void DrawMasteryPanelImpl(Rect r);
        private void DrawMasteryPanel(Rect r)
        {
            int y = 0;
            foreach (var id in Db.GeneralOrder)
            {
                Profile.GeneralMastery.TryGetValue(id, out int m);
                if (y * 38 + 40 > r.height) break;
                Gui.Text(new Rect(r.x, r.y + y * 38, 300, 36), Db.Generals[id].Name, Gui.LabelSmall);
                Gui.Bar(new Rect(r.x + 200, r.y + y * 38 + 6, 360, 24), Mathf.Clamp01(m / 100f), Gui.Gold, m + " / 100");
                y++;
            }
            DrawMasteryPanelImpl(r);
        }

        // ------------------------------------------------------------------ codex
        private int _codexTab;

        private void DrawCodex()
        {
            Backdrop();
            Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.05f, 0.04f, 0.1f, 0.6f));
            var r = new Rect(Gui.VW / 2 - 820, Gui.Safe.y + 30, 1640, Gui.Safe.height - 140);
            Gui.DrawPanel(r);
            string[] tabs = { "Nguyên tố", "Phản ứng", "Công trình", "Kẻ địch", "Cách chơi" };
            for (int i = 0; i < tabs.Length; i++)
                if (Gui.Btn(new Rect(r.x + 24 + i * 250, r.y + 20, 240, 56), tabs[i], true, Gui.ButtonSmall, _codexTab == i ? "blue" : null)) { _codexTab = i; _scroll = Vector2.zero; }
            var area = new Rect(r.x + 24, r.y + 96, r.width - 48, r.height - 120);
            _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, area.width - 30, 2600));
            float y = 0;
            void Line(string icon, string text, float h = 64)
            {
                if (icon != null) Gui.Icon(new Rect(0, y + 4, 48, 48), icon);
                Gui.Text(new Rect(icon != null ? 64 : 0, y, area.width - 120, h), text, Gui.LabelSmall);
                y += h;
            }
            switch (_codexTab)
            {
                case 0:
                    foreach (var e in Elements.All) { var i = Elements.Info(e); Line(ArtLibrary.ElementIcon(e), $"<b>{i.Name}</b> – trạng thái <b>{i.Status}</b>: {i.StatusDescription}"); }
                    Line(null, "Mỗi tháp/lính/tướng có 1 nguyên tố chính và tối đa 1 nguyên tố phụ (mở ở cấp 3). Sát thương nguyên tố bỏ qua giáp; vật lý bị giáp giảm (tối thiểu 25%).", 90);
                    break;
                case 1:
                    foreach (var rd in ReactionTable.AllBespoke) Line(ArtLibrary.ElementIcon(rd.A), $"<b>{rd.Name}</b> ({Elements.Name(rd.A)} + {Elements.Name(rd.B)}): {rd.Description}");
                    Line(null, $"<b>{ReactionTable.ResonanceDef.Name}</b>: {ReactionTable.ResonanceDef.Description} Phản ứng có thời gian hồi theo mục tiêu và giới hạn {World.MaxReactionsPerTick} phản ứng mỗi nhịp; sát thương do phản ứng tạo ra không gây phản ứng dây chuyền.", 90);
                    break;
                case 2:
                    foreach (var id in Db.BuildingOrder) { var b = Db.Buildings[id]; Line("icon/fam_" + id, $"<b>{b.Name}</b>: {b.Description}"); }
                    Line(null, "Mỗi họ công trình có 3 cấp; ở cấp 3 chọn 1 trong 2 nhánh chuyên môn. Chỉ có 2 tài nguyên: Vàng và Mana. Thu nhập trả lúc bình minh. Công trình bị phá thành phế tích – xây lại vào ban ngày.", 90);
                    break;
                case 3:
                    foreach (var u in Db.Units.Values.Where(u => u.Role == UnitRole.Enemy).OrderBy(u => u.UnlockNight))
                        Line(null, $"<b>{u.Name}</b> {(u.UnlockNight < 99 ? "(từ đêm " + u.UnlockNight + ")" : "")} – {u.Description}", 56);
                    break;
                default:
                    Line(null, "<b>Ban ngày:</b> chọn công trình ở thanh dưới, đặt lên bản đồ. Kéo để vẽ tường. Bấm công trình để nâng cấp/ấn nguyên tố/bán. Xem dự báo đêm ở bên phải rồi bấm BẮT ĐẦU ĐÊM.", 100);
                    Line(null, "<b>Ban đêm:</b> chỉ điều khiển 1 Tướng (WASD / cần điều khiển). Tướng tự đánh. Space/Q hoặc nút vàng: Tối thượng. Shift/E: Lướt. F/Tab: tốc độ 1×/2×. Esc: tạm dừng.", 100);
                    Line(null, "<b>Lính và tháp hoàn toàn tự động</b>: lính bảo vệ khu vực quanh Doanh Trại rồi quay về. Tướng đồng minh (AI) giữ hướng được giao.", 90);
                    Line(null, "<b>Thắng:</b> sống sót 15 đêm. <b>Thua:</b> Lâu Đài bị phá. Lưu tự động mỗi bình minh.", 70);
                    break;
            }
            GUI.EndScrollView();
            if (Gui.Btn(new Rect(Gui.Safe.x + 40, Gui.Safe.yMax - 100, 260, 72), "QUAY LẠI")) Screen = Session != null ? AppScreen.Run : AppScreen.Title;
        }
    }
}
