using System.Collections;
using UnityEngine;

namespace Pachinko
{
    /// <summary>静态钉柱。碰撞时做一次轻微的脉冲反馈，不影响物理。</summary>
    public class PinView : MonoBehaviour
    {
        private Transform _visual;
        private SpriteRenderer _inner;
        private Color _innerColor;
        private Coroutine _pulse;

        public void Initialize(Transform visual, SpriteRenderer inner)
        {
            _visual = visual;
            _inner = inner;
            _innerColor = inner != null ? inner.color : Color.white;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<MarbleController>() == null) return;
            if (_pulse != null) StopCoroutine(_pulse);
            _pulse = StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                if (_visual != null) _visual.localScale = Vector3.one * (1f + 0.14f * k);
                if (_inner != null) _inner.color = Color.Lerp(_innerColor, Theme.AmberBright, k * 0.8f);
                yield return null;
            }
            if (_visual != null) _visual.localScale = Vector3.one;
            if (_inner != null) _inner.color = _innerColor;
            _pulse = null;
        }
    }
}
