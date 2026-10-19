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
            Rig.Size = 9f;
            Screen = AppScreen.Run;
            Paused = false;
            Speed = 1;
            _acc = 0f;
            ClearBuildMode();
            SelectedBuilding = -1;
            AudioService.Music("music_day");
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

            var g = Session.W.PlayerGeneral;
            bool follow = Session.Phase == RunPhase.Night && g != null;
            Vector2 look = g != null ? WorldView.V(g.Velocity) * 0.25f : Vector2.zero;
            Rig.ShakeEnabled = Settings.ScreenShake;
            Rig.Update(dt, follow, g != null ? WorldView.V(g.Position) : Rig.Focus, look, Input);
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
                    AudioService.Play("horn", 1f, 1, 1f);
                    AudioService.Music(Session.Plan.BossUnitId != null ? "music_boss" : "music_night");
                    ShowBanner($"Đêm {Session.Night}" + (Session.Plan.BossUnitId != null ? " – BOSS!" : ""));
                    if (!Profile.TutorialDone && Session.Night == 1) ShowToast("WASD / cần điều khiển để di chuyển Tướng. Space / nút vàng: Tối thượng.");
                    break;
                case RunPhase.Report:
                    AudioService.Play("victory_small", 1f, 1, 1f);
                    AudioService.Music("music_day");
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
                    AudioService.Play(win ? "victory" : "defeat", 1f, 1, 1f);
                    AudioService.Music(win ? "music_day" : "music_title");
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
            if (Input.StartNightPressed && BuildLevel == null) { StartNightAction(); return; }
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
                        if (n > 0) { AudioService.Play("build", 0.9f); ShowToast($"Đã xây {n} đoạn tường."); }
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
                AudioService.Play("build", 0.9f);
                View.Vfx.Smoke(WorldView.V(b.Center), 6);
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
            if (n > 0) { AudioService.Play("build", 0.9f); ShowToast($"Đã xây {n} đoạn tường."); }
        }

        public void StartNightAction()
        {
            ClearBuildMode();
            SelectedBuilding = -1;
            Paused = false;
            Session.StartNight();
        }

        // ------------------------------------------------------------------ events -> presentation
        private void ProcessEvents()
        {
            var w = Session.W;
            var vfx = View.Vfx;
            for (int i = 0; i < w.Events.Count; i++)
            {
                var e = w.Events[i];
                var at = WorldView.V(e.Position);
                switch (e.Type)
                {
                    case SimEventType.Hit:
                    {
                        if (e.Value <= 0f) { if (e.Text == "block") vfx.Burst(at, new Color(0.8f, 0.9f, 1f), 4, 1.5f, 0.25f); break; }
                        var target = w.Get(e.ActorId);
                        bool enemy = target != null && target.Team == Team.Enemy;
                        var uv = View.UnitViewOf(e.ActorId);
                        if (uv != null) uv.Flash = 0.1f;
                        var bv = View.BuildingViewOf(e.ActorId);
                        if (bv != null) bv.Flash = 0.12f;
                        if (Settings.DamageNumbers && (enemy || e.Value >= 8f))
                            vfx.Number(at, e.ActorId, e.Value, enemy ? (e.Element == ElementId.None ? new Color(1f, 0.97f, 0.9f) : ArtLibrary.ElementColor(e.Element)) : new Color(1f, 0.45f, 0.4f), enemy ? 24f : 22f);
                        if (e.Element != ElementId.None && Settings.VfxLevel >= 1 && UnityEngine.Random.value < 0.35f) vfx.Element(at, e.Element, 0.6f);
                        if (target == w.Castle) { Rig.Shake(0.15f); AudioService.Play("castle_hit", 0.6f, 2, 0.3f); }
                        else AudioService.Play("hit", 0.35f, 4, 0.05f);
                        break;
                    }
                    case SimEventType.Kill:
                        vfx.Smoke(at, 4);
                        vfx.Burst(at, new Color(1f, 0.9f, 0.6f), 5, 2f, 0.25f);
                        AudioService.Play("death", 0.5f, 3, 0.05f);
                        break;
                    case SimEventType.Reaction:
                        if (e.Text == "arc" || e.Text == "chain")
                        {
                            vfx.Element(at, e.Element == ElementId.None ? ElementId.Lightning : e.Element, 0.7f);
                            AudioService.Play("zap", 0.4f, 3, 0.05f);
                            break;
                        }
                        vfx.Ring(at + new Vector2(0, 0.3f), ArtLibrary.ElementColor(e.Element), 1.2f, 0.5f);
                        vfx.Element(at, e.Element, 1.2f);
                        vfx.Label(at, e.Text, ArtLibrary.ElementColor(e.Element), 30f, 1.1f);
                        AudioService.Play("reaction", 0.7f, 3, 0.08f);
                        break;
                    case SimEventType.WallBreached:
                        vfx.Smoke(at, 10, new Color(0.7f, 0.65f, 0.6f, 0.8f));
                        Rig.Shake(0.5f);
                        vfx.Label(at, "Tường bị phá!", new Color(1f, 0.5f, 0.3f), 30f, 1.6f);
                        AudioService.Play("breach", 1f, 2, 0.2f);
                        break;
                    case SimEventType.BuildingDestroyed:
                        vfx.Smoke(at, 12, new Color(0.6f, 0.55f, 0.55f, 0.85f));
                        Rig.Shake(0.4f);
                        AudioService.Play("breach", 0.9f, 2, 0.2f);
                        break;
                    case SimEventType.UltimateCast:
                        vfx.Ring(at, ArtLibrary.ElementColor(e.Element), e.Value, 0.6f, "fx/ring_thick");
                        vfx.Element(at, e.Element, 2f);
                        vfx.Label(at, e.Text, Gui.Gold, 34f, 1.4f);
                        Rig.Shake(0.35f);
                        AudioService.Play("ultimate", 1f, 2, 0.2f);
                        break;
                    case SimEventType.Dash:
                        vfx.Smoke(at, 3, new Color(0.9f, 0.95f, 1f, 0.6f));
                        AudioService.Play("dash", 0.6f, 2, 0.1f);
                        break;
                    case SimEventType.MeleeSwing:
                    {
                        var a = w.Get(e.ActorId) as Unit;
                        if (a != null && Settings.VfxLevel >= 1) vfx.Slash(at, a.FacingX, e.Element == ElementId.None ? new Color(1, 1, 1, 0.8f) : ArtLibrary.ElementColor(e.Element, 0.9f));
                        AudioService.Play("swing", 0.25f, 4, 0.04f);
                        break;
                    }
                    case SimEventType.ProjectileFired:
                        AudioService.Play(e.Element == ElementId.None ? "arrow" : "magic", 0.25f, 3, 0.05f);
                        break;
                    case SimEventType.Freeze:
                        vfx.Element(at, ElementId.Ice, 1.2f);
                        break;
                    case SimEventType.Heal:
                        if (Settings.DamageNumbers && e.Value >= 6f) vfx.Number(at, -1, e.Value, new Color(0.5f, 1f, 0.5f), 22f);
                        break;
                    case SimEventType.Summon:
                        vfx.Ring(at, new Color(0.7f, 0.4f, 1f), 1.4f, 0.6f);
                        vfx.Burst(at, new Color(0.7f, 0.4f, 1f), 10, 2f, 0.3f, "fx/smoke");
                        AudioService.Play("summon", 0.7f, 2, 0.2f);
                        break;
                    case SimEventType.BossPhase:
                    case SimEventType.Announcement:
                        ShowBanner(e.Text);
                        Rig.Shake(0.4f);
                        AudioService.Play("horn", 0.8f, 1, 1f);
                        break;
                    case SimEventType.GeneralDowned:
                        ShowBanner(e.Text + " gục ngã! Hồi sinh sau 12 giây.");
                        break;
                    case SimEventType.Building:
                    case SimEventType.Upgrade:
                        vfx.Smoke(at, 5);
                        vfx.Burst(at, Gui.Gold, 8, 2f, 0.3f);
                        break;
                    case SimEventType.Telegraph:
                        AudioService.Play("warning", 0.6f, 2, 0.3f);
                        if (!string.IsNullOrEmpty(e.Text) && e.ActorId >= 0) vfx.Label(at, e.Text, new Color(1f, 0.4f, 0.3f), 26f, 1f);
                        break;
                    case SimEventType.Hazard:
                        if (e.Text == "lightning")
                        {
                            vfx.Element(at, ElementId.Lightning, 1.6f);
                            vfx.Ring(at, ArtLibrary.ElementColor(ElementId.Lightning), e.Value, 0.4f);
                            Rig.Shake(0.25f);
                            AudioService.Play("zap", 0.9f, 2, 0.1f);
                        }
                        else if (e.Text == "vent")
                        {
                            vfx.Element(at, ElementId.Fire, 1.6f);
                            vfx.Smoke(at, 8, new Color(0.35f, 0.3f, 0.3f, 0.8f));
                            AudioService.Play("breach", 0.6f, 2, 0.15f);
                        }
                        else if (e.Text == "ice_crack")
                        {
                            vfx.Burst(at, new Color(0.8f, 0.95f, 1f), 10, 2.2f, 0.3f);
                            vfx.Label(at, "Băng vỡ!", new Color(0.7f, 0.9f, 1f), 24f, 1f);
                            AudioService.Play("breach", 0.5f, 2, 0.15f);
                        }
                        break;
                    case SimEventType.Explosion:
                        vfx.Ring(at, ArtLibrary.ElementColor(e.Element == ElementId.None ? ElementId.Fire : e.Element), Mathf.Max(1f, e.Value), 0.45f, "fx/ring_thick");
                        vfx.Element(at, ElementId.Fire, 1.4f);
                        vfx.Smoke(at, 6);
                        Rig.Shake(0.2f);
                        AudioService.Play("breach", 0.7f, 3, 0.1f);
                        break;
                    case SimEventType.Stealth:
                        vfx.Burst(at, new Color(0.7f, 0.5f, 1f), 6, 1.5f, 0.3f, "fx/smoke");
                        break;
                    case SimEventType.StatusApplied:
                        if (e.Text == "exhausted") vfx.Label(at, "Kiệt sức! (+50% sát thương)", new Color(1f, 0.9f, 0.4f), 26f, 1.4f);
                        else if (e.Text == "possessed") vfx.Label(at, "Tháp bị khống chế!", new Color(0.8f, 0.5f, 1f), 26f, 1.4f);
                        break;
                }
            }
            w.Events.Clear();
            if (ToastTime > 0) ToastTime -= Time.unscaledDeltaTime;
            if (BannerTime > 0) BannerTime -= Time.unscaledDeltaTime;
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
