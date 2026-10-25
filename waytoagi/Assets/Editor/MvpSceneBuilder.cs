using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Pachinko;
using UnityEditor;
using UnityEditor.Build;
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
        public const string AndroidBuildDirectory = "Builds/Android";
        public const string AndroidBuildName = "PachinkoMVP.apk";
        public const string AndroidPackageId = "com.waytoagi.pachinko.mvp";

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

        [MenuItem("Pachinko/4. 构建 Android APK", priority = 4)]
        public static void BuildAndroid()
        {
            EnsureAndroidSupport();
            EnsureAndroidActiveTarget();
            EnsureAndroidTools();

            if (!File.Exists(ScenePath))
            {
                throw new Exception($"缺少启动场景 {ScenePath}，请先执行 Pachinko → 1. 生成 MVP 场景。");
            }

            EnsureFolders();
            SyncConfig();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ConfigurePlayer();
            ConfigureAndroidPlayer();

            string output = Path.GetFullPath(Path.Combine(AndroidBuildDirectory, AndroidBuildName));
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? AndroidBuildDirectory);

            // 目标产物是可直接安装的 APK，而不是 Google Play 使用的 AAB。
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Android 构建失败：{report.summary.result}，错误 {report.summary.totalErrors}");
            }
            Debug.Log($"[BUILD] Android APK 已输出：{output}，大小 {report.summary.totalSize / (1024f * 1024f):F1} MB");
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

        public static void RunBuildAndroidBatch()
        {
            try
            {
                BuildAndroid();
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

        private static void EnsureAndroidSupport()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new Exception(
                    "当前 Unity 编辑器没有安装 Android Build Support。请先在 Unity Hub 中为 6000.3.8f1 添加 " +
                    "Android Build Support、Android SDK & NDK Tools 和 OpenJDK，然后重新打开项目。");
            }
        }

        private static void EnsureAndroidActiveTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) return;
            throw new Exception(
                "当前激活平台不是 Android。请先在 File > Build Profiles 中选择 Android 并点击 Switch Platform，" +
                "或在命令行构建时加 -buildTarget Android。");
        }

        /// <summary>
        /// Android 主模块安装成功不代表 JDK / SDK / NDK 已安装。提前检查，避免构建到 Gradle
        /// 阶段才报 "JDK not found" 这类难以定位的错误。
        /// </summary>
        internal static void EnsureAndroidTools()
        {
            string androidPlayer = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            if (string.IsNullOrEmpty(androidPlayer))
            {
                androidPlayer = Path.Combine(EditorApplication.applicationContentsPath,
                    "PlaybackEngines", "AndroidPlayer");
            }

            string jdk = ResolveAndroidToolPath("jdkRootPath", Path.Combine(androidPlayer, "OpenJDK"));
            string sdk = ResolveAndroidToolPath("sdkRootPath", Path.Combine(androidPlayer, "SDK"));
            string ndk = ResolveAndroidToolPath("ndkRootPath", Path.Combine(androidPlayer, "NDK"));

            var missing = new List<string>();

            string java = Path.Combine(jdk, "bin", "java.exe");
            if (!File.Exists(java)) missing.Add($"OpenJDK 17\n      期望路径：{java}");

            string adb = Path.Combine(sdk, "platform-tools", "adb.exe");
            if (!File.Exists(adb)) missing.Add($"Android SDK · platform-tools\n      期望路径：{adb}");

            string buildTools = Path.Combine(sdk, "build-tools");
            if (!Directory.Exists(buildTools) || Directory.GetDirectories(buildTools).Length == 0)
                missing.Add($"Android SDK · build-tools\n      期望路径：{buildTools}\\<版本>");

            string platforms = Path.Combine(sdk, "platforms");
            if (!Directory.Exists(platforms) || Directory.GetDirectories(platforms).Length == 0)
                missing.Add($"Android SDK · platforms\n      期望路径：{platforms}\\android-<版本>");

            string ndkProperties = Path.Combine(ndk, "source.properties");
            if (!File.Exists(ndkProperties)) missing.Add($"Android NDK r27c\n      期望路径：{ndkProperties}");

            if (missing.Count == 0) return;

            throw new Exception(
                "Android 构建工具不完整，缺少：\n  - " + string.Join("\n  - ", missing) +
                "\n\n这不是项目配置或 JAVA_HOME 的问题。Unity 只安装了 Android Build Support 主模块，" +
                "请关闭 Unity，用管理员身份运行 Unity 安装器（或 Unity Hub 的 Add modules），为 6000.3.8f1 补装 " +
                "Android SDK & NDK Tools 与 OpenJDK。若使用外部工具链，请先在 " +
                "Edit > Preferences > External Tools 中指定 JDK / SDK / NDK 路径。");
        }

        private static string ResolveAndroidToolPath(string propertyName, string embeddedPath)
        {
            try
            {
                Type settingsType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    settingsType = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                    if (settingsType != null) break;
                }

                var property = settingsType?.GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                string configured = property?.GetValue(null) as string;
                if (!string.IsNullOrEmpty(configured) && Directory.Exists(configured)) return configured;
            }
            catch
            {
                // 反射不可用时回退到 Unity 内置模块路径。
            }
            return embeddedPath;
        }

        private static void ConfigureAndroidPlayer()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log(PlayerSettings.Android.useCustomKeystore
                ? "[BUILD] Android：ARM64 / IL2CPP / 使用自定义 keystore 签名。"
                : "[BUILD] Android：ARM64 / IL2CPP / 未配置 keystore，使用 Unity 调试签名；APK 可直接安装测试，但不能上架。");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "WaytoAGI";
            PlayerSettings.productName = "弹珠机 V3.0";
            PlayerSettings.bundleVersion = "3.0.0";
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

            // 输入处理保持项目当前设置。项目只使用旧版 Input（鼠标 / 触屏 / 加速度计），
            // 因此可以在 Player Settings 中改为 Input Manager (Old)，避免 Android 的 Both 警告；
            // 构建脚本不再强制覆盖该设置。

            // 固定物理步长由 GameBootstrap 在运行时设置（Time.fixedDeltaTime = 1/60），
            // 不再改写 ProjectSettings/TimeManager.asset。Unity 6 中该序列化字段不是 float，
            // 构建期写 floatValue 会报 "type is not a supported float value"。

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);
        }
    }

    /// <summary>
    /// 即使从 File > Build Profiles 直接点 Build，也先检查 Android 工具链，
    /// 避免绕过 Pachinko 菜单后只看到 Unity 的 "JDK not found"。
    /// </summary>
    public class AndroidBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                MvpSceneBuilder.EnsureAndroidTools();
            }
        }
    }
}
