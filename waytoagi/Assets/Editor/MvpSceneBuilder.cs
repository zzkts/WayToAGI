using System;
using System.IO;
using Pachinko;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PachinkoEditor
{
    public static class MvpSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string BuildDirectory = "Builds/Windows";
        public const string BuildName = "PachinkoMVP.exe";

        [MenuItem("Pachinko/1. 生成 MVP 场景", priority = 1)]
        public static void BuildScene()
        {
            EnsureFolders();
            SyncConfig();

            if (!Application.isBatchMode)
            {
                bool proceed = EditorUtility.DisplayDialog("生成 MVP 场景",
                    "将新建并保存 Assets/Scenes/Main.unity，并把场景中的 GameBootstrap 作为启动入口。\n\n继续？",
                    "生成", "取消");
                if (!proceed) return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BUILD] MVP 场景已生成：" + ScenePath);
        }

        [MenuItem("Pachinko/2. 运行数值自检", priority = 2)]
        public static void RunSelfTestMenu()
        {
            MvpSelfTest.RunAll(false);
        }

        [MenuItem("Pachinko/3. 构建 Windows 试玩包", priority = 3)]
        public static void BuildWindows()
        {
            BuildScene();
            EnsureFolders();
            string output = Path.GetFullPath(Path.Combine(BuildDirectory, BuildName));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? BuildDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Windows 构建失败：{report.summary.result}，错误 {report.summary.totalErrors}");
            }
            Debug.Log($"[BUILD] Windows 试玩包已输出：{output}，大小 {report.summary.totalSize / (1024f * 1024f):F1} MB");
        }

        public static void RunBuildSceneBatch()
        {
            try
            {
                BuildScene();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void RunBuildWindowsBatch()
        {
            try
            {
                BuildWindows();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void RunSelfTestBatch()
        {
            bool ok = MvpSelfTest.RunAll(true);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Config")) AssetDatabase.CreateFolder("Assets/Resources", "Config");
            if (!Directory.Exists(BuildDirectory)) Directory.CreateDirectory(BuildDirectory);
        }

        private static void SyncConfig()
        {
            string streaming = "Assets/StreamingAssets/game_config.json";
            string resources = "Assets/Resources/Config/game_config.json";
            if (!File.Exists(streaming))
            {
                throw new FileNotFoundException("缺少主配置文件 Assets/StreamingAssets/game_config.json");
            }
            File.Copy(streaming, resources, true);
            AssetDatabase.ImportAsset(resources, ImportAssetOptions.ForceUpdate);
            Debug.Log("[BUILD] 数值配置已同步到 Resources 兜底路径。");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "WaytoAGI";
            PlayerSettings.productName = "弹珠机 MVP";
            PlayerSettings.bundleVersion = "2.0.0";
            PlayerSettings.defaultScreenWidth = 720;
            PlayerSettings.defaultScreenHeight = 1280;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            // MVP 采用竖屏锁定：台面是纵向布局，竖屏也避免加速度计轴向随旋转切换。
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // ProjectSettings/ProjectSettings.asset: activeInputHandler = 2（Both）
            var projectAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (projectAssets.Length > 0)
            {
                var serialized = new SerializedObject(projectAssets[0]);
                var property = serialized.FindProperty("activeInputHandler");
                if (property != null && property.intValue != 2)
                {
                    property.intValue = 2;
                    serialized.ApplyModifiedProperties();
                    Debug.Log("[BUILD] 输入处理已设置为 Both（旧 Input + Input System），旧 Input 用于鼠标/触屏/加速度计。");
                }
            }

            // 固定物理步长 1/60 秒。
            var timeAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (timeAssets.Length > 0)
            {
                var serialized = new SerializedObject(timeAssets[0]);
                var property = serialized.FindProperty("Fixed Timestep");
                if (property != null)
                {
                    property.floatValue = 1f / 60f;
                    serialized.ApplyModifiedProperties();
                }
            }

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.waytoagi.pachinko.mvp");
        }
    }
}
