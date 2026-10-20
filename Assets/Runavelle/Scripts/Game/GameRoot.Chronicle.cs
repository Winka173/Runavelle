using System.Linq;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>P5 Kingdom Chronicle screens: searchable archive (Profile tab) and the replay-seed shortcut.</summary>
    public sealed partial class GameRoot
    {
        private string _chronicleQuery = "";
        private int _chronicleSel;
        private Vector2 _chronicleScroll;

        private void DrawChronicleTab(Rect r)
        {
            var list = Profile.Chronicles;
            if (list.Count == 0)
            {
                Gui.Text(new Rect(r.x, r.y, r.width, 80), "Chưa có biên niên nào. Mỗi ván kết thúc sẽ được ghi lại (sự kiện thật, không phải ảnh minh hoạ).", Gui.Label);
                return;
            }
            Gui.Text(new Rect(r.x, r.y, 160, 52), "Tìm:", Gui.Label);
            _chronicleQuery = GUI.TextField(new Rect(r.x + 90, r.y, r.width - 90, 52), _chronicleQuery ?? "", 40, new GUIStyle(GUI.skin.textField) { fontSize = Mathf.RoundToInt(26 * Gui.Scale) });
            float y = r.y + 64;
            // runs (left column)
            for (int i = 0; i < list.Count && i < 10; i++)
            {
                var c = list[i];
                string mname = Db.Maps.TryGetValue(c.Map ?? "", out var md) ? md.Name : c.Map;
                string label = $"{c.StartedUtc} {(c.Result == "WIN" ? "THẮNG" : "THUA")} {c.Nights}đ · {mname}";
                if (Gui.Btn(new Rect(r.x, y + i * 50, 300, 46), label, _chronicleSel != i, Gui.ButtonSmall)) { _chronicleSel = i; _chronicleScroll = Vector2.zero; }
            }
            var sel = list[Mathf.Clamp(_chronicleSel, 0, list.Count - 1)];
            var hits = string.IsNullOrWhiteSpace(_chronicleQuery) ? sel.Entries : sel.Search(_chronicleQuery, Db);
            var er = new Rect(r.x + 316, y, r.width - 316, r.height - 150);
            Gui.Text(new Rect(er.x, er.y, er.width, 34), $"<b>Mã {sel.Seed}</b> · {(Db.Generals.TryGetValue(sel.General ?? "", out var gd) ? gd.Name : sel.General)}" +
                (sel.Ascension > 0 ? $" · TC{sel.Ascension}" : "") + $" · {hits.Count}/{sel.Entries.Count} mục", Gui.LabelSmall);
            float ey = er.y + 40;
            foreach (var e in hits.Skip((int)_chronicleScroll.y).Take(Mathf.Max(1, (int)((er.height - 50) / 34))))
            {
                Gui.Text(new Rect(er.x, ey, er.width, 34), $"<color=#6a5a8a>Đêm {e.Night} {(e.AtNight ? "(đêm)" : "(ngày)")}</color> {e.Text}", Gui.LabelSmall);
                ey += 34;
            }
            if (hits.Count > 12)
            {
                if (Gui.Btn(new Rect(er.xMax - 230, er.yMax + 4, 110, 46), "Lên", _chronicleScroll.y > 0, Gui.ButtonSmall)) _chronicleScroll.y = Mathf.Max(0, _chronicleScroll.y - 10);
                if (Gui.Btn(new Rect(er.xMax - 110, er.yMax + 4, 110, 46), "Xuống", _chronicleScroll.y + 10 < hits.Count, Gui.ButtonSmall)) _chronicleScroll.y += 10;
            }
            if (Gui.Btn(new Rect(er.x, er.yMax + 4, 360, 52), "CHƠI LẠI MÃ NÀY", Db.Maps.ContainsKey(sel.Map ?? "") && Db.Generals.ContainsKey(sel.General ?? ""), Gui.Button)) ReplayChronicle(sel);
        }

        /// <summary>Replay: same map/general/seed/ascension/challenge (decrees and builds are the player's to choose again).</summary>
        private void ReplayChronicle(RunChronicle c)
        {
            var setup = c.ToReplaySetup();
            setup.UnlockedGenerals.AddRange(Profile.UnlockedGenerals(Db));
            if (!setup.UnlockedGenerals.Contains(setup.GeneralId)) return;
            ApplyCosmetics();
            BeginSession(new GameSession(Db, setup));
        }
    }
}
