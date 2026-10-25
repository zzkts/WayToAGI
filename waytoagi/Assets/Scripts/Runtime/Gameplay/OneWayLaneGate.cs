using System.Collections.Generic;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 发射道单向闸门：只允许弹珠从发射道内向左离开，阻止台面里的弹珠再回落到弹簧下方。
    ///
    /// 发射道在隔板顶端以上原本完全向台面敞开，右侧钉柱/顶墙/右墙的反弹球会从上方掉回
    /// 发射道，最终落到弹簧下方。这里用一道全高碰撞墙封住出口，并逐颗弹珠按速度方向
    /// 开关 <see cref="Physics2D.IgnoreCollision"/>：
    /// - 球在发射道内且向左运动：暂时忽略碰撞，正常出膛；
    /// - 球在闸门外且向右运动：恢复碰撞，被挡回台面；
    /// - 穿透期间保持忽略，直到球完全越过闸门，避免在半重叠状态重新开启碰撞导致弹飞。
    /// 不使用传送；正常出膛不额外改速度，只对在闸门附近被倾斜控球压住、已经失去向左速度的
    /// 飞行球施加很小的向左牵引，保证它能离开发射道。隔板本身已从底墙封到闸门下沿，
    /// 台面球也没有从底部绕进发射道的路径。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class OneWayLaneGate : MonoBehaviour
    {
        private BoxCollider2D _gate;
        private MarbleController[] _marbles;
        private readonly HashSet<Collider2D> _crossing = new HashSet<Collider2D>();

        public void Initialize(MarbleController[] marbles)
        {
            _gate = GetComponent<BoxCollider2D>();
            _marbles = marbles;
        }

        private void FixedUpdate()
        {
            if (_gate == null || _marbles == null) return;

            float gateX = transform.position.x;
            float halfThickness = _gate.bounds.extents.x;
            float gateBottom = _gate.bounds.min.y;
            float gateTop = _gate.bounds.max.y;

            for (int i = 0; i < _marbles.Length; i++)
            {
                var marble = _marbles[i];
                if (marble == null) continue;

                var ball = marble.Collider;
                if (ball == null) continue;

                if (!marble.gameObject.activeInHierarchy)
                {
                    // 隐藏的球不参与物理，也不对非激活碰撞体调用 IgnoreCollision；
                    // 下次从弹仓取出时会在第一个 FixedUpdate 里重新恢复默认碰撞。
                    _crossing.Remove(ball);
                    continue;
                }

                Vector2 position = marble.transform.position;
                float radius = marble.Radius;
                Rigidbody2D body = marble.Body;
                float vx = body != null ? body.linearVelocity.x : 0f;

                // 倾斜控球可能把刚出膛的球往回压，极端时甚至让球停在发射道里。
                // 在闸门高度范围内给飞行中的球一个很轻的向左牵引，只处理“没有在正常向左出膛”的球；
                // 正常发射（vx 约 -1.6 以下）完全不受影响；完全落在台面里的普通球也不会被牵引。
                bool overlapsGateBand = position.x + radius > gateX - halfThickness - 0.01f
                    && position.y > gateBottom && position.y < gateTop;
                if (overlapsGateBand && marble.IsInFlight && !marble.IsCaptured && body != null && vx > -1.0f)
                {
                    Vector2 velocity = body.linearVelocity;
                    velocity.x -= 8f * Time.fixedDeltaTime;
                    body.linearVelocity = velocity;
                    vx = velocity.x;
                }

                bool insideLane = position.x > gateX;
                bool movingOut = vx < -0.02f;

                if (_crossing.Contains(ball))
                {
                    // 只有完全位于闸门一侧时才结束穿透，避免碰撞体仍重叠时恢复碰撞。
                    bool fullyOutside = position.x + radius <= gateX - halfThickness - 0.01f;
                    bool fullyInside = position.x - radius >= gateX + halfThickness + 0.01f;
                    if (fullyOutside || (fullyInside && !movingOut))
                    {
                        _crossing.Remove(ball);
                        Physics2D.IgnoreCollision(_gate, ball, false);
                    }
                    else
                    {
                        Physics2D.IgnoreCollision(_gate, ball, true);
                    }
                    continue;
                }

                if (insideLane && movingOut)
                {
                    _crossing.Add(ball);
                    Physics2D.IgnoreCollision(_gate, ball, true);
                }
                else
                {
                    Physics2D.IgnoreCollision(_gate, ball, false);
                }
            }
        }
    }
}
