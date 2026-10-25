using System;
using System.Collections;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 单向闸门的真实物理验收：-gate-test 启动时在菜单状态直接给弹珠赋速度，
    /// 验证“台面球向右撞闸门会被挡回、发射道球向左能正常穿过”。
    /// 不依赖玩家流程，也不会修改存档；测试结束直接退出并返回 0/1。
    /// </summary>
    public class GatePhysicsProbe : MonoBehaviour
    {
        public static bool IsRequested
        {
            get
            {
                foreach (var arg in Environment.GetCommandLineArgs())
                {
                    if (string.Equals(arg, "-gate-test", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(0.6f);

            var gate = UnityEngine.Object.FindFirstObjectByType<OneWayLaneGate>();
            var marbles = UnityEngine.Object.FindObjectsByType<MarbleController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (gate == null || marbles == null || marbles.Length == 0)
            {
                Debug.LogError($"[GateTest] FAIL missing gate={gate == null} marbles={marbles?.Length ?? 0}");
                Application.Quit(1);
                yield break;
            }

            var marble = marbles[0];
            int failures = 0;
            int total = 0;

            // 台面侧向右撞闸门：必须在闸门外被挡回，不能进入发射道。
            float[] inboundY = { 2.50f, 3.40f, 4.00f, 5.00f };
            for (int i = 0; i < inboundY.Length; i++)
            {
                total++;
                float y = inboundY[i];
                marble.PrepareAt(new Vector2(2.00f, y));
                marble.Launch(new Vector2(9f, 0f));
                yield return new WaitForSeconds(0.20f);
                float x = marble.transform.position.x;
                bool pass = x < 2.58f;
                Debug.Log($"[GateTest] inbound y={y:F2} x={x:F3} {(pass ? "PASS" : "FAIL")}");
                if (!pass) failures++;
                marble.HideImmediate();
            }

            // 发射道内向左出膛：必须穿过闸门进入台面。
            float[] outboundY = { 2.50f, 3.40f, 4.00f, 5.00f };
            for (int i = 0; i < outboundY.Length; i++)
            {
                total++;
                float y = outboundY[i];
                marble.PrepareAt(new Vector2(3.20f, y));
                marble.Launch(new Vector2(-7f, 0f));
                yield return new WaitForSeconds(0.20f);
                float x = marble.transform.position.x;
                bool pass = x < 2.55f;
                Debug.Log($"[GateTest] outbound y={y:F2} x={x:F3} {(pass ? "PASS" : "FAIL")}");
                if (!pass) failures++;
                marble.HideImmediate();
            }

            // 真实齐射的最低横向速度 / 上仰角，确认出膛不会被闸门挡住。
            total++;
            marble.PrepareAt(new Vector2(3.19f, BoardGeometry.LauncherSeatY));
            marble.Launch(new Vector2(-1.70f, 7.0f));
            yield return new WaitForSeconds(0.50f);
            float rackX = marble.transform.position.x;
            bool rackPass = rackX < 2.55f;
            Debug.Log($"[GateTest] rack-launch x={rackX:F3} {(rackPass ? "PASS" : "FAIL")}");
            if (!rackPass) failures++;
            marble.HideImmediate();

            // 高速回弹球：用连续碰撞检测确认不会穿透薄闸门。
            total++;
            marble.PrepareAt(new Vector2(1.80f, 3.40f));
            marble.Launch(new Vector2(14f, 0f));
            yield return new WaitForSeconds(0.20f);
            float fastX = marble.transform.position.x;
            bool fastPass = fastX < 2.58f;
            Debug.Log($"[GateTest] fast-inbound x={fastX:F3} {(fastPass ? "PASS" : "FAIL")}");
            if (!fastPass) failures++;
            marble.HideImmediate();

            // 斜向回弹：模拟从上方弹回发射道的情况。
            total++;
            marble.PrepareAt(new Vector2(2.10f, 5.00f));
            marble.Launch(new Vector2(4f, -3f));
            yield return new WaitForSeconds(0.25f);
            float diagX = marble.transform.position.x;
            bool diagPass = diagX < 2.58f;
            Debug.Log($"[GateTest] diagonal-inbound x={diagX:F3} {(diagPass ? "PASS" : "FAIL")}");
            if (!diagPass) failures++;
            marble.HideImmediate();

            // 同一颗球先出闸、再从台面反弹回来：闸门必须已恢复碰撞，不能二次穿入。
            total++;
            marble.PrepareAt(new Vector2(3.20f, 3.40f));
            marble.Launch(new Vector2(-7f, 0f));
            yield return new WaitForSeconds(0.20f);
            float exitX = marble.transform.position.x;
            marble.Launch(new Vector2(7f, 0f));
            yield return new WaitForSeconds(0.25f);
            float reboundX = marble.transform.position.x;
            bool reboundPass = exitX < 2.55f && reboundX < 2.58f;
            Debug.Log($"[GateTest] exit-then-rebound exitX={exitX:F3} reboundX={reboundX:F3} {(reboundPass ? "PASS" : "FAIL")}");
            if (!reboundPass) failures++;
            marble.HideImmediate();

            // 底部侧向：隔板已封到底墙，台面低处的球也不能从下方钻进弹簧下方。
            total++;
            marble.PrepareAt(new Vector2(2.00f, -1.00f));
            marble.Launch(new Vector2(9f, 0f));
            yield return new WaitForSeconds(0.20f);
            float bottomX = marble.transform.position.x;
            bool bottomPass = bottomX < 2.50f;
            Debug.Log($"[GateTest] bottom-side x={bottomX:F3} {(bottomPass ? "PASS" : "FAIL")}");
            if (!bottomPass) failures++;
            marble.HideImmediate();

            // 极端倾斜：球在发射道里被向右压住（甚至接近静止）时，
            // 闸门的出膛牵引必须把它推回台面，不能停在弹簧上方。
            total++;
            marble.PrepareAt(new Vector2(2.80f, 3.40f));
            marble.Launch(new Vector2(0f, 1.0f));
            yield return new WaitForSeconds(0.55f);
            float heldX = marble.transform.position.x;
            bool heldPass = heldX < 2.55f;
            Debug.Log($"[GateTest] held-inbound x={heldX:F3} {(heldPass ? "PASS" : "FAIL")}");
            if (!heldPass) failures++;
            marble.HideImmediate();

            Debug.Log($"[GateTest] RESULT {total - failures}/{total} passed, failures={failures}");
            yield return new WaitForSeconds(0.2f);
            Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
