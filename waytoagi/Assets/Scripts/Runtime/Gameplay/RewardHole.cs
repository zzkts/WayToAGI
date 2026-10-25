using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    /// <summary>奖励洞：一次性触发、展示当前积分，命中后交由 GameManager 结算。</summary>
    public class RewardHole : MonoBehaviour
    {
        public HoleDef Def { get; private set; }
        public int CurrentPoints { get; private set; }
        public bool HasReward => Def != null && Def.HasReward;
        public event Action<RewardHole, MarbleController> Hit;

        private Text _pointsLabel;
        private SpriteRenderer _glow;
        private Color _glowColor;
        private bool _armed;

        public void Initialize(HoleDef def, int points, Text pointsLabel, SpriteRenderer glow)
        {
            Def = def;
            CurrentPoints = HasReward ? points : 0;
            _pointsLabel = pointsLabel;
            _glow = glow;
            _glowColor = glow != null ? glow.color : Theme.Orange;
            SetPoints(CurrentPoints);
        }

        public void Arm()
        {
            _armed = true;
        }

        public void Disarm()
        {
            _armed = false;
        }

        public void SetPoints(int points)
        {
            CurrentPoints = HasReward ? Mathf.Max(0, points) : 0;
            if (_pointsLabel != null)
            {
                _pointsLabel.text = HasReward
                    ? $"×{Def.multiplier} · {CurrentPoints}"
                    : "空穴";
            }
        }

        private void Update()
        {
            if (_glow == null) return;
            float pulse = 0.72f + Mathf.Sin(Time.time * 1.8f + transform.position.x) * 0.12f;
            _glow.color = _glowColor.Alpha(pulse * 0.32f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_armed) return;
            var marble = other.GetComponent<MarbleController>();
            if (marble == null || marble.IsCaptured) return;
            // 同一个洞可以被同一轮齐射的多颗弹珠命中；每颗弹珠由 GameManager 单独结算。
            Hit?.Invoke(this, marble);
        }
    }
}
