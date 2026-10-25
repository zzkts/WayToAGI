using System;
using System.Collections;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 弹珠表现与物理控制。齐射时由 GameManager 在弹簧回弹的同一帧统一赋初速度，
    /// 之后的飞行、反弹与落洞全部交给 Unity 2D 物理。
    /// V3 中弹珠外观由当前附魔决定；解锁引爆后，每累计碰撞若干次会冲击附近弹珠。
    /// </summary>
    public class MarbleController : MonoBehaviour
    {
        public Rigidbody2D Body => _body;
        public Collider2D Collider => _collider;
        public float Radius => _collider != null ? _collider.radius : 0.1f;
        public bool IsCaptured { get; private set; }
        public bool IsInFlight { get; private set; }
        public int CollisionCount => _collisionCount;
        public event Action Lost;

        private GameConfig _config;
        private Rigidbody2D _body;
        private CircleCollider2D _collider;
        private Transform _visualRoot;
        private SpriteRenderer _shadow;
        private SpriteRenderer _outline;
        private SpriteRenderer _inner;
        private SpriteRenderer _highlight;
        private TrailRenderer _trail;
        private Color _fillColor = Theme.Cream;
        private bool _lostRaised;
        private float _lowSpeedTime;
        private float _flightTime;

        private ExplosionSettings _explosion;
        private bool _explosionUnlocked;
        private int _collisionCount;

        public void Initialize(GameConfig config)
        {
            _config = config;
            _body = gameObject.GetComponent<Rigidbody2D>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.mass = config.physics.ballMass;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;

            _collider = gameObject.GetComponent<CircleCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<CircleCollider2D>();
            _collider.radius = config.physics.ballRadius;
            _collider.isTrigger = false;
            var ballMaterial = new PhysicsMaterial2D("BallMaterial")
            {
                bounciness = config.physics.ballBounciness,
                friction = 0.07f
            };
            _collider.sharedMaterial = ballMaterial;

            _visualRoot = new GameObject("BallVisual").transform;
            _visualRoot.SetParent(transform, false);

            _shadow = Theme.CircleObject(_visualRoot, "Shadow", new Vector2(0.025f, -0.038f), config.physics.ballRadius * 2.1f, Theme.Shadow.Alpha(0.42f), 38);
            _outline = Theme.CircleObject(_visualRoot, "Outline", Vector2.zero, config.physics.ballRadius * 2.05f, Theme.Ink, 40);
            _inner = Theme.CircleObject(_visualRoot, "Inner", Vector2.zero, config.physics.ballRadius * 1.82f, Theme.Cream, 41);
            _highlight = Theme.CircleObject(_visualRoot, "Highlight", new Vector2(-0.022f, 0.028f), config.physics.ballRadius * 0.48f, Color.white.Alpha(0.72f), 42);

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.20f;
            _trail.minVertexDistance = 0.015f;
            _trail.startWidth = config.physics.ballRadius * 0.9f;
            _trail.endWidth = 0f;
            _trail.numCapVertices = 4;
            _trail.numCornerVertices = 2;
            _trail.alignment = LineAlignment.View;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.sortingOrder = 39;
            var trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null) _trail.material = new Material(trailShader);
            _trail.emitting = false;

            SetAppearance(config.GetStandardBall(), null);
            SetExplosion(config.explosion, false);
            gameObject.SetActive(false);
        }

        /// <summary>附魔决定弹珠主体、描边、高光与拖尾颜色；未附魔时使用标准白球配色。</summary>
        public void SetAppearance(BallDef standardBall, EnchantDef enchant)
        {
            Color fill = enchant != null ? Theme.Hex(enchant.color) : Theme.BallColor(standardBall);
            Color outline = enchant != null ? Theme.Darken(fill, 0.36f) : Theme.BallOutline(standardBall);
            _fillColor = fill;
            if (_outline != null) _outline.color = outline;
            if (_inner != null) _inner.color = fill;
            if (_highlight != null) _highlight.color = Color.Lerp(fill, Color.white, 0.65f).Alpha(0.8f);
            if (_shadow != null) _shadow.color = Theme.Shadow.Alpha(0.42f);
            if (_trail != null)
            {
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(fill, 0f),
                        new GradientColorKey(Theme.Lighten(fill, 0.2f), 1f)
                    },
                    new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
                _trail.colorGradient = gradient;
            }
        }

        public void SetExplosion(ExplosionSettings settings, bool unlocked)
        {
            _explosion = settings;
            _explosionUnlocked = unlocked && settings != null;
        }

        public void PrepareAt(Vector2 position)
        {
            StopAllCoroutines();
            IsCaptured = false;
            IsInFlight = false;
            _lostRaised = false;
            _lowSpeedTime = 0f;
            _flightTime = 0f;
            _collisionCount = 0;
            gameObject.SetActive(true);
            _body.simulated = true;
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _collider.enabled = true;
            transform.position = new Vector3(position.x, position.y, 0f);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (_visualRoot != null) _visualRoot.localScale = Vector3.one;
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }
        }

        public void ReleasePhysics()
        {
            if (IsCaptured) return;
            _body.simulated = true;
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 1f;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _body.WakeUp();
            IsInFlight = true;
            _lowSpeedTime = 0f;
            _flightTime = 0f;
            if (_trail != null) _trail.emitting = true;
        }

        public void Launch(Vector2 velocity)
        {
            ReleasePhysics();
            _body.linearVelocity = velocity;
        }

        public void CaptureInto(Vector2 holeCenter)
        {
            if (IsCaptured) return;
            IsCaptured = true;
            IsInFlight = false;
            _body.simulated = false;
            _collider.enabled = false;
            if (_trail != null) _trail.emitting = false;
            StartCoroutine(CaptureAnimation(holeCenter));
        }

        private IEnumerator CaptureAnimation(Vector2 target)
        {
            Vector3 start = transform.position;
            Vector3 startScale = _visualRoot != null ? _visualRoot.localScale : Vector3.one;
            float duration = 0.28f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float ease = k * k * (3f - 2f * k);
                transform.position = Vector3.Lerp(start, new Vector3(target.x, target.y - 0.22f, 0f), ease);
                if (_visualRoot != null) _visualRoot.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, ease);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            StopAllCoroutines();
            IsInFlight = false;
            IsCaptured = false;
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsInFlight || IsCaptured) return;

            // 碰撞音只区分钉柱与挡板；墙面、闸门和弹珠互撞不发声。
            if (collision.collider.GetComponent<Baffle>() != null)
            {
                AudioManager.Play("hit_baffle");
            }
            else if (collision.collider.GetComponent<PinView>() != null)
            {
                AudioManager.Play("hit_peg");
            }

            if (!_explosionUnlocked || _explosion == null) return;

            _collisionCount++;
            if (_collisionCount < _explosion.triggerCollisions) return;
            _collisionCount = 0;
            Explode();
        }

        /// <summary>引爆：对半径内的其他飞行中弹珠施加一次向外冲量，不造成伤害或直接得分。</summary>
        private void Explode()
        {
            AudioManager.Play("explode");
            Vector2 center = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, _explosion.radius);
            int affected = 0;
            foreach (var hit in hits)
            {
                if (hit == null || hit == _collider) continue;
                var other = hit.GetComponent<MarbleController>();
                if (other == null || other == this || !other.IsInFlight || other.IsCaptured) continue;

                Vector2 delta = (Vector2)other.transform.position - center;
                Vector2 dir = delta.sqrMagnitude > 0.0001f
                    ? delta.normalized
                    : UnityEngine.Random.insideUnitCircle.normalized;
                other.Body.AddForce(dir * (_explosion.force * other.Body.mass), ForceMode2D.Impulse);
                other.Body.WakeUp();
                affected++;
            }

            StartCoroutine(ExplosionVisual());
            Debug.Log($"[Explosion] pos={center}, radius={_explosion.radius:F2}, affected={affected}");
        }

        private IEnumerator ExplosionVisual()
        {
            var go = new GameObject("ExplosionFx");
            go.transform.SetParent(transform.parent, false);
            go.transform.position = transform.position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Theme.Glow;
            renderer.sortingOrder = 45;
            Color color = Theme.Lighten(_fillColor, 0.35f);

            float duration = 0.34f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float size = _explosion.radius * 2f * Mathf.Lerp(0.28f, 1f, k * k * (3f - 2f * k));
                go.transform.localScale = new Vector3(size, size, 1f);
                renderer.color = color.Alpha(0.72f * (1f - k));
                yield return null;
            }
            Destroy(go);
        }

        private void Update()
        {
            if (!IsInFlight || IsCaptured) return;

            _flightTime += Time.deltaTime;
            if (_body.linearVelocity.sqrMagnitude < 0.0225f)
            {
                _lowSpeedTime += Time.deltaTime;
            }
            else
            {
                _lowSpeedTime = 0f;
            }

            if (_lowSpeedTime > 1.5f)
            {
                _lowSpeedTime = 0f;
                // 防卡死：速度持续过低时施加一次明显的横向扰动，帮助脱离钉柱/挡板夹角。
                float dir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                float kickX = dir * UnityEngine.Random.Range(1.2f, 2.4f);
                float kickY = UnityEngine.Random.Range(-0.8f, 0.6f);
                _body.AddForce(new Vector2(kickX, kickY) * Mathf.Max(0.2f, _config.physics.ballMass), ForceMode2D.Impulse);
                _body.WakeUp();
            }

            Vector2 p = transform.position;
            bool outside = p.x < -6f || p.x > 6f || p.y < -6.8f || p.y > 8f;
            if ((outside || _flightTime > 28f) && !_lostRaised)
            {
                _lostRaised = true;
                Lost?.Invoke();
            }
        }
    }
}
