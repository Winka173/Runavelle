using System;
using System.Collections.Generic;
using System.IO;
using Runavelle.Core;
using UnityEngine;

namespace Runavelle.Game
{
    /// <summary>Creates the game from code when any scene starts (no scene/prefab GUID references needed).</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameRoot>() != null) return;
            var go = new GameObject("Runavelle");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }
    }

    public enum AppScreen { Title, GeneralSelect, MapSelect, Run, Settings, History, Codex }

    /// <summary>App flow, fixed-step simulation driver, input routing and event presentation.</summary>
    public sealed partial class GameRoot : MonoBehaviour
    {
        public ContentDatabase Db;
        public Profile Profile;
        public readonly GameSettings Settings = new GameSettings();
        public readonly InputAdapter Input = new InputAdapter();
        public CameraRig Rig;
        public GameSession Session;
        public WorldView View;
        public AppScreen Screen = AppScreen.Title;
        private AppScreen _settingsReturn = AppScreen.Title;

        private float _acc;
        public int Speed = 1;
        public bool Paused;
        private string ProfilePath => Path.Combine(Application.persistentDataPath, "profile.json");
        private string RunPath => Path.Combine(Application.persistentDataPath, "run.json");
        public bool HasSavedRun;

        // run setup (menu selections)
        public string PickGeneral = "roland";
        public string PickMap = "moonstone_valley";
        public int PickAscension;
        public bool PickEndless;
        public string PickChallenge;
        public string PickWeapon, PickRelic;

        // build / selection state
        public string BuildLevel;
        public int SelectedBuilding = -1;
        public Int2 GhostOrigin;
        public bool GhostValid;
        public bool HasGhost;
        public bool WallDragging;
        public Int2 WallStart;
        public string BuildFamilyMenu;
        public string Toast;
        public float ToastTime;
        public string Banner;
        public float BannerTime;
        public readonly List<string> NewAchievements = new List<string>();
        public readonly MusicDirector Music = new MusicDirector();
        public readonly BarkDirector Barks = new BarkDirector();
        public HighlightRecorder Highlights;
        public MilestoneTracker Milestones;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            UnityEngine.Screen.orientation = ScreenOrientation.AutoRotation;
            UnityEngine.Screen.autorotateToPortrait = false;
            UnityEngine.Screen.autorotateToPortraitUpsideDown = false;
            UnityEngine.Screen.autorotateToLandscapeLeft = true;
            UnityEngine.Screen.autorotateToLandscapeRight = true;
            Db = ContentDatabase.Create(); // throws if content breaks a design lock (startup assertion)
            LoadProfile();
            Settings.Load(Profile);
            Settings.Apply();
            AudioService.Init(gameObject, Settings);
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.16f, 0.14f, 0.22f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            foreach (var other in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (other != cam) other.enabled = false;
            Rig = new CameraRig { Cam = cam };
            HasSavedRun = File.Exists(RunPath) || File.Exists(RunPath + ".bak");
            var unlocked = Profile.UnlockedGenerals(Db);
            if (!unlocked.Contains(PickGeneral) && unlocked.Count > 0) PickGeneral = unlocked[0];
            AudioService.Music("music_title");
        }

        // ------------------------------------------------------------------ profile / saves
        private void LoadProfile()
        {
            try { Profile = Profile.Deserialize(SaveStore.Read(ProfilePath, out _)); }
            catch (Exception e) { Debug.LogWarning("Profile load failed, starting fresh: " + e.Message); Profile = new Profile(); }
        }

        public void SaveProfile()
        {
            Settings.Save(Profile);
            try { SaveStore.Write(ProfilePath, Profile.Serialize()); } catch (Exception e) { Debug.LogWarning("Profile save failed: " + e.Message); }
        }

        private void SaveRun()
        {
            if (Session == null || !RunSave.CanSave(Session)) return;
            try { SaveStore.Write(RunPath, RunSave.Serialize(Session)); HasSavedRun = true; }
            catch (Exception e) { Debug.LogWarning("Run save failed: " + e.Message); }
        }

        public void DeleteRunSave()
        {
            SaveStore.Delete(RunPath);
            HasSavedRun = false;
        }

        public void StartNewRun(bool daily = false)
        {
            ulong seed = daily ? DailySeed() : (ulong)DateTime.UtcNow.Ticks;
            var setup = new RunSetup { MapId = PickMap, GeneralId = PickGeneral, Seed = seed, Ascension = PickAscension, Endless = PickEndless, IsDaily = daily,
                ChallengeId = PickChallenge, WeaponId = PickWeapon, RelicId = PickRelic };
            setup.UnlockedGenerals.AddRange(Profile.UnlockedGenerals(Db));
            ApplyCosmetics();
            BeginSession(new GameSession(Db, setup));
            Profile.RunsPlayed++;
            if (!Profile.TutorialDone) ShowBanner("Ngày 1: Xây Nhà Dân (Vàng) và Giếng Mana (Mana) gần Lâu Đài.");
        }

        public static ulong DailySeed()
        {
            var d = DateTime.UtcNow.Date;
            return (ulong)(d.Year * 10000 + d.Month * 100 + d.Day) * 2654435761UL;
        }

        public void ContinueRun()
        {
            try
            {
                var json = SaveStore.Read(RunPath, out bool backup);
                if (json == null) { ShowToast("Không đọc được bản lưu."); DeleteRunSave(); return; }
                ApplyCosmetics();
                BeginSession(RunSave.Deserialize(Db, json));
                if (backup) ShowToast("Đã khôi phục từ bản sao lưu.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Continue failed: " + e);
                ShowToast("Bản lưu hỏng - đã xoá.");
                DeleteRunSave();
            }
        }

        private void BeginSession(GameSession s)
        {
            View?.Destroy();
            Session = s;
            View = new WorldView(s, transform);
            Rig.MapSize = new Vector2(s.W.Map.Width, s.W.Map.Height);
            Rig.Focus = WorldView.V(s.W.Castle.Center);
            Rig.Size = Settings.CameraZoom;
            Screen = AppScreen.Run;
            Paused = false;
            Speed = 1;
            _acc = 0f;
            ClearBuildMode();
            SelectedBuilding = -1;
            Highlights = new HighlightRecorder();
            Milestones = new MilestoneTracker();
            Milestones.Prime(s);
            Barks.ResetClock();
            _dusk = 0f;
            View.Dusk = 0f;
            View.RebuildAftermath();
        }

        public void QuitToTitle()
        {
            SaveRun();
            View?.Destroy();
            View = null;
            Session = null;
            Screen = AppScreen.Title;
            AudioService.Music("music_title");
        }

        // ------------------------------------------------------------------ frame
        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            bool night = Session != null && Session.Phase == RunPhase.Night && Screen == AppScreen.Run;
            Input.Bindings = Settings.Keys;
            Input.Update(night, Settings.TouchControls);
            if (Screen != AppScreen.Run || Session == null) { Rig.Cam.backgroundColor = new Color(0.16f, 0.14f, 0.22f); return; }

            if (Input.PausePressed && (Session.Phase == RunPhase.Night || Session.Phase == RunPhase.Day)) Paused = !Paused;
            if (!Paused)
            {
                if (Session.Phase == RunPhase.Day) DayInput();
                else if (Session.Phase == RunPhase.Night) NightInput();
                // fixed-step simulation: tick count depends only on time * speed, never on frame rate
                float step = World.Dt;
                float speed = Session.Phase == RunPhase.Night ? Speed * (Settings.SlowNights ? 0.75f : 1f) : (Settings.FastDay ? 2f : 1f);
                _acc += dt * speed;
                int guard = 0;
                while (_acc >= step && guard++ < 8)
                {
                    Session.Tick();
                    _acc -= step;
                    if (Session.Phase != RunPhase.Night && Session.Phase != RunPhase.Day) { _acc = 0; break; }
                }
                if (guard >= 8) _acc = 0f;
            }
            View.Alpha = Mathf.Clamp01(_acc / World.Dt);
            ProcessEvents();
            HandlePhaseTransitions();
            TickDusk(Paused ? 0f : dt);
            TickPresentationDirectors(dt);

            var g = Session.W.PlayerGeneral;
            bool follow = Session.Phase == RunPhase.Night && g != null;
            Vector2 look = g != null ? WorldView.V(g.Velocity) * 0.25f : Vector2.zero;
            Rig.ShakeAgg.Strength = Settings.ScreenShake ? Settings.ShakeAmount : 0f;
            Rig.ReducedMotion = Settings.ReducedMotion;
            if (Peeking(out var peekAt)) Rig.Update(dt, true, peekAt, Vector2.zero, Input); // player-requested look; ends on any move input
            else Rig.Update(dt, follow, g != null ? WorldView.V(g.Position) : Rig.Focus, look, Input);
            View.Sync(Paused ? 0f : dt, Settings, Session.Phase == RunPhase.Day);
            Rig.Cam.backgroundColor = Color.Lerp(new Color(0.24f, 0.36f, 0.22f), new Color(0.07f, 0.07f, 0.14f), Session.W.IsNight ? 1f : 0f);
        }

        private RunPhase _lastPhase = RunPhase.Day;

        private void HandlePhaseTransitions()
        {
            if (Session.Phase == _lastPhase) return;
            var prev = _lastPhase;
            _lastPhase = Session.Phase;
            switch (Session.Phase)
            {
                case RunPhase.Night:
                    AudioService.Play("horn", 1f, 1, 1f, category: SoundCategory.Stinger);
                    Caption("[Tù và chiến tranh – đêm bắt đầu]");
                    ShowBanner($"Đêm {Session.Night}" + (Session.Plan.BossUnitId != null ? " – BOSS!" : ""));
                    Barks.ResetClock();
                    if (Session.W.AIGenerals.Count > 0) Barks.Say(BarkTrigger.NightStart, Session.W.AIGenerals[0], 0.5f);
                    if (!Profile.TutorialDone && Session.Night == 1) ShowToast("WASD / cần điều khiển để di chuyển Tướng. Space / nút vàng: Tối thượng.");
                    break;
                case RunPhase.Report:
                    DawnCelebration();
                    break;
                case RunPhase.Day:
                    if (Session.AutosaveRequested) { Session.AutosaveRequested = false; SaveRun(); }
                    if (!Profile.TutorialDone) TutorialHint();
                    FeatureHints();
                    break;
                case RunPhase.DecreeDraft:
                case RunPhase.GeneralEvolution:
                case RunPhase.KingdomEvolution:
                    SaveRun();
                    break;
                case RunPhase.Victory:
                case RunPhase.Defeat:
                    bool win = Session.Phase == RunPhase.Victory;
                    NewAchievements.Clear();
                    NewAchievements.AddRange(Profile.RecordRun(Session, win));
                    if (Session.Night >= 3) Profile.TutorialDone = true;
                    SaveProfile();
                    DeleteRunSave();
                    AudioService.Play(win ? "victory" : "defeat", 1f, 1, 1f, category: SoundCategory.Stinger);
                    if (win) foreach (var u in Session.W.Units) if (!u.Dead && u.Team == Team.Player) View.UnitViewOf(u.Id)?.SetCheer(2f);
                    break;
            }
        }

        private void TutorialHint()
        {
            switch (Session.Night)
            {
                case 2: ShowBanner("Mẹo: Tường + Cổng chặn đường; Tháp Cung tự bắn. Kéo chuột để vẽ tường."); break;
                case 3: ShowBanner("Mẹo: Doanh Trại huấn luyện lính tự động bảo vệ khu vực quanh nó."); break;
                case 4: ShowBanner("Mẹo: Nước + Sét = Điện Giật. Kết hợp nguyên tố của tháp, lính và tướng!"); break;
                case 5: ShowBanner("Đêm 5 có BOSS. Xem dự báo bên phải: hướng tấn công và cảnh báo."); break;
                case 6: ShowBanner("Mẹo: Bóng Ma tàng hình chỉ lộ diện gần lính/tháp hoặc khi trúng Ánh Sáng. Ánh Sáng + Bóng Tối = Nhật Thực."); break;
            }
        }

        public void ShowToast(string s) { Toast = s; ToastTime = 3.5f; }
        public void ShowBanner(string s) { Banner = s; BannerTime = 4f; }

        // ------------------------------------------------------------------ input per phase
        private void NightInput()
        {
            var mv = Input.Move;
            Session.SetMoveInput(new Vec2(mv.x, mv.y));
            if (Input.UltPressed || Input.UltButtonTouched) Session.RequestUltimate();
            if (Input.DashPressed || Input.DashButtonTouched) Session.RequestDash();
            if (Input.SpeedPressed) Speed = Speed == 1 ? 2 : 1;
        }

        public void ClearBuildMode()
        {
            BuildLevel = null;
            HasGhost = false;
            WallDragging = false;
            BuildFamilyMenu = null;
        }

        private Int2 PointerTile()
        {
            var w = Rig.ScreenToWorld(Input.PointerScreen);
            return new Int2(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y));
        }

        private void DayInput()
        {
            if (Input.UndoPressed) { if (Session.UndoLast()) ShowToast("Đã hoàn tác."); }
            if (Input.StartNightPressed && BuildLevel == null) { if (InDusk) SkipDusk(); else StartNightAction(); return; }
            if (Input.CancelPressed) { ClearBuildMode(); SelectedBuilding = -1; }
            if (BuildLevel != null)
            {
                var def = Db.BuildingOfLevel(BuildLevel);
                var tile = PointerTile();
                if (!Input.TouchMode || Input.PointerDown)
                {
                    if (!Input.PointerOverUi || HasGhost)
                    {
                        if (!Input.PointerOverUi)
                        {
                            GhostOrigin = new Int2(tile.X - (def.Size - 1) / 2, tile.Y - (def.Size - 1) / 2);
                            HasGhost = true;
                        }
                    }
                }
                if (Input.PointerOverUi) return;
                GhostValid = Session.CanPlace(BuildLevel, GhostOrigin).Ok;
                if (def.IsLinear && !Db.Level(BuildLevel).IsGate)
                {
                    if (Input.TouchMode)
                    {
                        if (Input.PointerDown) { if (!WallDragging) { WallDragging = true; WallStart = GhostOrigin; } }
                        return; // confirm button in HUD places the line
                    }
                    if (Input.PointerDown) { WallDragging = true; WallStart = GhostOrigin; }
                    if (Input.PointerUp && WallDragging)
                    {
                        int n = Session.PlaceWallLine(BuildLevel, WallStart, GhostOrigin);
                        WallDragging = false;
                        if (n > 0) { ShowToast($"Đã xây {n} đoạn tường."); }
                        else ShowToast(Session.CanPlace(BuildLevel, GhostOrigin).Reason ?? "Không xây được");
                    }
                    return;
                }
                if (!Input.TouchMode && Input.PointerDown) TryPlaceGhost();
                return;
            }
            if (Input.PointerDown && !Input.PointerOverUi)
            {
                var t = PointerTile();
                var b = Session.W.BuildingAt(t.X, t.Y);
                SelectedBuilding = b != null ? b.Id : -1;
                if (b != null) AudioService.Ui();
            }
        }

        public void TryPlaceGhost()
        {
            if (BuildLevel == null || !HasGhost) return;
            var r = Session.CanPlace(BuildLevel, GhostOrigin);
            if (!r.Ok) { ShowToast(r.Reason); AudioService.Play("error", 0.7f); return; }
            var b = Session.Place(BuildLevel, GhostOrigin);
            if (b != null)
            {
                if (!Input.TouchMode && (UnityEngine.Input.GetKey(KeyCode.LeftShift))) return; // shift: keep placing
                ClearBuildMode();
                SelectedBuilding = b.Id;
            }
        }

        public void ConfirmWallLine()
        {
            if (!WallDragging || BuildLevel == null) return;
            int n = Session.PlaceWallLine(BuildLevel, WallStart, GhostOrigin);
            WallDragging = false;
            if (n > 0) { ShowToast($"Đã xây {n} đoạn tường."); }
        }

        // ------------------------------------------------------------------ dusk → night
        private float _dusk;
        public bool InDusk => _dusk > 0f;
        private float DuskDuration => Settings.ReducedMotion ? 0.6f : 2.2f;

        /// <summary>Dusk: short sky/music/bell transition, then the night starts. Day simulation keeps running (state is identical).</summary>
        public void StartNightAction()
        {
            if (InDusk || Session.Phase != RunPhase.Day) return;
            ClearBuildMode();
            SelectedBuilding = -1;
            Paused = false;
            _dusk = DuskDuration;
            Session.BeginRally(); // squads march to their posts during the bell (autonomous; no troop micro)
            AudioService.Play("dusk_bell", 0.9f, 1, 1f, category: SoundCategory.Stinger);
            AudioService.Play("horn", 0.55f, 1, 0.4f, category: SoundCategory.Stinger);
            Caption("[Chuông hoàng hôn – kèn tập hợp]");
        }

        /// <summary>Skip the dusk choreography: the night starts now (posts are identical either way).</summary>
        public void SkipDusk()
        {
            if (InDusk) _dusk = Mathf.Min(_dusk, 0.01f);
        }

        private void TickDusk(float dt)
        {
            if (_dusk <= 0f) { View.Dusk = Mathf.MoveTowards(View.Dusk, 0f, dt); return; }
            _dusk -= dt;
            View.Dusk = 1f - Mathf.Clamp01(_dusk / DuskDuration);
            if (_dusk <= 0f && Session.Phase == RunPhase.Day) { Session.StartNight(); View.Dusk = 0f; }
        }

        /// <summary>Dawn after a won night: relief beat, gold sparkle at the castle, aftermath reset (repairs restore walls).</summary>
        private void DawnCelebration()
        {
            AudioService.Play("dawn_chime", 1f, 1, 1f, category: SoundCategory.Stinger);
            var c = WorldView.V(Session.W.Castle.Center);
            View.Vfx.Element(c, ElementId.Light, 2f);
            View.Vfx.Burst(c, Gui.Gold, 16, 3f, 0.35f);
            foreach (var u in Session.W.Units) if (!u.Dead && u.Team == Team.Player) View.UnitViewOf(u.Id)?.SetCheer(1.2f);
            View.RebuildAftermath();
        }

        private float _milestoneCheck;

        private void TickPresentationDirectors(float dt)
        {
            var w = Session.W;
            // music intensity
            bool boss = false; int near = 0;
            foreach (var u in w.Units)
            {
                if (u.Dead || u.Team != Team.Enemy) continue;
                if (u.IsBoss) boss = true;
                if (w.Castle != null && Core.Vec2.Distance(u.Position, w.Castle.Center) < 7f) near++;
            }
            var st = Music.Update(dt, Session.Phase, InDusk, boss, w.Castle != null ? w.Castle.HpFraction : 1f, near);
            AudioService.Music(MusicDirector.Track(st));
            // barks + milestones
            Barks.Enabled = Settings.Captions || Settings.Voice > 0f;
            Barks.Update(Session.NightTime);
            _milestoneCheck -= dt;
            if (_milestoneCheck <= 0f) { _milestoneCheck = 1f; Milestones.Check(Session); }
            if (Milestones.TryPop(out var m))
            {
                ShowBanner("Cột mốc: " + m.Text);
                AudioService.Play("milestone", 0.9f, 1, 1f, category: SoundCategory.Stinger);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause) return;
            if (Session != null && Session.Phase == RunPhase.Night) Paused = true;
            SaveRun();
            SaveProfile();
        }

        private void OnApplicationQuit()
        {
            SaveRun();
            SaveProfile();
        }
    }
}
