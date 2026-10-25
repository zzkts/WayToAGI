using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 手机重力感应 → Physics2D 横向重力。
    /// 输入经过死区、低通滤波、量程限制；不可用时自动降级为键盘 / 触屏左右半屏。
    /// </summary>
    public class TiltController : MonoBehaviour
    {
        public float CurrentTilt => _smoothed;
        public bool ControlEnabled => _controlEnabled;
        public float ControlMultiplier { get; set; } = 1f;
        public bool HasSensor => SystemInfo.supportsAccelerometer && !Application.isEditor;
        public bool UsingSimulation => !HasSensor;
        public bool Inverted => _save != null && _save.tiltInverted;

        private GameConfig _config;
        private SaveData _save;
        private float _smoothed;
        private float _rawSmoothed;
        private bool _controlEnabled;
        private bool _lastUseSensor;

        public void Initialize(GameConfig config, SaveData save)
        {
            _config = config;
            _save = save;
            _smoothed = 0f;
            _rawSmoothed = 0f;
            _controlEnabled = false;
            Physics2D.gravity = new Vector2(0f, -config.physics.gravity);
        }

        public void SetControl(bool enabled, bool centerImmediately = false)
        {
            _controlEnabled = enabled;
            if (!enabled)
            {
                if (centerImmediately)
                {
                    _smoothed = 0f;
                    _rawSmoothed = 0f;
                }
                ApplyGravity();
            }
        }

        public void CenterImmediately()
        {
            _smoothed = 0f;
            _rawSmoothed = 0f;
            Physics2D.gravity = new Vector2(0f, -_config.physics.gravity);
        }

        public bool CalibrateNow()
        {
            if (!HasSensor)
            {
                CenterImmediately();
                return false;
            }
            _save.tiltCalibration = ReadSensorAxis();
            _rawSmoothed = 0f;
            _smoothed = 0f;
            SaveSystem.Save(_save);
            return true;
        }

        public bool ToggleInvert()
        {
            if (_save == null) return false;
            _save.tiltInverted = !_save.tiltInverted;
            SaveSystem.Save(_save);
            return _save.tiltInverted;
        }

        private void Update()
        {
            if (_config == null) return;

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float target = 0f;
            bool useSensor = HasSensor;

            if (_controlEnabled)
            {
                if (useSensor)
                {
                    float raw = ReadSensorAxis() - _save.tiltCalibration;
                    if (_save.tiltInverted) raw = -raw;
                    _rawSmoothed = Mathf.Lerp(_rawSmoothed, raw, 1f - Mathf.Exp(-_config.tilt.sensorSmoothing * dt));
                    float magnitude = Mathf.Abs(_rawSmoothed);
                    if (magnitude <= _config.tilt.deadZone)
                    {
                        target = 0f;
                    }
                    else
                    {
                        float normalized = (magnitude - _config.tilt.deadZone) /
                                           Mathf.Max(0.001f, _config.tilt.fullScale - _config.tilt.deadZone);
                        target = Mathf.Sign(_rawSmoothed) * Mathf.Clamp01(normalized);
                    }
                }
                else
                {
                    target = ReadSimulatedTilt();
                    _rawSmoothed = target * _config.tilt.fullScale;
                }
            }

            _smoothed = Mathf.Lerp(_smoothed, target, 1f - Mathf.Exp(-_config.tilt.smoothing * dt));
            if (Mathf.Abs(_smoothed) < 0.001f) _smoothed = 0f;
            ApplyGravity();
            _lastUseSensor = useSensor;
        }

        private void ApplyGravity()
        {
            if (_config == null) return;
            float x = _controlEnabled ? _smoothed * _config.tilt.maxHorizontalGravity * Mathf.Max(0.1f, ControlMultiplier) : 0f;
            Physics2D.gravity = new Vector2(x, -_config.physics.gravity);
        }

        private float ReadSimulatedTilt()
        {
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            if (left && !right) return -1f;
            if (right && !left) return 1f;

            // 无加速度计设备上的降级方案：按住屏幕左/右半边控制倾斜。
            bool pointerHeld = Input.GetMouseButton(0) || Input.touchCount > 0;
            if (pointerHeld)
            {
                Vector2 pointer = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
                if (pointer.x < Screen.width * 0.5f) return -1f;
                return 1f;
            }
            return 0f;
        }

        private float ReadSensorAxis()
        {
            Vector3 acceleration = Input.acceleration;
            // 加速度计使用设备坐标，不在系统旋转时自动重映射。
            switch (Screen.orientation)
            {
                case ScreenOrientation.LandscapeLeft:
                    return acceleration.y;
                case ScreenOrientation.LandscapeRight:
                    return -acceleration.y;
                case ScreenOrientation.PortraitUpsideDown:
                    return -acceleration.x;
                default:
                    return acceleration.x;
            }
        }

        private void OnDisable()
        {
            if (_config != null)
            {
                Physics2D.gravity = new Vector2(0f, -_config.physics.gravity);
            }
        }
    }
}
