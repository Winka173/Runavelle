using System;
using System.IO;
using System.Linq;
using Runavelle.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Runavelle.EditorTools
{
    /// <summary>Import rules for generated art/audio (applied automatically on first import).</summary>
    public sealed class RunavelleAssetImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("Runavelle/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
            ti.isReadable = false;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.Replace('\\', '/').Contains("Runavelle/Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            bool music = Path.GetFileName(assetPath).StartsWith("music_");
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.6f : 0.75f;
            ai.defaultSampleSettings = s;
        }
    }

    /// <summary>Menu "Runavelle": project setup, checks and player builds (also callable in batchmode).</summary>
    public static class RunavelleMenu
    {
        public const string ScenePath = "Assets/Runavelle/Scenes/Main.unity";

        [MenuItem("Runavelle/1. Setup project (scene, build settings, player settings)")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/Runavelle/Scenes");
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Runavelle";
            PlayerSettings.productName = "Runavelle";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.runavelle.game");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.runavelle.game");
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[Runavelle] Project setup complete: " + ScenePath);
        }

        [MenuItem("Runavelle/2. Run checks (content + 3-night simulation smoke)")]
        public static void RunChecks()
        {
            var db = ContentDatabase.Create(); // throws on any design-lock / reference violation
            int missing = 0;
            var manifest = Resources.Load<TextAsset>("Art/manifest");
            if (manifest == null) throw new Exception("Art manifest missing. Run: python3 Tools/Art/build_art.py");
            var keys = ((System.Collections.Generic.Dictionary<string, object>)MiniJson.Parse(manifest.text)).Keys.ToList();
            foreach (var k in keys) if (Resources.Load<Texture2D>("Art/" + k) == null) { missing++; Debug.LogError("Missing art " + k); }
            foreach (var mapId in db.MapOrder)
            {
                var s = new GameSession(db, new RunSetup { MapId = mapId, GeneralId = "roland", Seed = 1 });
                var bot = new AutoPlayer(s, AutoPlayer.Strategy.Balanced, 1);
                for (int n = 0; n < 3 && s.Phase != RunPhase.Defeat; n++)
                {
                    while (s.Phase != RunPhase.Day && s.Phase != RunPhase.Defeat) bot.HandleChoices();
                    if (s.Phase == RunPhase.Defeat) break;
                    bot.PlayDay();
                    s.StartNight();
                    int guard = 0;
                    while (s.Phase == RunPhase.Night && guard++ < 20 * 600) { bot.PlayNightTick(); s.Tick(); }
                }
                Debug.Log($"[Runavelle] smoke {mapId}: night {s.Night} phase {s.Phase} kills {s.W.Analytics.TotalKills}");
            }
            Debug.Log($"[Runavelle] Checks finished. Art sprites: {keys.Count}, missing: {missing}.");
            if (missing > 0) throw new Exception(missing + " art files missing");
        }

        [MenuItem("Runavelle/3. Build Windows (x64)")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Runavelle.exe");

        [MenuItem("Runavelle/4. Build Android (APK, ARM64)")]
        public static void BuildAndroid()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Build(BuildTarget.Android, "Builds/Android/Runavelle.apk");
        }

        [MenuItem("Runavelle/5. Build iOS (Xcode project, requires macOS)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, "Builds/iOS");

        private static void Build(BuildTarget target, string path)
        {
            SetupProject();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = target, options = BuildOptions.None });
            Debug.Log($"[Runavelle] Build {target}: {report.summary.result} -> {path} ({report.summary.totalSize / (1024 * 1024)} MB)");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Batchmode entry: Unity -batchmode -quit -executeMethod Runavelle.EditorTools.RunavelleMenu.CI</summary>
        public static void CI()
        {
            SetupProject();
            RunChecks();
        }
    }
}
