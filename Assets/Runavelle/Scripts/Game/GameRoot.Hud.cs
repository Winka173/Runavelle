using System.Collections.Generic;
using System.Linq;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>In-run HUD: day build bar, forecast, selection panel, AI Generals; night combat HUD; overlays.</summary>
    public sealed partial class GameRoot
    {
        private readonly List<SpriteRenderer> _ghost = new List<SpriteRenderer>();
        private SpriteRenderer _rangeRing;
        private readonly List<Int2> _wallValid = new List<Int2>(), _wallInvalid = new List<Int2>();

        private static readonly string[] FamilyOrder = { "house", "mana_well", "wall", "archer_tower", "magic_tower", "barracks", "shrine", "castle" };

        private void DrawRun()
        {
            if (Session == null) return;
            var s = Session;
            DrawWorldLabels();
            DrawTopBar();
            switch (s.Phase)
            {
                case RunPhase.Day: DrawDayHud(); break;
                case RunPhase.Night: DrawNightHud(); break;
                case RunPhase.Report: DrawReport(); break;
                case RunPhase.DecreeDraft: DrawDraft(); break;
                case RunPhase.GeneralEvolution: DrawGeneralEvolution(); break;
                case RunPhase.KingdomEvolution: DrawKingdomEvolution(); break;
                case RunPhase.Victory:
                case RunPhase.Defeat: DrawEnd(); break;
            }
            if (BannerTime > 0 && !string.IsNullOrEmpty(Banner))
            {
                var c = GUI.color;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(BannerTime));
                var r = new Rect(Gui.VW / 2 - 560, Gui.Safe.y + 110, 1120, 70);
                GUI.Box(r, GUIContent.none, Gui.Tooltip);
                Gui.Text(r, Banner, new GUIStyle(Gui.Hint) { fontSize = 28, font = Gui.Heading });
                GUI.color = c;
            }
            if (Paused && (s.Phase == RunPhase.Night || s.Phase == RunPhase.Day)) DrawPause();
        }

        // ------------------------------------------------------------------ floating texts / offscreen arrows
        private void DrawWorldLabels()
        {
            foreach (var t in View.Vfx.Texts)
            {
                var p = Rig.WorldToGui(t.World);
                float a = 1f - Mathf.Clamp01((t.Life - t.Max * 0.6f) / (t.Max * 0.4f));
                var st = new GUIStyle(Gui.Number) { fontSize = Mathf.RoundToInt(t.Size), alignment = TextAnchor.MiddleCenter, font = t.TargetId >= 0 ? Gui.Numbers : Gui.Heading };
                var c = t.Col; c.a = a;
                Gui.Text(new Rect(p.x - 200, p.y - 20, 400, 40), t.Text, st, c, true);
            }
        }

        private void DrawOffscreenThreats()
        {
            var w = Session.W;
            var cam = Rig.Cam;
            int shown = 0;
            foreach (var u in w.Units)
            {
                if (u.Dead || u.Team != Team.Enemy || shown > 12) continue;
                var vp = cam.WorldToViewportPoint(new Vector3(u.Position.X, u.Position.Y, 0));
                if (vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f) continue;
                // only threats close to our structures / castle
                if (Session.Nav.DistanceToCastle(u.Position) > 22f && !u.IsBoss) continue;
                shown++;
                var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f).normalized;
                float ex = Mathf.Clamp(0.5f + dir.x * 0.47f, 0.03f, 0.97f), ey = Mathf.Clamp(0.5f + dir.y * 0.45f, 0.05f, 0.95f);
                var gp = new Vector2(ex * Gui.VW, (1f - ey) * Gui.VH);
                var m = GUI.matrix;
                float ang = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg + 90f;
                GUIUtility.RotateAroundPivot(ang, gp * Gui.Scale);
                Gui.Icon(new Rect(gp.x - 22, gp.y - 22, 44, 44), "fx/arrowhead", u.IsBoss ? new Color(1f, 0.5f, 0.2f) : new Color(1f, 0.3f, 0.25f, 0.9f));
                GUI.matrix = m;
            }
        }

        // ------------------------------------------------------------------ top bar
        private void DrawTopBar()
        {
            var s = Session;
            var r = new Rect(Gui.Safe.x + 16, Gui.Safe.y + 12, 560, 92);
            Gui.DrawPanel(r, true);
            Gui.Icon(new Rect(r.x + 18, r.y + 18, 54, 54), "icon/gold");
            Gui.Text(new Rect(r.x + 78, r.y + 14, 170, 60), s.Economy.Gold.ToString(), Gui.NumberBig, new Color(0.98f, 0.82f, 0.4f));
            Gui.Icon(new Rect(r.x + 250, r.y + 18, 54, 54), "icon/mana");
            Gui.Text(new Rect(r.x + 310, r.y + 14, 140, 60), s.Economy.Mana.ToString(), Gui.NumberBig, new Color(0.5f, 0.78f, 1f));
            if (s.Phase == RunPhase.Day)
            {
                var inc = s.ProjectedIncome();
                Gui.Text(new Rect(r.x + 430, r.y + 10, 130, 36), "+" + inc.Gold, Gui.Number, new Color(0.98f, 0.82f, 0.4f));
                Gui.Text(new Rect(r.x + 430, r.y + 44, 130, 36), "+" + inc.Mana, Gui.Number, new Color(0.5f, 0.78f, 1f));
            }
            // night counter
            var c = new Rect(Gui.VW / 2 - 200, Gui.Safe.y + 12, 400, 92);
            Gui.DrawPanel(c, true);
            Gui.Icon(new Rect(c.x + 18, c.y + 18, 54, 54), s.Phase == RunPhase.Night ? "icon/night" : "icon/day");
            string phase = s.Phase == RunPhase.Night ? "Đêm" : "Ngày";
            string total = s.Setup.Endless && s.Night > s.TotalNights ? "∞" : s.TotalNights.ToString();
            Gui.Text(new Rect(c.x + 80, c.y + 8, 300, 50), $"{phase} {s.Night}/{total}", new GUIStyle(Gui.H2) { normal = { textColor = Gui.Light } });
            if (s.Phase == RunPhase.Night)
            {
                int alive = s.W.Units.Count(u => !u.Dead && u.Team == Team.Enemy);
                int spawned = s.Plan.Groups.Sum(g => g.Spawned), total2 = s.Plan.TotalEnemies;
                Gui.Bar(new Rect(c.x + 84, c.y + 56, 290, 22), spawned / (float)Mathf.Max(1, total2), new Color(0.9f, 0.4f, 0.35f), $"Địch còn {alive} · {spawned}/{total2}");
            }
            else Gui.Text(new Rect(c.x + 84, c.y + 52, 300, 34), s.Setup.Ascension > 0 ? "Thăng Cấp " + s.Setup.Ascension : s.Setup.IsDaily ? "Thử thách hằng ngày" : s.W.Map.Name, Gui.Hint);
            // castle hp + controls
            var rr = new Rect(Gui.Safe.xMax - 576, Gui.Safe.y + 12, 560, 92);
            Gui.DrawPanel(rr, true);
            Gui.Icon(new Rect(rr.x + 16, rr.y + 18, 54, 54), "icon/castle");
            var castle = s.W.Castle;
            Gui.Bar(new Rect(rr.x + 78, rr.y + 28, 260, 34), castle.HpFraction, castle.HpFraction < 0.35f ? Gui.Bad : Gui.Good, Mathf.CeilToInt(castle.Hp) + "/" + Mathf.CeilToInt(castle.MaxHp));
            if (s.Phase == RunPhase.Night && Gui.IconBtn(new Rect(rr.x + 352, rr.y + 14, 64, 64), "icon/speed", null, true, Speed == 2)) Speed = Speed == 1 ? 2 : 1;
            if (s.Phase == RunPhase.Night) Gui.Text(new Rect(rr.x + 352, rr.y + 70, 64, 22), Speed + "×", new GUIStyle(Gui.Hint) { fontSize = 18 });
            if (Gui.IconBtn(new Rect(rr.x + 422, rr.y + 14, 64, 64), "icon/pause")) Paused = !Paused;
            if (Gui.IconBtn(new Rect(rr.x + 488, rr.y + 14, 64, 64), "icon/decree")) _showDecrees = !_showDecrees;
            if (_showDecrees) DrawDecreeList(new Rect(rr.x - 200, rr.yMax + 10, 760, 420));
        }

        private bool _showDecrees;

        private void DrawDecreeList(Rect r)
        {
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x + 20, r.y + 14, r.width - 40, 44), "Chiếu chỉ & tiến hoá", Gui.H2);
            float y = r.y + 60;
            if (Session.KingdomEvolution != null) { Gui.Text(new Rect(r.x + 20, y, r.width - 40, 34), "<b>Vương quốc:</b> " + Session.KingdomEvolutions.Find(k => k.Id == Session.KingdomEvolution)?.Name, Gui.LabelSmall); y += 34; }
            if (Session.W.PlayerGeneral.Evolution != null) { Gui.Text(new Rect(r.x + 20, y, r.width - 40, 34), "<b>Tướng:</b> " + Session.W.PlayerGeneral.Evolution.Name, Gui.LabelSmall); y += 34; }
            if (Session.TakenDecrees.Count == 0) Gui.Text(new Rect(r.x + 20, y, r.width - 40, 40), "Chưa có chiếu chỉ. Chiếu chỉ được ban sau mỗi đêm chẵn.", Gui.LabelSmall);
            foreach (var id in Session.TakenDecrees.Distinct())
            {
                if (y > r.yMax - 40) break;
                var d = Db.Decrees[id];
                int n = Session.DecreeStacks(id);
                Gui.Text(new Rect(r.x + 20, y, r.width - 40, 52), $"<b>{d.Name}</b>{(n > 1 ? " ×" + n : "")}: {d.Description}", Gui.LabelSmall);
                y += 50;
            }
        }

        // ------------------------------------------------------------------ day HUD
        private void DrawDayHud()
        {
            var s = Session;
            // forecast (right)
            var fr = new Rect(Gui.Safe.xMax - 476, Gui.Safe.y + 120, 460, 400);
            Gui.DrawPanel(fr);
            Gui.Text(new Rect(fr.x + 20, fr.y + 12, 420, 44), $"Dự báo đêm {s.Night}", Gui.H2);
            float y = fr.y + 58;
            Gui.Text(new Rect(fr.x + 20, y, 420, 34), "<b>Hướng:</b> " + string.Join(", ", s.Plan.Lanes.Select(l => s.LaneById(l)?.Name ?? l)), Gui.LabelSmall);
            y += 38;
            foreach (var tag in s.Plan.Tags.Take(5))
            {
                Gui.Icon(new Rect(fr.x + 20, y, 30, 30), TagIcon(tag));
                Gui.Text(new Rect(fr.x + 58, y, 380, 32), tag, Gui.LabelSmall, tag.StartsWith("BOSS") ? new Color(0.75f, 0.2f, 0.15f) : (Color?)null);
                y += 32;
            }
            var counts = new Dictionary<string, int>();
            foreach (var g in s.Plan.Groups) { counts.TryGetValue(g.UnitId, out int n); counts[g.UnitId] = n + g.Count; }
            float x = fr.x + 20;
            y += 6;
            foreach (var kv in counts.OrderByDescending(k => k.Value).Take(8))
            {
                var tex = ArtLibrary.Tex("enemy/" + kv.Key + "_0");
                if (tex != null) GUI.DrawTexture(new Rect(x, y, 48, 48), tex, ScaleMode.ScaleToFit);
                Gui.Text(new Rect(x, y + 38, 52, 26), "×" + kv.Value, new GUIStyle(Gui.LabelSmall) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
                x += 52;
            }
            if (Gui.Btn(new Rect(fr.x + 20, fr.yMax - 76, 420, 64), "BẮT ĐẦU ĐÊM", true, Gui.ButtonBig, "red")) StartNightAction();

            DrawAiGeneralPanel(new Rect(Gui.Safe.xMax - 476, fr.yMax + 10, 460, 170));
            DrawBuildBar();
            if (BuildLevel == null && SelectedBuilding >= 0) DrawSelection();
            if (BuildLevel != null) DrawBuildHelp();
        }

        private static string TagIcon(string tag)
        {
            if (tag.StartsWith("BOSS")) return "icon/boss";
            if (tag.StartsWith("Kẻ bay")) return "icon/flyer";
            if (tag.StartsWith("Công thành")) return "icon/siege";
            if (tag.StartsWith("Độn thổ")) return "icon/burrow";
            if (tag.StartsWith("Tàng hình")) return "icon/stealth";
            if (tag.StartsWith("Hút")) return "icon/drain";
            if (tag.StartsWith("Giáp")) return "icon/armor";
            if (tag.StartsWith("Tinh anh")) return "icon/elite";
            if (tag.StartsWith("Triệu")) return "icon/summon";
            return "icon/swarm";
        }

        private void DrawAiGeneralPanel(Rect r)
        {
            var s = Session;
            if (s.AiSlotsAvailable == 0) return;
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x + 20, r.y + 10, r.width - 40, 36), "Tướng đồng minh (AI)", Gui.H2);
            for (int slot = 0; slot < s.AiSlotsAvailable; slot++)
            {
                float y = r.y + 54 + slot * 56;
                if (!s.AiSlotCommissioned[slot])
                {
                    var avail = s.AvailableAiGenerals();
                    var cost = GameSession.CommissionCost(slot);
                    if (avail.Count == 0) { Gui.Text(new Rect(r.x + 20, y, 400, 40), "Không còn tướng khả dụng", Gui.LabelSmall); continue; }
                    string gid = avail[(_aiPick + slot) % avail.Count];
                    if (Gui.Btn(new Rect(r.x + 20, y, 60, 48), "◄")) _aiPick = (_aiPick + avail.Count - 1) % avail.Count;
                    Gui.Text(new Rect(r.x + 86, y + 6, 160, 40), Db.Generals[gid].Name, Gui.Label);
                    if (Gui.Btn(new Rect(r.x + 240, y, 190, 48), "Thuê " + cost.Gold + "g " + cost.Mana + "m", s.Economy.CanAfford(cost), Gui.ButtonSmall)) s.CommissionAiGeneral(slot, gid);
                }
                else
                {
                    var g = Db.Generals[s.AiGeneralIds[slot]];
                    Gui.Text(new Rect(r.x + 20, y + 6, 180, 40), g.Name, Gui.Label);
                    var lane = s.LaneById(s.AiGeneralLanes[slot]);
                    if (Gui.Btn(new Rect(r.x + 190, y, 240, 48), "Giữ: " + (lane?.Name ?? "?"), true, Gui.ButtonSmall))
                    {
                        var ground = s.W.Map.Lanes.Where(l => !l.FlyerOnly).ToList();
                        int i = ground.FindIndex(l => l.Id == s.AiGeneralLanes[slot]);
                        s.AssignAiLane(slot, ground[(i + 1) % ground.Count].Id);
                    }
                }
            }
        }

        private int _aiPick;

        private void DrawBuildBar()
        {
            var s = Session;
            float slot = 116, gap = 10;
            float w = FamilyOrder.Length * (slot + gap) + 30 + 2 * (slot * 0.8f + gap);
            var r = new Rect(Gui.VW / 2 - w / 2, Gui.Safe.yMax - slot - 34, w, slot + 24);
            Gui.DrawPanel(r, true);
            float x = r.x + 16;
            foreach (var fid in FamilyOrder)
            {
                var def = Db.Buildings[fid];
                var rect = new Rect(x, r.y + 12, slot, slot);
                bool sel = BuildFamilyMenu == fid || (BuildLevel != null && Db.LevelOwner[BuildLevel] == fid);
                string preview = fid == "castle" ? "building/" + s.W.Castle.Level.Id : ArtLibrary.LevelPreviewKey(Db.Level(def.RootLevels[0]), Db);
                if (Gui.IconBtn(rect, preview, null, true, sel))
                {
                    if (fid == "castle") { ClearBuildMode(); SelectedBuilding = s.W.Castle.Id; Rig.Focus = WorldView.V(s.W.Castle.Center); }
                    else if (def.RootLevels.Length > 1) { BuildFamilyMenu = BuildFamilyMenu == fid ? null : fid; BuildLevel = null; }
                    else { BeginBuild(def.RootLevels[0]); }
                }
                var cost = fid == "castle" ? Cost.Free : s.CostOf(Db.Level(def.RootLevels[0]));
                Gui.Text(new Rect(x, rect.yMax - 30, slot, 28), fid == "castle" ? "Nâng cấp" : CostShort(cost, def.RootLevels.Length > 1), new GUIStyle(Gui.Hint) { fontSize = 17 });
                x += slot + gap;
            }
            if (Gui.IconBtn(new Rect(x + 10, r.y + 22, slot * 0.8f, slot * 0.8f), "icon/undo", null, s.UndoStack.Count > 0) && s.UndoLast()) ShowToast("Đã hoàn tác.");
            if (BuildFamilyMenu != null) DrawVariantMenu(BuildFamilyMenu, new Rect(r.x, r.y - 250, r.width, 240));
        }

        private string CostShort(Cost c, bool from) => (from ? "từ " : "") + (c.Gold > 0 ? c.Gold + "g" : "") + (c.Mana > 0 ? " " + c.Mana + "m" : "");

        private void DrawVariantMenu(string fid, Rect r)
        {
            var def = Db.Buildings[fid];
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x + 20, r.y + 10, r.width - 40, 40), def.Name + ": " + def.Description, Gui.LabelSmall);
            float x = r.x + 20, cell = Mathf.Min(160, (r.width - 40) / Mathf.Max(1, def.RootLevels.Length) - 8);
            foreach (var lid in def.RootLevels)
            {
                var l = Db.Level(lid);
                var rect = new Rect(x, r.y + 56, cell, cell * 0.72f);
                bool unlocked = Session.IsLevelUnlocked(l);
                if (Gui.IconBtn(rect, ArtLibrary.LevelPreviewKey(l, Db), null, unlocked, BuildLevel == lid)) BeginBuild(lid);
                if (l.Element != ElementId.None) Gui.Icon(new Rect(rect.x + 4, rect.y + 4, 30, 30), ArtLibrary.ElementIcon(l.Element));
                string name = l.Soldier != SoldierClass.None ? Loc.Soldier(l.Soldier) : l.Element != ElementId.None ? Elements.Name(l.Element) : l.Name;
                Gui.Text(new Rect(x - 6, rect.yMax + 2, cell + 12, 30), name, new GUIStyle(Gui.LabelSmall) { alignment = TextAnchor.MiddleCenter, fontSize = 18 });
                var c = Session.CostOf(l);
                Gui.Text(new Rect(x - 6, rect.yMax + 28, cell + 12, 26), CostShort(c, false), new GUIStyle(Gui.LabelSmall) { alignment = TextAnchor.MiddleCenter, fontSize = 17 }, Session.Economy.CanAfford(c) ? (Color?)null : Gui.Bad);
                x += cell + 8;
            }
        }

        private void BeginBuild(string levelId)
        {
            BuildLevel = levelId;
            SelectedBuilding = -1;
            HasGhost = false;
            WallDragging = false;
            var l = Db.Level(levelId);
            if (!Session.Economy.CanAfford(Session.CostOf(l))) ShowToast("Không đủ tài nguyên cho " + l.Name);
        }

        private void DrawBuildHelp()
        {
            var l = Db.Level(BuildLevel);
            var def = Db.BuildingOfLevel(BuildLevel);
            var r = new Rect(Gui.Safe.x + 16, Gui.Safe.yMax - 420, 560, 250);
            Gui.DrawPanel(r);
            Gui.Text(new Rect(r.x + 20, r.y + 12, 520, 40), l.Name + "  " + CostShort(Session.CostOf(l), false), Gui.H2);
            Gui.Text(new Rect(r.x + 20, r.y + 54, 520, 100), l.Description + (l.IncomePerDawn.IsFree ? "" : $"  (hoà vốn ~{BreakEven(l):0.#} ngày)"), Gui.LabelSmall);
            bool linear = def.IsLinear && !l.IsGate;
            string help = Input.TouchMode ? (linear ? (WallDragging ? "Chạm điểm cuối rồi bấm XÂY" : "Chạm điểm đầu của tường") : "Chạm để chọn vị trí rồi bấm XÂY")
                                            : (linear ? "Giữ và kéo chuột để vẽ tường" : "Bấm để xây · Shift: xây liên tục · Chuột phải: huỷ");
            Gui.Text(new Rect(r.x + 20, r.y + 150, 520, 36), help, Gui.LabelSmall, Gui.Muted);
            if (Gui.Btn(new Rect(r.x + 20, r.yMax - 66, 200, 54), "HUỶ", true, Gui.Button)) ClearBuildMode();
            if (Input.TouchMode && Gui.Btn(new Rect(r.x + 240, r.yMax - 66, 300, 54), "XÂY", HasGhost, Gui.Button, "green"))
            {
                if (linear) ConfirmWallLine(); else TryPlaceGhost();
            }
        }

        private float BreakEven(BuildingLevel l)
        {
            var c = Session.CostOf(l);
            float value = l.IncomePerDawn.Gold + l.IncomePerDawn.Mana * 1.3f;
            return value <= 0 ? 0 : (c.Gold + c.Mana * 1.3f) / value;
        }

        // ------------------------------------------------------------------ selection panel
        private void DrawSelection()
        {
            var s = Session;
            var b = s.W.Get(SelectedBuilding) as Building;
            if (b == null || b.Dead) { SelectedBuilding = -1; return; }
            var r = new Rect(Gui.Safe.x + 16, Gui.Safe.y + 120, 600, Gui.Safe.height - 300);
            Gui.DrawPanel(r);
            float y = r.y + 14;
            Gui.Text(new Rect(r.x + 20, y, 560, 44), b.Level.Name + (b.Ruined ? " (phế tích)" : ""), Gui.H2);
            y += 44;
            Gui.Text(new Rect(r.x + 20, y, 560, 30), $"{b.Def.Name} · cấp {b.Level.Tier}{(b.Level.Branch != "" ? " · nhánh " + b.Level.Branch : "")}", Gui.LabelSmall, Gui.Muted);
            y += 32;
            Gui.Text(new Rect(r.x + 20, y, 560, 80), b.Level.Description, Gui.LabelSmall);
            y += 70;
            string stats = $"Máu {Mathf.CeilToInt(b.Hp)}/{Mathf.CeilToInt(b.MaxHp)} · Giáp {b.Level.Armor:0}";
            if (b.Level.Attack != null)
            {
                var a = s.Spawner.BuildingAttack(b, false);
                stats += $"\nSát thương {a.Damage:0} · {1f / a.Interval:0.0}/s · tầm {a.Range:0.#}" + (a.Element != ElementId.None ? " · " + Elements.Name(a.Element) : "");
            }
            if (!b.Level.IncomePerDawn.IsFree) stats += "\nThu mỗi bình minh: " + Gui.Money(b.Level.IncomePerDawn.Gold, b.Level.IncomePerDawn.Mana);
            if (b.Def.Family == BuildingFamily.Barracks) stats += $"\nĐội hình: {b.Squad.Count}/{s.Spawner.SquadSize(b)} {Loc.Soldier(b.Level.Soldier)}";
            Gui.Text(new Rect(r.x + 20, y, 560, 90), stats, Gui.LabelSmall);
            y += 92;
            if (b.Ruined)
            {
                var rc = s.RebuildCost(b);
                if (Gui.Btn(new Rect(r.x + 20, y, 560, 60), "Xây lại · " + CostShort(rc, false), s.Economy.CanAfford(rc))) s.Rebuild(b);
                y += 70;
            }
            else
            {
                foreach (var next in b.Level.Next)
                {
                    var l = Db.Level(next);
                    var res = s.CanUpgrade(b, next);
                    var br = new Rect(r.x + 20, y, 560, 84);
                    if (Gui.Btn(br, "", res.Ok || res.Reason?.StartsWith("Không đủ") == true ? res.Ok : false, Gui.Button)) { if (s.Upgrade(b, next)) AudioService.Play("upgrade", 0.9f); }
                    Gui.Icon(new Rect(br.x + 10, br.y + 10, 64, 64), ArtLibrary.LevelPreviewKey(l, Db));
                    Gui.Text(new Rect(br.x + 84, br.y + 6, 470, 34), $"<b>{l.Name}</b>  {CostShort(s.CostOf(l), false)}", Gui.LabelSmall);
                    Gui.Text(new Rect(br.x + 84, br.y + 38, 470, 46), Short(l.Description, 80), new GUIStyle(Gui.LabelSmall) { fontSize = 17 });
                    y += 92;
                    if (y > r.yMax - 220) break;
                }
                // element imbue
                if (b.Level.CanImbue || b.Level.SecondarySlot)
                {
                    bool secondary = b.Primary != ElementId.None && b.Level.SecondarySlot && (b.Def.Family != BuildingFamily.Wall);
                    var cost = secondary ? ContentDatabase.ImbueSecondaryCost : ContentDatabase.ImbuePrimaryCost;
                    Gui.Text(new Rect(r.x + 20, y, 560, 32), (secondary ? "Ô nguyên tố phụ" : "Ấn nguyên tố") + " · " + CostShort(cost, false), Gui.LabelSmall);
                    y += 34;
                    float ex = r.x + 20;
                    foreach (var e in Elements.All)
                    {
                        bool can = s.CanImbue(b, e, secondary).Ok;
                        bool cur = b.Primary == e || b.Secondary == e;
                        if (Gui.IconBtn(new Rect(ex, y, 62, 62), ArtLibrary.ElementIcon(e), null, can, cur)) s.Imbue(b, e, secondary);
                        ex += 68;
                    }
                    y += 72;
                }
            }
            if (b.Def.Family != BuildingFamily.Castle && b.Lifetime < 0f)
            {
                var sv = s.SellValue(b);
                if (Gui.Btn(new Rect(r.x + 20, r.yMax - 76, 270, 60), "Bán +" + CostShort(sv, false), true, Gui.ButtonSmall, "red")) { s.Sell(b); SelectedBuilding = -1; }
            }
            if (Gui.Btn(new Rect(r.xMax - 210, r.yMax - 76, 190, 60), "Đóng", true, Gui.ButtonSmall)) SelectedBuilding = -1;
        }

        private static string Short(string s, int n) => s == null ? "" : s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        // ------------------------------------------------------------------ night HUD
        private void DrawNightHud()
        {
            var s = Session;
            var g = s.W.PlayerGeneral;
            DrawOffscreenThreats();
            // General panel (left)
            var r = new Rect(Gui.Safe.x + 16, Gui.Safe.y + 112, 560, 112);
            Gui.DrawPanel(r, true);
            GUI.DrawTexture(new Rect(r.x + 14, r.y + 12, 88, 88), ArtLibrary.Tex("portrait/" + g.General.Id), ScaleMode.ScaleToFit);
            Gui.Text(new Rect(r.x + 112, r.y + 8, 430, 40), g.General.Name + (g.State == UnitState.Downed ? $" – hồi sinh {g.DownedTimer:0}s" : ""), new GUIStyle(Gui.H2) { normal = { textColor = Gui.Light }, fontSize = 26 });
            Gui.Bar(new Rect(r.x + 112, r.y + 52, 430, 26), g.HpFraction, new Color(1f, 0.8f, 0.3f), Mathf.CeilToInt(g.Hp) + "/" + Mathf.CeilToInt(g.MaxHp));
            float ultT = 1f - Mathf.Clamp01(g.UltCooldown / s.Spawner.UltCooldown(g));
            Gui.Bar(new Rect(r.x + 112, r.y + 82, 430, 18), ultT, new Color(1f, 0.6f, 0.2f));
            // AI generals status
            float ay = r.yMax + 8;
            foreach (var ai in s.W.AIGenerals)
            {
                var ar = new Rect(r.x, ay, 420, 54);
                Gui.DrawPanel(ar, true);
                GUI.DrawTexture(new Rect(ar.x + 10, ar.y + 6, 42, 42), ArtLibrary.Tex("portrait/" + ai.General.Id), ScaleMode.ScaleToFit);
                Gui.Bar(new Rect(ar.x + 60, ar.y + 10, 160, 18), ai.HpFraction, new Color(1f, 0.8f, 0.3f));
                Gui.Text(new Rect(ar.x + 60, ar.y + 26, 350, 28), ai.State == UnitState.Downed ? "Gục ngã" : ai.AiReason, new GUIStyle(Gui.Hint) { fontSize = 17, alignment = TextAnchor.MiddleLeft });
                ay += 60;
            }
            // boss bar
            var boss = s.W.Units.FirstOrDefault(u => !u.Dead && u.IsBoss);
            if (boss != null)
            {
                var br = new Rect(Gui.VW / 2 - 400, Gui.Safe.y + 186, 800, 64);
                Gui.DrawPanel(br, true);
                string ph = boss.Boss != null ? " · " + boss.Boss.Def.Phases[boss.Boss.PhaseIndex].Name : "";
                Gui.Text(new Rect(br.x + 20, br.y + 4, 760, 30), boss.Def.Name + ph, new GUIStyle(Gui.Hint) { font = Gui.Heading, fontSize = 22 });
                Gui.Bar(new Rect(br.x + 20, br.y + 34, 760, 22), boss.HpFraction, new Color(0.85f, 0.25f, 0.3f));
            }
            // touch controls
            bool touch = Settings.TouchControls || Input.TouchMode;
            float ub = 190;
            var ultRect = new Rect(Gui.Safe.xMax - ub - 40, Gui.Safe.yMax - ub - 40, ub, ub);
            var dashRect = new Rect(ultRect.x - 130, ultRect.yMax - 120, 110, 110);
            Gui.Block(ultRect); Gui.Block(dashRect);
            GUI.DrawTexture(ultRect, ArtLibrary.Tex("ui/ult_button"), ScaleMode.ScaleToFit);
            if (g.UltCooldown > 0f)
            {
                Gui.Rect(new Rect(ultRect.x + 20, ultRect.y + 20 + (ultRect.height - 40) * ultT, ultRect.width - 40, (ultRect.height - 40) * (1 - ultT)), new Color(0, 0, 0, 0.45f));
                Gui.Text(ultRect, Mathf.CeilToInt(g.UltCooldown).ToString(), new GUIStyle(Gui.NumberBig) { alignment = TextAnchor.MiddleCenter }, Gui.Light, true);
            }
            Gui.Text(new Rect(ultRect.x - 40, ultRect.yMax - 6, ultRect.width + 80, 30), g.General.Ultimate.Name + (touch ? "" : " [Space]"), new GUIStyle(Gui.Hint) { fontSize = 18 });
            if (GUI.Button(ultRect, GUIContent.none, GUIStyle.none)) s.RequestUltimate();
            if (Gui.IconBtn(dashRect, "icon/dash", null, g.DashCooldown <= 0f)) s.RequestDash();
            if (g.DashCooldown > 0f) Gui.Text(dashRect, Mathf.CeilToInt(g.DashCooldown).ToString(), new GUIStyle(Gui.NumberBig) { alignment = TextAnchor.MiddleCenter }, Gui.Light, true);
            Input.UltButtonScreen = GuiToScreen(ultRect);
            Input.DashButtonScreen = GuiToScreen(dashRect);
            if (touch && Input.JoyActive)
            {
                var o = new Vector2(Input.JoyOrigin.x / Gui.Scale, (UnityEngine.Screen.height - Input.JoyOrigin.y) / Gui.Scale);
                float rad = Input.JoyRadius / Gui.Scale;
                GUI.DrawTexture(new Rect(o.x - rad, o.y - rad, rad * 2, rad * 2), ArtLibrary.Tex("ui/joystick_base"));
                var k = o + new Vector2(Input.JoyOffset.x, -Input.JoyOffset.y) / Gui.Scale;
                GUI.DrawTexture(new Rect(k.x - rad * 0.45f, k.y - rad * 0.45f, rad * 0.9f, rad * 0.9f), ArtLibrary.Tex("ui/joystick_knob"));
            }
            else if (touch) Gui.Text(new Rect(Gui.Safe.x + 40, Gui.Safe.yMax - 80, 600, 40), "Chạm & kéo nửa trái để di chuyển", Gui.Hint, Gui.Light, true);
        }

        private static Rect GuiToScreen(Rect r) => new Rect(r.x * Gui.Scale, UnityEngine.Screen.height - r.yMax * Gui.Scale, r.width * Gui.Scale, r.height * Gui.Scale);

        // ------------------------------------------------------------------ overlays
        private Rect Modal(float w, float h, bool dim = true)
        {
            if (dim) Gui.Rect(new Rect(0, 0, Gui.VW, Gui.VH), new Color(0.03f, 0.02f, 0.08f, 0.55f));
            var r = new Rect(Gui.VW / 2 - w / 2, Mathf.Max(Gui.Safe.y + 110, Gui.VH / 2 - h / 2), w, h);
            Gui.DrawPanel(r);
            return r;
        }

        private void DrawPause()
        {
            var r = Modal(620, 520);
            Gui.Text(new Rect(r.x, r.y + 20, r.width, 60), "Tạm dừng", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            float y = r.y + 110;
            if (Gui.Btn(new Rect(r.x + 80, y, 460, 70), "TIẾP TỤC", true, Gui.ButtonBig)) Paused = false;
            y += 84;
            if (Gui.Btn(new Rect(r.x + 80, y, 460, 64), "CÀI ĐẶT")) OpenSettingsFromRun();
            y += 78;
            if (Gui.Btn(new Rect(r.x + 80, y, 460, 64), "CẨM NANG")) { Screen = AppScreen.Codex; }
            y += 78;
            string q = Session.Phase == RunPhase.Night ? "VỀ MENU (mất tiến trình đêm)" : "LƯU & VỀ MENU";
            if (Gui.Btn(new Rect(r.x + 80, y, 460, 64), q, true, Gui.ButtonSmall, "red")) QuitToTitle();
        }

        private void DrawReport()
        {
            var s = Session;
            var rep = s.LastReport;
            var r = Modal(1000, 640);
            Gui.Text(new Rect(r.x, r.y + 20, r.width, 60), $"Bình minh sau đêm {rep.Night}", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            float y = r.y + 100, x = r.x + 60;
            void L(string t) { Gui.Text(new Rect(x, y, r.width - 120, 40), t, Gui.Label); y += 42; }
            L($"Thời gian: {rep.Duration:0}s · Hạ gục: {rep.Kills} · Lâu Đài mất: {rep.CastleDamage:0} máu");
            L($"Lính tử trận: {rep.SoldiersLost} · Công trình bị phá: {rep.BuildingsLost}");
            L("Thu nhập: " + Gui.Money(rep.Income.Gold, rep.Income.Mana));
            L("Thưởng chiến thắng: " + Gui.Money(rep.Reward.Gold, rep.Reward.Mana) + " · Tiền thưởng hạ gục: " + Gui.Money(rep.Bounty.Gold, rep.Bounty.Mana));
            if (rep.Reinforced > 0) L($"Bổ sung {rep.Reinforced} lính: -{rep.ReinforceCost} Vàng");
            if (rep.ManaDrained > 0) L($"<color=#c0392b>Bị hút {rep.ManaDrained} Mana</color>");
            foreach (var n in rep.Notes) L("<color=#c0392b>" + n + "</color>");
            var top = RunAnalytics.Top(s.W.Analytics.Reactions.ToDictionary(k => k.Key, k => k.Value), 3);
            if (top.Count > 0) L("Phản ứng nổi bật: " + string.Join(", ", top.Select(t => ReactionTable.Get(t.Key).Name + " ×" + t.Value)));
            if (s.Night < s.TotalNights || s.Setup.Endless)
            {
                var plan = s.Waves.Plan(s.Night + 1, System.Math.Max(s.TotalNights, s.Night + 1));
                L("<b>Tình báo đêm tới:</b> " + string.Join(", ", plan.Lanes.Select(l => s.LaneById(l)?.Name ?? l)) + (plan.Tags.Count > 0 ? " · " + string.Join(" · ", plan.Tags.Take(3)) : ""));
            }
            if (Gui.Btn(new Rect(r.center.x - 200, r.yMax - 96, 400, 72), "TIẾP TỤC", true, Gui.ButtonBig)) s.ContinueFromReport();
        }

        private void DrawDraft()
        {
            var s = Session;
            var r = Modal(1500, 640);
            Gui.Text(new Rect(r.x, r.y + 16, r.width, 60), "Chiếu Chỉ Hoàng Gia – chọn 1", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            float cw = 440, gap = 30;
            float x = r.center.x - (s.DecreeOffer.Count * cw + (s.DecreeOffer.Count - 1) * gap) / 2;
            string chosen = null;
            foreach (var id in s.DecreeOffer)
            {
                var d = Db.Decrees[id];
                var cr = new Rect(x, r.y + 100, cw, 400);
                GUI.Box(cr, GUIContent.none, Gui.PanelDark);
                Gui.Icon(new Rect(cr.center.x - 40, cr.y + 20, 80, 80), "icon/decree");
                Gui.Text(new Rect(cr.x + 20, cr.y + 110, cw - 40, 40), d.Name, new GUIStyle(Gui.H2) { alignment = TextAnchor.MiddleCenter, normal = { textColor = RarityColor(d.Rarity) } });
                Gui.Text(new Rect(cr.x + 20, cr.y + 150, cw - 40, 30), CategoryName(d.Category) + " · " + RarityName(d.Rarity), new GUIStyle(Gui.Hint) { fontSize = 18 });
                Gui.Text(new Rect(cr.x + 24, cr.y + 190, cw - 48, 130), d.Description, new GUIStyle(Gui.Label) { normal = { textColor = Gui.Light }, fontSize = 23 });
                string mods = string.Join("; ", d.Modifiers.Select(m => m.Describe()));
                if (mods.Length > 0) Gui.Text(new Rect(cr.x + 24, cr.y + 318, cw - 48, 70), mods, new GUIStyle(Gui.LabelSmall) { normal = { textColor = new Color(0.8f, 0.78f, 0.9f) }, fontSize = 16 });
                if (Gui.Btn(new Rect(cr.x + 60, cr.yMax + 14, cw - 120, 64), "BAN HÀNH", true, Gui.Button, d.Category == DecreeCategory.Cursed ? "purple" : null)) chosen = id;
                x += cw + gap;
            }
            if (Gui.Btn(new Rect(r.x + 30, r.y + 20, 260, 56), "Đổi " + CostShort(GameSession.RerollCost, false), s.RerollsUsed == 0 && s.Economy.CanAfford(GameSession.RerollCost), Gui.ButtonSmall)) s.RerollDraft();
            if (chosen != null) s.ChooseDecree(chosen);
        }

        public static string CategoryName(DecreeCategory c)
        {
            switch (c)
            {
                case DecreeCategory.Economy: return "Kinh tế";
                case DecreeCategory.Fortress: return "Pháo đài";
                case DecreeCategory.Army: return "Quân đội";
                case DecreeCategory.General: return "Tướng";
                case DecreeCategory.Elemental: return "Nguyên tố";
                default: return "Nguyền (đánh đổi)";
            }
        }

        public static string RarityName(Rarity r) => r == Rarity.Common ? "Thường" : r == Rarity.Rare ? "Hiếm" : "Sử thi";
        private static Color RarityColor(Rarity r) => r == Rarity.Common ? Gui.Light : r == Rarity.Rare ? new Color(0.55f, 0.8f, 1f) : new Color(1f, 0.75f, 0.35f);

        private void DrawGeneralEvolution()
        {
            var s = Session;
            var g = s.W.PlayerGeneral.General;
            var r = Modal(1300, 600);
            Gui.Text(new Rect(r.x, r.y + 16, r.width, 60), g.Name + " tiến hoá – chọn một con đường", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            for (int i = 0; i < g.Evolutions.Length; i++)
            {
                var e = g.Evolutions[i];
                var cr = new Rect(r.x + 50 + i * 610, r.y + 100, 590, 360);
                GUI.Box(cr, GUIContent.none, Gui.PanelDark);
                Gui.Text(new Rect(cr.x + 20, cr.y + 20, 550, 50), e.Name, new GUIStyle(Gui.H2) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Gui.Gold } });
                Gui.Text(new Rect(cr.x + 24, cr.y + 80, 540, 260), e.Description, new GUIStyle(Gui.Label) { normal = { textColor = Gui.Light } });
                if (Gui.Btn(new Rect(cr.x + 145, cr.yMax + 14, 300, 64), "CHỌN", true, Gui.Button)) s.ChooseGeneralEvolution(i);
            }
        }

        private void DrawKingdomEvolution()
        {
            var s = Session;
            var r = Modal(1600, 620);
            Gui.Text(new Rect(r.x, r.y + 16, r.width, 60), "Tiến Hoá Vương Quốc – định hướng cho phần còn lại của ván", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter });
            float cw = 480;
            for (int i = 0; i < s.KingdomEvolutions.Count; i++)
            {
                var k = s.KingdomEvolutions[i];
                var cr = new Rect(r.x + 50 + i * (cw + 30), r.y + 100, cw, 380);
                GUI.Box(cr, GUIContent.none, Gui.PanelDark);
                Gui.Text(new Rect(cr.x + 20, cr.y + 20, cw - 40, 50), k.Name, new GUIStyle(Gui.H2) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Gui.Gold } });
                Gui.Text(new Rect(cr.x + 24, cr.y + 80, cw - 48, 280), k.Description, new GUIStyle(Gui.Label) { normal = { textColor = Gui.Light }, fontSize = 23 });
                if (Gui.Btn(new Rect(cr.x + 90, cr.yMax + 14, cw - 180, 64), "CHỌN", true, Gui.Button)) s.ChooseKingdomEvolution(k.Id);
            }
        }

        private void DrawEnd()
        {
            var s = Session;
            bool win = s.Phase == RunPhase.Victory;
            var r = Modal(1500, 820);
            Gui.Text(new Rect(r.x, r.y + 14, r.width, 70), win ? "CHIẾN THẮNG!" : "Lâu Đài đã sụp đổ", new GUIStyle(Gui.H1) { alignment = TextAnchor.MiddleCenter, fontSize = 52, normal = { textColor = win ? new Color(0.2f, 0.5f, 0.15f) : new Color(0.7f, 0.15f, 0.1f) } });
            DrawEndAnalytics(new Rect(r.x + 40, r.y + 100, r.width - 80, r.height - 220), win);
            if (Gui.Btn(new Rect(r.center.x - 460, r.yMax - 96, 420, 72), "CHƠI LẠI", true, Gui.ButtonBig, "green")) { PickMap = s.Setup.MapId; PickGeneral = s.Setup.GeneralId; StartNewRun(); }
            if (Gui.Btn(new Rect(r.center.x + 40, r.yMax - 96, 420, 72), "VỀ MENU", true, Gui.ButtonBig)) QuitToTitle();
        }

        private void DrawEndAnalytics(Rect r, bool win)
        {
            var s = Session;
            var an = s.W.Analytics;
            int nights = win ? s.TotalNights : s.Night - 1;
            float y = r.y;
            void L(string t, float h = 38) { Gui.Text(new Rect(r.x, y, r.width / 2 - 20, h), t, Gui.LabelSmall); y += h; }
            L($"<b>{s.W.PlayerGeneral.General.Name}</b> · {s.W.Map.Name} · sống sót <b>{nights}</b> đêm · hạ <b>{an.TotalKills}</b> kẻ địch · điểm {s.ComputeScore(win)}");
            L("Mã hạt giống: <b>" + s.Setup.Seed + "</b>" + (s.Setup.Ascension > 0 ? " · Thăng Cấp " + s.Setup.Ascension : ""));
            if (s.AiGeneralIds.Any(x => x != null)) L("Tướng đồng minh: " + string.Join(", ", s.AiGeneralIds.Where(x => x != null).Select(x => Db.Generals[x].Name)));
            var fams = s.W.Buildings.Where(b => !b.Dead && b.Def.PlayerBuildable).GroupBy(b => b.Def.Name).Select(g => g.Key + " ×" + g.Count());
            L("Vương quốc: " + string.Join(", ", fams), 60);
            L("Gây sát thương nhiều nhất: " + string.Join(", ", RunAnalytics.Top(an.DamageBySource, 4).Select(k => k.Key + " " + Mathf.RoundToInt(k.Value))), 60);
            L("Phản ứng hàng đầu: " + string.Join(", ", RunAnalytics.Top(an.Reactions.ToDictionary(k => k.Key, k => k.Value), 4).Select(k => ReactionTable.Get(k.Key).Name + " ×" + k.Value)), 60);
            L("Chiếu chỉ: " + string.Join(", ", s.TakenDecrees.Select(d => Db.Decrees[d].Name)), 60);
            if (s.KingdomEvolution != null) L("Tiến hoá vương quốc: " + s.KingdomEvolutions.Find(k => k.Id == s.KingdomEvolution)?.Name);
            // right column: defeat reasons + trajectory
            float ry = r.y;
            float rx = r.x + r.width / 2 + 10;
            void R(string t, float h = 38) { Gui.Text(new Rect(rx, ry, r.width / 2 - 10, h), t, Gui.LabelSmall); ry += h; }
            if (!win)
            {
                R("<b>Vì sao thua?</b>");
                R("Đòn kết liễu: " + (s.DefeatCause ?? "?"));
                R("Nguồn gây sát thương Lâu Đài: " + string.Join(", ", RunAnalytics.Top(an.CastleDamageBy, 3).Select(k => k.Key + " " + Mathf.RoundToInt(k.Value))), 60);
                R($"Tường bị phá: {an.Breaches.Count}" + (an.Breaches.Count > 0 ? " (gần nhất ở đêm " + an.Breaches.Last().Night + ")" : ""));
                foreach (var tip in DefeatAdvice.Suggest(s)) R("• " + tip, 58);
            }
            R("<b>Vàng / Mana theo đêm</b>");
            float gx = rx, gy = ry + 4, gw = r.width / 2 - 30, gh = 120;
            Gui.Rect(new Rect(gx, gy, gw, gh), new Color(0, 0, 0, 0.12f));
            int n = an.Nights.Count;
            if (n > 0)
            {
                float maxV = Mathf.Max(1, an.Nights.Max(k => Mathf.Max(k.Gold, k.Mana)));
                for (int i = 0; i < n; i++)
                {
                    float bw = gw / n;
                    float hg = an.Nights[i].Gold / maxV * gh, hm = an.Nights[i].Mana / maxV * gh;
                    Gui.Rect(new Rect(gx + i * bw + 2, gy + gh - hg, bw * 0.45f, hg), new Color(0.95f, 0.75f, 0.3f));
                    Gui.Rect(new Rect(gx + i * bw + 2 + bw * 0.45f, gy + gh - hm, bw * 0.45f, hm), new Color(0.4f, 0.7f, 1f));
                }
            }
            if (NewAchievements.Count > 0) Gui.Text(new Rect(rx, gy + gh + 10, r.width / 2 - 10, 60), "<b>Thành tựu mới:</b> " + string.Join(", ", NewAchievements.Select(Mastery.Name)), Gui.LabelSmall);
        }

        // ------------------------------------------------------------------ world-space ghost
        private void LateUpdate()
        {
            if (Session == null || Screen != AppScreen.Run || Session.Phase != RunPhase.Day || BuildLevel == null || !HasGhost) { HideGhost(); return; }
            var def = Db.BuildingOfLevel(BuildLevel);
            var level = Db.Level(BuildLevel);
            var tiles = new List<Int2>();
            var valid = new List<bool>();
            if (WallDragging && def.IsLinear && !level.IsGate)
            {
                Session.PreviewWallLine(BuildLevel, WallStart, GhostOrigin, _wallValid, _wallInvalid);
                foreach (var t in _wallValid) { tiles.Add(t); valid.Add(true); }
                foreach (var t in _wallInvalid) { tiles.Add(t); valid.Add(false); }
            }
            else { tiles.Add(GhostOrigin); valid.Add(Session.CanPlace(BuildLevel, GhostOrigin).Ok); }
            while (_ghost.Count < tiles.Count * 2)
            {
                var go = new GameObject("ghost");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                _ghost.Add(sr);
            }
            var sprite = ArtLibrary.Get(ArtLibrary.LevelPreviewKey(level, Db));
            for (int i = 0; i < _ghost.Count; i++) _ghost[i].gameObject.SetActive(i < tiles.Count * 2);
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                var center = new Vector3(t.X + def.Size * 0.5f, t.Y + def.Size * 0.5f, 0);
                var pad = _ghost[i * 2];
                pad.sprite = ArtLibrary.Get("fx/pixel");
                pad.sortingOrder = -30900;
                pad.transform.position = center;
                pad.transform.localScale = new Vector3(def.Size * 8f, def.Size * 8f, 1);
                pad.color = valid[i] ? new Color(0.3f, 1f, 0.4f, 0.35f) : new Color(1f, 0.25f, 0.2f, 0.4f);
                var spr = _ghost[i * 2 + 1];
                spr.sprite = sprite;
                spr.sortingOrder = 31000;
                spr.transform.position = center;
                spr.color = valid[i] ? new Color(1, 1, 1, 0.75f) : new Color(1f, 0.5f, 0.5f, 0.6f);
            }
            if (_rangeRing == null)
            {
                var go = new GameObject("range");
                go.transform.SetParent(transform, false);
                _rangeRing = go.AddComponent<SpriteRenderer>();
                _rangeRing.sprite = ArtLibrary.Get("fx/ring");
                _rangeRing.sortingOrder = -30800;
            }
            float range = level.Attack != null ? level.Attack.Range + def.Size * 0.5f : level.Aura != null ? level.Aura.Radius : level.Soldier != SoldierClass.None ? 5.5f : 0f;
            _rangeRing.gameObject.SetActive(range > 0f && !WallDragging);
            if (range > 0f)
            {
                _rangeRing.transform.position = new Vector3(GhostOrigin.X + def.Size * 0.5f, GhostOrigin.Y + def.Size * 0.5f, 0);
                float d = range * 2f / 0.875f;
                _rangeRing.transform.localScale = new Vector3(d, d, 1);
                _rangeRing.color = new Color(1f, 0.95f, 0.6f, 0.75f);
            }
        }

        private void HideGhost()
        {
            foreach (var g in _ghost) if (g.gameObject.activeSelf) g.gameObject.SetActive(false);
            if (_rangeRing != null && _rangeRing.gameObject.activeSelf) _rangeRing.gameObject.SetActive(false);
        }
    }
}
