using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pachinko
{
    public static class RuntimeBootstrapSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // 策划要求固定物理步长 1/60 秒；在场景加载前应用，避免第一帧仍用旧值。
            Time.fixedDeltaTime = 1f / 60f;
            Application.targetFrameRate = 60;
            Physics2D.velocityIterations = 10;
            Physics2D.positionIterations = 3;
        }
    }

    /// <summary>场景入口：读取配置与存档，搭建相机、台面、物理对象和 UI。</summary>
    [DefaultExecutionOrder(-500)]
    public class GameBootstrap : MonoBehaviour
    {
        public GameManager Game { get; private set; }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.fixedDeltaTime = 1f / 60f;
            Physics2D.velocityIterations = 10;
            Physics2D.positionIterations = 3;
            Physics2D.queriesHitTriggers = true;

            GameConfig config;
            string source;
            try
            {
                config = ConfigLoader.Load(out source);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Bootstrap] 配置加载失败：{e}");
                ShowFatalError("配置加载失败\n请检查 StreamingAssets/game_config.json\n\n" + e.Message);
                return;
            }

            Debug.Log($"[Bootstrap] 配置来源：{source}");
            if (HasCommandLineFlag("-fresh-save"))
            {
                SaveSystem.Delete();
                Debug.Log("[Bootstrap] -fresh-save：已清空本地存档");
            }
            var save = SaveSystem.Load(config);
            Physics2D.gravity = new Vector2(0f, -config.physics.gravity);

            var camera = CreateCamera();
            var board = BoardBuilder.Build(config, transform, camera);

            int poolSize = Mathf.Clamp(Mathf.Max(14, config.VolleySize), 1, 16);
            var marbleRoot = new GameObject("Marbles").transform;
            marbleRoot.SetParent(transform, false);
            var marbles = new MarbleController[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var marbleGo = new GameObject("Marble_" + i);
                marbleGo.transform.SetParent(marbleRoot, false);
                var marble = marbleGo.AddComponent<MarbleController>();
                marble.Initialize(config);
                marbles[i] = marble;
            }

            if (board.LaneGate != null) board.LaneGate.Initialize(marbles);

            var launcherGo = new GameObject("SpringLauncher");
            launcherGo.transform.SetParent(transform, false);
            var launcher = launcherGo.AddComponent<SpringLauncher>();
            launcher.Initialize(config, board, camera);

            var tilt = gameObject.AddComponent<TiltController>();
            tilt.Initialize(config, save);

            var score = new ScoreManager(save);
            var shop = new ShopManager(config, save);

            Game = gameObject.AddComponent<GameManager>();
            Game.Initialize(config, save, board, marbles, launcher, tilt, score, shop, camera);

            var fitter = camera.gameObject.AddComponent<CameraFitter>();
            fitter.Initialize(camera, board.VisualBounds);

            if (GatePhysicsProbe.IsRequested)
            {
                gameObject.AddComponent<GatePhysicsProbe>();
            }

            if (RuntimeDiagnostics.IsAutoplayRequested)
            {
                var diag = gameObject.AddComponent<RuntimeDiagnostics>();
                diag.Initialize(Game);
            }
        }

        private Camera CreateCamera()
        {
            var go = new GameObject("MainCamera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Theme.Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static bool HasCommandLineFlag(string flag)
        {
            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (string.Equals(arg, flag, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void ShowFatalError(string message)
        {
            var canvas = UIFactory.CreateCanvas(out var safe);
            UIFactory.SetFullScreenImage(safe, "ErrorBg", Theme.Background);
            var panel = UIFactory.Panel(safe, "ErrorPanel", UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(900f, 500f), Theme.Panel, 30, true);
            UIFactory.Label(panel, "ErrorTitle", "启动失败", 44, Theme.Red, TextAnchor.MiddleCenter,
                UIFactory.TopCenter, new Vector2(0f, -40f), new Vector2(800f, 70f), true);
            UIFactory.Label(panel, "ErrorBody", message, 22, Theme.Cream, TextAnchor.UpperLeft,
                UIFactory.TopLeft, new Vector2(40f, -130f), new Vector2(820f, 300f));
        }
    }

    public class CameraFitter : MonoBehaviour
    {
        private Camera _camera;
        private Bounds _bounds;
        private float _lastAspect;

        public void Initialize(Camera camera, Bounds bounds)
        {
            _camera = camera;
            _bounds = bounds;
            Apply();
        }

        private void Update()
        {
            if (_camera == null) return;
            if (!Mathf.Approximately(_lastAspect, _camera.aspect)) Apply();
        }

        private void Apply()
        {
            if (_camera == null) return;
            _lastAspect = _camera.aspect;
            const float verticalPadding = 1.72f;
            const float horizontalPadding = 0.45f;
            float sizeForHeight = _bounds.extents.y + verticalPadding;
            float sizeForWidth = (_bounds.extents.x + horizontalPadding) / Mathf.Max(0.0001f, _camera.aspect);
            _camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
            _camera.transform.position = new Vector3(_bounds.center.x, _bounds.center.y, -10f);
        }
    }
}
