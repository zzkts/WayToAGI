using UnityEngine;

namespace Pachinko
{
    /// <summary>P1 动态障碍：缓慢旋转的挡板。速度保持很慢，避免出现永久封路。</summary>
    public class RotatingBar : MonoBehaviour
    {
        private Rigidbody2D _body;
        private float _degreesPerSecond;

        public void Initialize(float degreesPerSecond)
        {
            _degreesPerSecond = degreesPerSecond;
            _body = gameObject.GetComponent<Rigidbody2D>();
            if (_body == null) _body = gameObject.AddComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.angularVelocity = degreesPerSecond;
        }

        private void FixedUpdate()
        {
            if (_body == null) return;
            _body.MoveRotation(_body.rotation + _degreesPerSecond * Time.fixedDeltaTime);
        }
    }
}
