using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pachinko
{
    /// <summary>
    /// 弹簧推杆：向下拖动压缩，松手后连续回弹，把待发射槽里的全部弹珠一次弹出。
    /// 出口常开，不设封闭闸门。
    /// </summary>
    public class SpringLauncher : MonoBehaviour
    {
        public float ChargeRatio { get; private set; }
        public bool IsCharging { get; private set; }
        public Func<bool> CanLaunch { get; set; }
        public event Action<float> LaunchRequested;

        private GameConfig _config;
        private BoardRefs _board;
        private Camera _camera;

        private Transform _plungerRoot;
        private Rigidbody2D _plungerBody;
        private BoxCollider2D _plungerCollider;
        private SpriteRenderer _plungerFace;
        private SpriteRenderer _gaugeFill;
        private GameObject _gaugeFillRoot;
        private LineRenderer _spring;
        private Text _hint;
        private float _readyHeadY;
        private float _maxPull;
        private float _pull;
        private bool _dragging;
        private float _dragStartWorldY;
        private int _activeFinger = -1;
        private Coroutine _burstRoutine;

        public void Initialize(GameConfig config, BoardRefs board, Camera camera)
        {
            _config = config;
            _board = board;
            _camera = camera;
            _maxPull = config.launch.maxPullWorld;
            _readyHeadY = BoardGeometry.LauncherSeatY - config.physics.ballRadius - 0.09f;

            BuildPlunger();
            BuildSpring();
            BuildGauge();
            _hint = board.LauncherHint;

            SetPull(0f);
        }

        private void BuildPlunger()
        {
            var root = new GameObject("SpringPlunger").transform;
            root.SetParent(_board.Root, false);
            root.localPosition = new Vector3(BoardGeometry.LauncherX, _readyHeadY, 0f);
            _plungerRoot = root;

            _plungerBody = root.gameObject.AddComponent<Rigidbody2D>();
            _plungerBody.bodyType = RigidbodyType2D.Kinematic;
            _plungerBody.gravityScale = 0f;
            _plungerBody.mass = _config.physics.plungerMass;
            _plungerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _plungerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _plungerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _plungerBody.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var rod = Theme.RoundedRect(root, "Rod", new Vector2(0f, -0.18f), new Vector2(0.10f, 0.36f), Theme.Hex("#2A6670"), 8, 29);
            Theme.CircleObject(root, "RodCap", new Vector2(0f, -0.36f), 0.12f, Theme.TealBright.Alpha(0.7f), 29);

            Theme.RoundedRect(root, "HeadShadow", new Vector2(0.02f, -0.055f), new Vector2(0.48f, 0.20f), Theme.Shadow.Alpha(0.5f), 10, 30);
            Theme.RoundedRect(root, "HeadFrame", Vector2.zero, new Vector2(0.48f, 0.20f), Theme.Hex("#2A6670"), 10, 31);
            _plungerFace = Theme.RoundedRect(root, "HeadFace", new Vector2(0f, 0.012f), new Vector2(0.40f, 0.13f), Theme.Amber, 8, 32);
            for (int i = -1; i <= 1; i++)
            {
                Theme.RoundedRect(root, "Grip", new Vector2(i * 0.105f, 0.012f), new Vector2(0.032f, 0.075f), Theme.Orange.Alpha(0.8f), 2, 33);
            }

            _plungerCollider = root.gameObject.AddComponent<BoxCollider2D>();
            _plungerCollider.size = new Vector2(0.46f, 0.17f);
            var mat = new PhysicsMaterial2D("PlungerMaterial")
            {
                bounciness = _config.physics.plungerBounciness,
                friction = 0.04f
            };
            _plungerCollider.sharedMaterial = mat;
            _plungerCollider.usedByEffector = false;
        }

        private void BuildSpring()
        {
            var go = new GameObject("SpringCoil");
            go.transform.SetParent(_board.Root, false);
            _spring = go.AddComponent<LineRenderer>();
            _spring.useWorldSpace = false;
            _spring.widthMultiplier = 0.023f;
            _spring.numCapVertices = 2;
            _spring.numCornerVertices = 1;
            _spring.material = new Material(Shader.Find("Sprites/Default"));
            _spring.startColor = Theme.Teal;
            _spring.endColor = Theme.TealBright;
            _spring.sortingOrder = 29;
        }

        private void BuildGauge()
        {
            var gaugeX = BoardGeometry.LauncherX - 0.27f;
            const float bottom = 1.12f;
            const float maxHeight = 1.80f;
            Theme.RoundedRect(_board.Root, "GaugeTrack", new Vector2(gaugeX, bottom + maxHeight * 0.5f), new Vector2(0.065f, maxHeight), Theme.Ink.Alpha(0.10f), 3, 14);
            Theme.CircleObject(_board.Root, "GaugeDotBottom", new Vector2(gaugeX, bottom), 0.075f, Theme.Teal.Alpha(0.4f), 15);
            Theme.CircleObject(_board.Root, "GaugeDotTop", new Vector2(gaugeX, bottom + maxHeight), 0.075f, Theme.Orange.Alpha(0.45f), 15);

            _gaugeFillRoot = new GameObject("GaugeFill");
            _gaugeFillRoot.transform.SetParent(_board.Root, false);
            _gaugeFillRoot.transform.localPosition = new Vector3(gaugeX, bottom, 0f);
            var fill = _gaugeFillRoot.AddComponent<SpriteRenderer>();
            fill.sprite = Theme.Rounded(6);
            fill.drawMode = SpriteDrawMode.Sliced;
            fill.size = new Vector2(0.075f, 0.01f);
            fill.color = Theme.Amber;
            fill.sortingOrder = 16;
            _gaugeFill = fill;
            _gaugeFillRoot.SetActive(false);
        }

        public void Prepare()
        {
            StopAllCoroutines();
            _burstRoutine = null;
            _dragging = false;
            _activeFinger = -1;
            IsCharging = false;
            ChargeRatio = 0f;
            _pull = 0f;
            _plungerBody.bodyType = RigidbodyType2D.Kinematic;
            _plungerBody.gravityScale = 0f;
            _plungerBody.linearVelocity = Vector2.zero;
            _plungerCollider.enabled = true;
            SetPull(0f);
            if (_gaugeFillRoot != null) _gaugeFillRoot.SetActive(false);
            if (_hint != null) _hint.gameObject.SetActive(true);
        }

        public void Fire(float ratio, int pulseCount)
        {
            ChargeRatio = Mathf.Clamp01(ratio);
            IsCharging = false;
            _dragging = false;
            _activeFinger = -1;
            if (_gaugeFillRoot != null) _gaugeFillRoot.SetActive(false);
            if (_hint != null) _hint.gameObject.SetActive(false);

            // 出口常开，弹簧只负责连续回弹的视觉；弹珠速度由 GameManager 的连弹脉冲同步施加。
            _plungerCollider.enabled = false;
            _plungerFace.color = Color.Lerp(Theme.Amber, Theme.Red, ChargeRatio * ChargeRatio);

            if (_burstRoutine != null) StopCoroutine(_burstRoutine);
            _burstRoutine = StartCoroutine(AnimateBurst(ChargeRatio, Mathf.Max(1, pulseCount)));

            Debug.Log($"[Launch] charge={ChargeRatio:F2}, pulses={pulseCount}, targetBallSpeed={Mathf.Lerp(_config.launch.minBallSpeed, _config.launch.maxBallSpeed, ChargeRatio):F2}");
        }

        /// <summary>
        /// 第一次行程从玩家拉下的位置弹回，之后每次脉冲都是幅度衰减的反复回弹。
        /// 每一轮对应 GameManager 弹出一排弹珠，最后一轮结束后弹簧收势归位。
        /// </summary>
        private IEnumerator AnimateBurst(float ratio, int pulseCount)
        {
            _plungerBody.bodyType = RigidbodyType2D.Kinematic;
            _plungerBody.linearVelocity = Vector2.zero;
            _plungerBody.angularVelocity = 0f;

            const float pulseDuration = 0.10f;
            float startOffset = _plungerRoot.localPosition.y - _readyHeadY;
            float overshoot = 0.10f + 0.12f * ratio;

            for (int pulse = 0; pulse < pulseCount; pulse++)
            {
                float t = 0f;
                float amplitude = overshoot * Mathf.Lerp(1f, 0.48f, pulse / (float)pulseCount);
                while (t < pulseDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / pulseDuration);
                    float offset;
                    if (pulse == 0)
                    {
                        // 先快速弹出，再回落到静止位置。
                        offset = k < 0.42f
                            ? Mathf.Lerp(startOffset, amplitude, EaseOut(k / 0.42f))
                            : Mathf.Lerp(amplitude, 0f, EaseIn((k - 0.42f) / 0.58f));
                    }
                    else
                    {
                        // 后续脉冲：零位 → 弹起 → 零位的反复回弹。
                        offset = Mathf.Sin(k * Mathf.PI) * amplitude;
                    }
                    SetHeadOffset(offset);
                    yield return null;
                }
                SetHeadOffset(0f);
            }

            float settle = 0.24f;
            float st = 0f;
            while (st < settle)
            {
                st += Time.deltaTime;
                float k = Mathf.Clamp01(st / settle);
                SetHeadOffset(Mathf.Sin(k * Mathf.PI * 2f) * 0.035f * (1f - k));
                yield return null;
            }

            SetPull(0f);
            _burstRoutine = null;
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        private void SetHeadOffset(float offset)
        {
            if (_plungerRoot == null) return;
            _plungerRoot.localPosition = new Vector3(BoardGeometry.LauncherX, _readyHeadY + offset, 0f);
            UpdateSpring();
        }

        private void SetPull(float pull)
        {
            _pull = Mathf.Clamp(pull, 0f, _maxPull);
            ChargeRatio = _maxPull > 0.0001f ? _pull / _maxPull : 0f;
            if (_plungerRoot != null)
            {
                _plungerRoot.localPosition = new Vector3(BoardGeometry.LauncherX, _readyHeadY - _pull, 0f);
            }
            if (_plungerFace != null)
            {
                _plungerFace.color = Color.Lerp(Theme.Amber, Theme.Red, ChargeRatio * ChargeRatio);
            }
            UpdateGauge();
            UpdateSpring();
        }

        private void UpdateGauge()
        {
            if (_gaugeFill == null || _gaugeFillRoot == null) return;
            const float maxHeight = 1.80f;
            float h = Mathf.Max(0.015f, maxHeight * ChargeRatio);
            _gaugeFill.size = new Vector2(0.075f, h);
            _gaugeFillRoot.transform.localPosition = new Vector3(BoardGeometry.LauncherX - 0.27f, 1.12f + h * 0.5f, 0f);
            _gaugeFill.color = Color.Lerp(Theme.Amber, Theme.Red, ChargeRatio);
            _gaugeFillRoot.SetActive(IsCharging && ChargeRatio > 0.004f);
        }

        private void UpdateSpring()
        {
            if (_spring == null || _plungerRoot == null) return;
            const float baseY = 0.96f;
            float topY = _plungerRoot.localPosition.y - 0.11f;
            float span = Mathf.Max(0.05f, topY - baseY);
            int coilCount = 9;
            int points = coilCount * 2 + 2;
            _spring.positionCount = points;
            _spring.SetPosition(0, new Vector3(BoardGeometry.LauncherX, baseY, 0f));
            for (int i = 0; i < coilCount; i++)
            {
                float t = (i + 1f) / (coilCount + 1f);
                float y = baseY + span * t;
                float dir = (i % 2 == 0) ? 1f : -1f;
                _spring.SetPosition(i * 2 + 1, new Vector3(BoardGeometry.LauncherX + dir * 0.060f, y - span * 0.018f, 0f));
                _spring.SetPosition(i * 2 + 2, new Vector3(BoardGeometry.LauncherX - dir * 0.060f, y + span * 0.018f, 0f));
            }
            _spring.SetPosition(points - 1, new Vector3(BoardGeometry.LauncherX, topY, 0f));
            _spring.enabled = true;
        }

        private void Update()
        {
            if (_hint != null && _hint.gameObject.activeSelf)
            {
                var c = _hint.color;
                c.a = 0.5f + Mathf.Sin(Time.time * 3.2f) * 0.18f;
                _hint.color = c;
            }

            bool canInteract = CanLaunch != null && CanLaunch();
            if (!canInteract)
            {
                if (_dragging) CancelDrag();
                return;
            }

            ReadPointer(out bool down, out bool held, out bool up, out Vector2 screenPosition, out int pointerId);
            if (!down && !held && !up) return;

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

            if (down && !_dragging && !overUi && InDragZone(world))
            {
                // 飞行中再次拉杆时，立刻停止上一发弹簧的回弹动画，交还控制权。
                if (_burstRoutine != null)
                {
                    StopCoroutine(_burstRoutine);
                    _burstRoutine = null;
                }
                _dragging = true;
                IsCharging = true;
                _activeFinger = pointerId;
                _dragStartWorldY = world.y;
                SetPull(0f);
                if (_hint != null) _hint.gameObject.SetActive(false);
            }

            if (_dragging)
            {
                if (held || up)
                {
                    float targetPull = _dragStartWorldY - world.y;
                    SetPull(targetPull);
                }

                if (up)
                {
                    _dragging = false;
                    IsCharging = false;
                    _activeFinger = -1;
                    if (ChargeRatio < 0.055f)
                    {
                        if (_gaugeFillRoot != null) _gaugeFillRoot.SetActive(false);
                        if (_hint != null) _hint.gameObject.SetActive(true);
                        SetPull(0f);
                    }
                    else
                    {
                        var request = LaunchRequested;
                        request?.Invoke(ChargeRatio);
                    }
                }
            }
        }

        private void CancelDrag()
        {
            _dragging = false;
            IsCharging = false;
            _activeFinger = -1;
            if (_gaugeFillRoot != null) _gaugeFillRoot.SetActive(false);
            if (_hint != null) _hint.gameObject.SetActive(true);
            SetPull(0f);
        }

        private bool InDragZone(Vector3 world)
        {
            return world.x > 2.34f && world.x < 3.58f && world.y > 0.45f && world.y < 3.40f;
        }

        private void ReadPointer(out bool down, out bool held, out bool up, out Vector2 screenPosition, out int pointerId)
        {
            down = false;
            held = false;
            up = false;
            pointerId = -1;
            screenPosition = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pointerId = touch.fingerId;
                screenPosition = touch.position;
                if (touch.phase == TouchPhase.Began)
                {
                    down = true;
                    held = true;
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    held = true;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    up = true;
                }
            }
            else
            {
                screenPosition = Input.mousePosition;
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
            }
        }
    }
}
