using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 可左右往返摆动的挡板。配置中的 amplitude 是往返总幅度（1.2 = 120px），
    /// 因此单侧位移取一半，避免长挡板在摆动时穿出机台侧壁。
    /// 使用 Kinematic Rigidbody2D.MovePosition，保证能稳定推动动态弹珠。
    /// </summary>
    public class Baffle : MonoBehaviour
    {
        private Rigidbody2D _body;
        private Vector2 _origin;
        private float _halfAmplitude;
        private float _period;
        private float _phase;

        public void Initialize(float totalAmplitude, float period, float phase)
        {
            _body = GetComponent<Rigidbody2D>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;

            _origin = transform.position;
            _halfAmplitude = Mathf.Max(0f, totalAmplitude) * 0.5f;
            _period = Mathf.Max(0.2f, period);
            _phase = phase;
        }

        private void FixedUpdate()
        {
            if (_body == null) return;
            float offset = Mathf.Sin((Time.time / _period) * Mathf.PI * 2f + _phase) * _halfAmplitude;
            _body.MovePosition(_origin + new Vector2(offset, 0f));
        }
    }
}
