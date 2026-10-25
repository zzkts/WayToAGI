using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    public static class BoardGeometry
    {
        public const float PlayHalfWidth = 3.55f;
        public const float PlayTop = 5.52f;
        public const float PlayBottom = -5.60f;
        public const float LauncherWallX = 2.62f;
        public const float LauncherWallTop = 3.10f;
        public const float LauncherWallBottom = PlayBottom;
        public const float LauncherGateBottom = 2.05f;
        public const float LauncherGateTop = PlayTop;
        public const float LauncherX = 3.06f;
        public const float LauncherSeatY = 2.35f;
        public const float HoleCenterY = -4.93f;
        public static readonly Vector2 BallSeat = new Vector2(LauncherX, LauncherSeatY);

        /// <summary>齐射待发射槽：最多 2 列、多行的紧凑弹珠架。</summary>
        public static Vector2 VolleyRackPosition(int index, int count)
        {
            count = Mathf.Max(1, count);
            index = Mathf.Clamp(index, 0, count - 1);
            int row = index / 2;
            bool oddLast = (count % 2 == 1) && (index == count - 1);
            float x = oddLast ? LauncherX : LauncherX + (index % 2 == 0 ? -0.13f : 0.13f);
            float y = LauncherSeatY + row * 0.225f;
            return new Vector2(x, y);
        }
    }

    public class BoardRefs
    {
        public Transform Root;
        public Transform Playfield;
        public Transform Effects;
        public Canvas WorldCanvas;
        public RectTransform WorldRoot;
        public Camera Camera;
        public RewardHole[] Holes;
        public List<PinView> Pins = new List<PinView>();
        public List<Baffle> Baffles = new List<Baffle>();
        public Text LauncherHint;
        public Bounds VisualBounds;
        public OneWayLaneGate LaneGate;
    }

    public static class BoardBuilder
    {
        private static PhysicsMaterial2D _pinMaterial;
        private static PhysicsMaterial2D _wallMaterial;
        private static PhysicsMaterial2D _barMaterial;
        private static PhysicsMaterial2D _baffleMaterial;

        public static BoardRefs Build(GameConfig config, Transform parent, Camera camera)
        {
            var refs = new BoardRefs { Camera = camera };
            var root = new GameObject("Board").transform;
            root.SetParent(parent, false);
            refs.Root = root;
            // 上方半圆凹槽的顶点在 y≈8.605，机柜和相机取景需要比旧版更高。
            refs.VisualBounds = new Bounds(new Vector3(0f, 1.45f, 0f), new Vector3(8.1f, 15.7f, 0.1f));

            _pinMaterial = new PhysicsMaterial2D("PinMaterial") { bounciness = config.physics.pinBounciness, friction = 0.04f };
            _wallMaterial = new PhysicsMaterial2D("WallMaterial") { bounciness = config.physics.wallBounciness, friction = 0.08f };
            _barMaterial = new PhysicsMaterial2D("BarMaterial") { bounciness = config.physics.barBounciness, friction = 0.06f };
            // 挡板用低弹跳、高摩擦材质，尽量让回落的弹珠沿导流板滑入槽口而不是弹回发射道。
            _baffleMaterial = new PhysicsMaterial2D("BaffleMaterial") { bounciness = 0.02f, friction = 0.35f };

            BuildBackground(root);
            BuildCabinet(root);
            BuildRails(root);
            BuildLauncherGeometry(root, config);
            refs.LaneGate = BuildLaneGate(root);

            refs.Playfield = new GameObject("Playfield").transform;
            refs.Playfield.SetParent(root, false);
            refs.Effects = new GameObject("Effects").transform;
            refs.Effects.SetParent(root, false);

            BuildPins(refs.Playfield);
            BuildBaffles(refs.Playfield, config);
            BuildBottomRamps(refs.Playfield);

            BuildWorldLabels(refs, camera);
            BuildHoles(refs, config);

            return refs;
        }

        private static void BuildBackground(Transform root)
        {
            var glow = Theme.SpriteObject(root, "AmbientGlow", Theme.Glow, Theme.Amber.Alpha(0.10f), -220, Vector2.zero);
            SetSimpleDiameter(glow, 16f);
            var glow2 = Theme.SpriteObject(root, "AmbientGlow2", Theme.Glow, Theme.Teal.Alpha(0.06f), -221, new Vector2(0, -3.2f));
            SetSimpleDiameter(glow2, 11f);

            // 台面外的装饰圆点，为横屏两侧留白增加一点街机灯牌的感觉。
            var dotRoot = new GameObject("BackgroundDots").transform;
            dotRoot.SetParent(root, false);
            Color[] colors = { Theme.Amber, Theme.TealBright, Theme.Orange };
            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f + 0.27f;
                float radiusX = 5.4f + (i % 3) * 0.85f;
                float radiusY = 7.0f + (i % 4) * 0.75f;
                var pos = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                float d = 0.12f + (i % 4) * 0.08f;
                var dot = Theme.CircleObject(dotRoot, "Dot", pos, d, colors[i % colors.Length].Alpha(0.10f), -210);
                dot.transform.localRotation = Quaternion.Euler(0, 0, i * 23f);
            }
        }

        private static void BuildCabinet(Transform root)
        {
            // 机柜整体向上加高，容纳上边框的半圆凹槽（顶点约 y=8.605）。
            Theme.RoundedRect(root, "BoardShadow", new Vector2(0.18f, 1.44f), new Vector2(8.02f, 15.53f), Theme.Shadow, 50, -100);
            Theme.RoundedRect(root, "CabinetOuter", new Vector2(0f, 1.505f), new Vector2(7.88f, 15.29f), Theme.Hex("#3A281C"), 50, -95);
            Theme.RoundedRect(root, "CabinetRim", new Vector2(0f, 1.525f), new Vector2(7.68f, 15.11f), Theme.Hex("#A66C35"), 47, -94);
            Theme.RoundedRect(root, "CabinetInner", new Vector2(0f, 1.525f), new Vector2(7.48f, 14.93f), Theme.Cream, 44, -93);
            Theme.RoundedRect(root, "PlaySurface", new Vector2(0f, 1.515f), new Vector2(7.12f, 14.37f), Theme.Hex("#F9EDD9"), 28, -90);

            // 顶边以上到半圆弧之间仍是机柜木框；半圆内部用与台面相同的颜色填满，
            // 形成真正可以进球的半圆凹槽，而不是一条悬空的弧线。
            // 凹槽的直径与 BuildRails 中的弧墙使用同一组边界计算，避免视觉和碰撞尺寸漂移。
            float domeLeftX = -BoardGeometry.PlayHalfWidth;
            float domeRightX = BoardGeometry.LauncherWallX;
            float domeCenterX = (domeLeftX + domeRightX) * 0.5f;
            float domeDiameter = domeRightX - domeLeftX;
            Theme.RoundedRect(root, "TopFrame", new Vector2(0f, 7.20f), new Vector2(7.48f, 3.50f), Theme.Hex("#A66C35"), 28, -89);
            Theme.CircleObject(root, "TopDomeInterior", new Vector2(domeCenterX, BoardGeometry.PlayTop), domeDiameter, Theme.Hex("#F9EDD9"), -88);

            // 表面淡淡的圆形装饰，像老式街机台面的印刷底纹；必须画在凹槽填色之上，
            // 否则凹槽圆的下半部分会把台面原有的光晕切出一道硬边。
            Theme.CircleObject(root, "SurfaceHalo", new Vector2(-1.7f, 2.1f), 5.8f, Theme.CreamDeep.Alpha(0.32f), -87);
            Theme.CircleObject(root, "SurfaceHalo2", new Vector2(2.1f, -1.4f), 4.0f, Theme.Amber.Alpha(0.045f), -86);
            Theme.CircleObject(root, "SurfaceHalo3", new Vector2(-2.3f, -3.9f), 2.3f, Theme.Teal.Alpha(0.035f), -86);

            Theme.RoundedRect(root, "BottomInset", new Vector2(0f, -3.95f), new Vector2(5.9f, 0.035f), Theme.Ink.Alpha(0.08f), 4, -85);
        }

        private static void BuildRails(Transform root)
        {
            const float railThickness = 0.23f;
            Color railColor = Theme.Hex("#B67B39");

            CreateRail(root, "LeftWall", new Vector2(-BoardGeometry.PlayHalfWidth, -5.52f), new Vector2(-BoardGeometry.PlayHalfWidth, BoardGeometry.PlayTop), railThickness, true, railColor, 8, 0f);
            CreateRail(root, "RightWall", new Vector2(BoardGeometry.PlayHalfWidth, -5.52f), new Vector2(BoardGeometry.PlayHalfWidth, BoardGeometry.PlayTop), railThickness, true, railColor, 8, 0f);

            // 上边框不再是一条直墙：整段台面顶边换成半圆，直径等于左侧台面顶边宽度
            // （左墙 x=-3.55 到发射道隔板 x=2.62，宽 6.17，半径 3.085）。
            // 半圆向上凹：直径落在原顶边高度，弧线向上拱到 y≈8.605，形成朝下开口的半圆凹槽；
            // 弹珠从闸门进入后可以继续向上进入凹槽，沿凹面自然弯曲，而不会被挡在闸门口。
            // 发射道/闸门在 x=2.62 右侧，仍保留直顶墙，不会被圆弧覆盖。
            float arcLeftX = -BoardGeometry.PlayHalfWidth;
            float arcRightX = BoardGeometry.LauncherWallX;
            float arcCenterX = (arcLeftX + arcRightX) * 0.5f;
            float arcRadius = (arcRightX - arcLeftX) * 0.5f;
            const int arcSegments = 48;

            CreateRail(root, "TopWall_Right",
                new Vector2(arcRightX - 0.02f, BoardGeometry.PlayTop),
                new Vector2(BoardGeometry.PlayHalfWidth, BoardGeometry.PlayTop),
                railThickness, true, railColor, 8, 0f);

            // 圆弧用一个连续的 EdgeCollider2D 和一条 LineRenderer 绘制。
            // 不能把圆弧拆成很多短 Rail：短圆角矩形在弧线上会连成一串“珠子”，看起来又像一排钉柱。
            var arcRoot = new GameObject("TopBorderArc");
            arcRoot.transform.SetParent(root, false);
            arcRoot.transform.localPosition = new Vector3(arcCenterX, BoardGeometry.PlayTop, 0f);

            var arcPoints = new Vector2[arcSegments + 1];
            var arcLinePoints = new Vector3[arcSegments + 1];
            for (int i = 0; i <= arcSegments; i++)
            {
                // 0° → 180°：取圆的上半段，最高点约为 y=8.605。
                float angle = Mathf.PI * i / arcSegments;
                var point = new Vector2(Mathf.Cos(angle) * arcRadius, Mathf.Sin(angle) * arcRadius);
                arcPoints[i] = point;
                arcLinePoints[i] = new Vector3(point.x, point.y, 0f);
            }

            var arcCollider = arcRoot.AddComponent<EdgeCollider2D>();
            arcCollider.points = arcPoints;
            // 与 0.20 的可视线宽一致；弧线整体位于原顶边上方，不再和钉柱争空间。
            arcCollider.edgeRadius = 0.10f;
            arcCollider.sharedMaterial = _wallMaterial;

            var arcMaterial = new Material(Shader.Find("Sprites/Default"));
            var arcShadow = CreateArcLine(arcRoot.transform, "TopBorderArcShadow",
                arcLinePoints, 0.27f, Theme.Shadow.Alpha(0.35f), 8, arcMaterial);
            arcShadow.transform.localPosition = new Vector3(0.025f, -0.05f, 0f);
            CreateArcLine(arcRoot.transform, "TopBorderArcVisual",
                arcLinePoints, 0.20f, railColor, 9, arcMaterial);

            // 弧线与直墙的接点各加一颗装饰铆钉，遮住拼接缝并强调这里是弯曲的上边框。
            for (int side = -1; side <= 1; side += 2)
            {
                var cap = new Vector2(arcCenterX + side * arcRadius, BoardGeometry.PlayTop);
                Theme.CircleObject(root, $"TopBorderCap_{side}", cap, 0.30f, Theme.Hex("#6D4A2E"), 10);
                Theme.CircleObject(root, $"TopBorderCapInner_{side}", cap, 0.17f, Theme.Amber, 11);
            }

            CreateRail(root, "BottomWall", new Vector2(-3.48f, -5.62f), new Vector2(3.48f, -5.62f), 0.16f, true, Theme.Hex("#6D4A2E"), 4, 0f);

            // 台面四角的螺丝；上半部随加高的机柜一起上移，落在凹槽上方的木框上。
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    float screwY = sy > 0 ? 8.86f : -5.72f;
                    Theme.CircleObject(root, "Screw", new Vector2(sx * 3.63f, screwY), 0.25f, Theme.Hex("#6D4A2E"), 2);
                    Theme.CircleObject(root, "ScrewHead", new Vector2(sx * 3.63f, screwY), 0.14f, Theme.Amber.Alpha(0.8f), 3);
                    Theme.RoundedRect(root, "ScrewSlot", new Vector2(sx * 3.63f, screwY), new Vector2(0.12f, 0.025f), Theme.Ink.Alpha(0.55f), 2, 4);
                }
            }
        }

        private static void BuildLauncherGeometry(Transform root, GameConfig config)
        {
            // 隔板从底墙一直封到闸门下沿，和上方单向闸门拼成整面墙：
            // 台面里的球无论从上方还是从底部右侧都进不了发射道，弹簧下方不会再接球。
            CreateRail(root, "LauncherDivider", new Vector2(BoardGeometry.LauncherWallX, BoardGeometry.LauncherWallBottom),
                new Vector2(BoardGeometry.LauncherWallX, BoardGeometry.LauncherGateBottom), 0.17f, true, Theme.Teal, 12, 0f);
            // 旧版单球导流屋顶在齐射中会形成可停留的斜台，已移除。
            CreateRail(root, "LauncherAccent", new Vector2(2.72f, 1.15f), new Vector2(2.72f, 2.95f), 0.035f, false,
                Theme.TealBright.Alpha(0.55f), 13, 0f);
            BuildLauncherBaffle(root, config.physics.ballRadius);
        }

        /// <summary>
        /// 弹簧下方挡板：左右两块向内下倾的导流板拼成一个 V 形接球槽，
        /// 接住发射道内异常下落的弹珠，避免它直接砸到弹簧正下方。
        /// 注意：发射道现在由“全高隔板 + 单向闸门”与台面完全隔开，正常流程不会有台面球进入这里；
        /// 本挡板只作为发射道内部异常情况的最后兜底，不承担入口拦截职责。
        /// 两个关键安全尺寸：
        /// 1）槽口实际净宽约 0.30，大于当前弹珠直径 0.20；若配置把 ballRadius 调大，构建时会打印卡球警告；
        /// 2）导流板最高点约 0.787，低于推杆拉到底时的碰撞体底缘 0.875；最高点位于推杆横向范围之外，
        ///    推杆正下方与导流板的实际最小净空约 0.29，不会挡住推杆行程。
        /// 挡板左侧伸入发射道隔板下方，两者碰撞体轻微搭接，避免在接缝处留下可卡球的凹口。
        /// 槽口下方是封闭的发射道底段（8 号洞已移到台面内），因此这里不再是“回到台面”的通道；
        /// 真正的出膛保障是单向闸门对低速球的轻微向左牵引。
        /// </summary>
        private static void BuildLauncherBaffle(Transform root, float ballRadius)
        {
            var left = CreateRail(root, "LauncherBaffle_L",
                new Vector2(2.64f, 0.70f), new Vector2(2.90f, 0.38f), 0.18f, true, Theme.Teal, 10, 0f);
            var right = CreateRail(root, "LauncherBaffle_R",
                new Vector2(3.37f, 0.38f), new Vector2(3.44f, 0.70f), 0.18f, true, Theme.Teal, 10, 0f);

            foreach (var rail in new[] { left, right })
            {
                var box = rail.GetComponent<BoxCollider2D>();
                if (box != null) box.sharedMaterial = _baffleMaterial;
            }

            // 槽口净宽是两块导流板碰撞体之间的最小水平距离；弹珠直径接近该值时容易在槽口来回卡住。
            const float slotClearWidth = 0.298f;
            float ballDiameter = ballRadius * 2f;
            if (slotClearWidth < ballDiameter + 0.04f)
            {
                Debug.LogWarning(
                    $"[Board] 弹簧挡板槽口净宽 {slotClearWidth:F2}，对弹珠直径 {ballDiameter:F2} 余量不足，可能卡球；" +
                    "请加宽槽口或减小 game_config.json 的 physics.ballRadius。");
            }

            // 槽口两侧高亮，提示玩家这里是挡板有意留出的落球口。
            Theme.CircleObject(root, "LauncherBaffleTipL", new Vector2(2.90f, 0.38f), 0.10f,
                Theme.TealBright.Alpha(0.75f), 11);
            Theme.CircleObject(root, "LauncherBaffleTipR", new Vector2(3.37f, 0.38f), 0.10f,
                Theme.TealBright.Alpha(0.75f), 11);
        }

        /// <summary>
        /// 发射道单向闸门：把隔板顶端到顶墙之间的整段开口封住。
        /// 物理碰撞是连续的一整面墙，由 OneWayLaneGate 只对“从发射道内向左出膛”的弹珠放行；
        /// 台面里向右反弹的球会被挡回，不能再从上方掉进发射道落到弹簧下方。
        /// 视觉上用一排斜叶片表示这里是可穿过的单向闸门，而不是普通实墙。
        /// </summary>
        private static OneWayLaneGate BuildLaneGate(Transform root)
        {
            float bottom = BoardGeometry.LauncherGateBottom;
            float top = BoardGeometry.LauncherGateTop;
            float height = top - bottom;

            var gateGo = new GameObject("LauncherGate");
            gateGo.transform.SetParent(root, false);
            gateGo.transform.localPosition = new Vector3(BoardGeometry.LauncherWallX, bottom + height * 0.5f, 0f);

            var box = gateGo.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.12f, height);
            box.sharedMaterial = _wallMaterial;

            var gate = gateGo.AddComponent<OneWayLaneGate>();

            for (int i = 0; i < 9; i++)
            {
                float y = bottom + 0.22f + i * ((height - 0.44f) / 8f);
                var slat = Theme.RoundedRect(root, $"LauncherGateSlat_{i}",
                    new Vector2(BoardGeometry.LauncherWallX, y), new Vector2(0.055f, 0.22f),
                    Theme.TealBright.Alpha(0.42f), 3, 11);
                slat.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            }

            return gate;
        }

        private static void BuildPins(Transform parent)
        {
            // 上半区只保留普通交错钉阵，不再用钉柱拼弧线；弧线属于上边框（见 BuildRails）。
            // 半圆向上拱到原顶边之上，钉阵不需要为弧线让出下方空间；保持五排、
            // 最高一排 y=3.15，给上方半圆凹槽留出完整的飞行空间。
            // 最下一排保持 y=0.60：挡板 A 的顶缘约 -0.83，给钉阵与摆动挡板之间留出穿行空间。
            float[] wideRow = { -3.05f, -1.75f, -0.45f, 0.85f, 2.15f };
            float[] offsetRow = { -2.40f, -1.10f, 0.20f, 1.50f };
            float[] rows = { 3.15f, 2.51f, 1.88f, 1.24f, 0.60f };

            var root = new GameObject("Pins").transform;
            root.SetParent(parent, false);
            int index = 0;
            for (int row = 0; row < rows.Length; row++)
            {
                float[] xs = row % 2 == 0 ? wideRow : offsetRow;
                for (int i = 0; i < xs.Length; i++)
                {
                    CreatePin(root, new Vector2(xs[i], rows[row]), index++);
                }
            }
        }

        private static void CreatePin(Transform parent, Vector2 pos, int index)
        {
            var go = new GameObject($"Pin_{index:00}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.145f;
            collider.sharedMaterial = _pinMaterial;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var shadow = Theme.CircleObject(visual, "Shadow", new Vector2(0.025f, -0.045f), 0.38f, Theme.Shadow.Alpha(0.45f), 20);
            var outline = Theme.CircleObject(visual, "Outline", Vector2.zero, 0.37f, Theme.Hex("#6A4B2E"), 21);
            var inner = Theme.CircleObject(visual, "Inner", Vector2.zero, 0.285f, Theme.Amber, 22);
            Theme.CircleObject(visual, "Highlight", new Vector2(-0.055f, 0.065f), 0.075f, Theme.Cream.Alpha(0.7f), 23);

            var pin = go.AddComponent<PinView>();
            pin.Initialize(visual, inner);
        }

        /// <summary>
        /// 三块横向挡板在钉柱区下方左 / 右 / 中错落分布，纵向间隔约 0.9。
        /// V3 中挡板不再旋转，而是按配置的周期与总幅度左右往返摆动；
        /// 位置与长度经过约束，保证摆到两端时仍留在机台侧壁和发射道隔板之间。
        /// </summary>
        private static void BuildBaffles(Transform parent, GameConfig config)
        {
            CreateBaffle(parent, config, "Baffle_A", new Vector2(-1.55f, -0.95f), 2.40f, 0f);
            CreateBaffle(parent, config, "Baffle_B", new Vector2(1.05f, -1.85f), 1.80f, Mathf.PI * 0.66f);
            CreateBaffle(parent, config, "Baffle_C", new Vector2(-0.10f, -2.80f), 2.50f, Mathf.PI * 1.33f);
        }

        private static void CreateBaffle(Transform parent, GameConfig config, string name, Vector2 pos, float length, float phase)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(length, 0.24f);
            collider.sharedMaterial = _barMaterial;

            Theme.RoundedRect(go.transform, "Shadow", new Vector2(0.03f, -0.06f), new Vector2(length + 0.12f, 0.40f), Theme.Shadow.Alpha(0.45f), 20, 24);
            Theme.RoundedRect(go.transform, "Frame", Vector2.zero, new Vector2(length + 0.10f, 0.36f), Theme.Hex("#2A6670"), 18, 25);
            Theme.RoundedRect(go.transform, "Inner", Vector2.zero, new Vector2(length, 0.24f), Theme.TealBright, 16, 26);
            Theme.CircleObject(go.transform, "CapL", new Vector2(-length * 0.5f, 0f), 0.32f, Theme.Amber, 27);
            Theme.CircleObject(go.transform, "CapR", new Vector2(length * 0.5f, 0f), 0.32f, Theme.Amber, 27);
            Theme.RoundedRect(go.transform, "Hub", Vector2.zero, new Vector2(0.32f, 0.10f), Theme.Ink.Alpha(0.55f), 3, 28);

            var baffle = go.AddComponent<Baffle>();
            baffle.Initialize(config.baffle.amplitude, config.baffle.period, phase);
        }

        private static void BuildBottomRamps(Transform parent)
        {
            CreateRail(parent, "LeftFunnel", new Vector2(-3.43f, -3.30f), new Vector2(-2.95f, -4.33f), 0.16f, true,
                Theme.Hex("#C68B4A"), 9, 0f);
            CreateRail(parent, "RightFunnel", new Vector2(3.43f, -3.30f), new Vector2(2.95f, -4.33f), 0.16f, true,
                Theme.Hex("#C68B4A"), 9, 0f);
        }

        private static void BuildWorldLabels(BoardRefs refs, Camera camera)
        {
            var go = new GameObject("WorldLabels", typeof(RectTransform));
            go.transform.SetParent(refs.Root, false);
            var rect = (RectTransform)go.transform;
            // 画布高度跟着加高的机柜一起增加，顶部铭牌抬到半圆凹槽上方仍不会被裁掉。
            rect.sizeDelta = new Vector2(750f, 1900f);
            rect.localScale = Vector3.one * 0.01f;
            rect.localPosition = Vector3.zero;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 300;
            refs.WorldCanvas = canvas;
            refs.WorldRoot = rect;

            // 台面顶部的铭牌文字：放在半圆凹槽顶点（y≈8.605）上方的木框上。
            UIFactory.Image(rect, "Plaque", Theme.Rounded(16), Theme.Hex("#4A3526"), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 886f), new Vector2(280f, 38f));
            UIFactory.Label(rect, "Brand", "PACHINKO  ·  弹珠机", 19, Theme.Cream, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 886f), new Vector2(300f, 38f), true);
        }

        private static void BuildHoles(BoardRefs refs, GameConfig config)
        {
            // V3 策划案 5.2 明确把“三个奖励洞（×1/1000、×3/5000、×5/20000）”标为废案，
            // 并要求采用原有设计，因此这里保留 V2 的 8 洞布局（4 奖励洞 + 4 空穴）。
            var holeRoot = new GameObject("Holes").transform;
            holeRoot.SetParent(refs.Root, false);
            refs.Holes = new RewardHole[config.holes.Length];

            int holeCount = config.holes.Length;
            float xSpan = holeCount > 1 ? config.holes[holeCount - 1].positionX - config.holes[0].positionX : 1.18f;
            float bayStep = holeCount > 1 ? Mathf.Abs(xSpan) / (holeCount - 1) : 1.18f;
            // 八个洞全部收在发射道隔板左侧的台面内；洞距约 0.73，洞宽约 0.67。
            // 这样 8 号空穴不会再落在弹簧/发射道下方，台面里也没有通往弹簧下方的洞。
            float bayWidth = Mathf.Clamp(bayStep - 0.06f, 0.60f, 2.4f);
            float ringSize = Mathf.Clamp(bayWidth * 0.86f, 0.62f, 1.28f);
            float mouthSize = Mathf.Clamp(bayWidth * 0.72f, 0.50f, 1.10f);
            float innerSize = Mathf.Clamp(bayWidth * 0.56f, 0.38f, 0.84f);
            int labelWidth = Mathf.Max(64, Mathf.RoundToInt(bayWidth * 100f) - 4);

            for (int i = 0; i < holeCount; i++)
            {
                var hole = config.holes[i];
                float x = hole.positionX;
                var color = Theme.Hex(hole.color);
                bool reward = hole.HasReward;
                var ringColor = reward ? Theme.Darken(color, 0.06f) : Theme.Hex("#5B5148");

                Theme.RoundedRect(holeRoot, $"Bay_{i}", new Vector2(x, -4.99f), new Vector2(bayWidth, 1.10f),
                    color.Alpha(reward ? 0.16f : 0.07f), 16, 2);
                Theme.RoundedRect(holeRoot, $"BayInner_{i}", new Vector2(x, -5.07f), new Vector2(bayWidth - 0.08f, 0.82f),
                    Theme.Ink.Alpha(0.075f), 14, 3);
                Theme.RoundedRect(holeRoot, $"BayEdge_{i}", new Vector2(x, -4.42f), new Vector2(bayWidth - 0.10f, 0.045f),
                    color.Alpha(reward ? 0.65f : 0.28f), 3, 6);

                SpriteRenderer glow = null;
                if (reward)
                {
                    glow = Theme.SpriteObject(holeRoot, $"HoleGlow_{i}", Theme.Glow, color.Alpha(0.3f), 5, new Vector2(x, -4.82f));
                    SetSimpleDiameter(glow, Mathf.Min(2.15f, bayWidth * 1.75f));
                }
                Theme.CircleObject(holeRoot, $"HoleRing_{i}", new Vector2(x, BoardGeometry.HoleCenterY), ringSize, ringColor, 8);
                Theme.CircleObject(holeRoot, $"HoleMouth_{i}", new Vector2(x, BoardGeometry.HoleCenterY), mouthSize, Theme.Ink, 9);
                Theme.CircleObject(holeRoot, $"HoleInner_{i}", new Vector2(x, BoardGeometry.HoleCenterY), innerSize, Theme.Hex("#1D130D"), 10);
                Theme.CircleObject(holeRoot, $"HoleSheen_{i}", new Vector2(x - bayWidth * 0.11f, BoardGeometry.HoleCenterY + bayWidth * 0.14f),
                    Mathf.Max(0.12f, bayWidth * 0.16f), color.Alpha(reward ? 0.28f : 0.12f), 11);

                UIFactory.Label(refs.WorldRoot, $"HoleNumber_{i}", (i + 1).ToString(), 26,
                    Theme.Cream.Alpha(reward ? 0.88f : 0.42f), TextAnchor.MiddleCenter, UIFactory.Center,
                    new Vector2(x * 100f, BoardGeometry.HoleCenterY * 100f), new Vector2(50f, 40f), true);
                UIFactory.Image(refs.WorldRoot, $"HolePill_{i}", Theme.Rounded(12),
                    Theme.Ink.Alpha(reward ? 0.9f : 0.4f), UIFactory.Center, UIFactory.Center,
                    new Vector2(x * 100f, -540f), new Vector2(labelWidth, 28f));
                var label = UIFactory.Label(refs.WorldRoot, $"HoleLabel_{i}", "", 13,
                    Theme.Cream.Alpha(reward ? 1f : 0.62f), TextAnchor.MiddleCenter,
                    UIFactory.Center, new Vector2(x * 100f, -540f), new Vector2(labelWidth, 28f), true);

                var trigger = new GameObject($"HoleTrigger_{i}");
                trigger.transform.SetParent(holeRoot, false);
                trigger.transform.localPosition = new Vector3(x, -5.03f, 0f);
                var collider = trigger.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(bayWidth, 1.16f);
                var rewardHole = trigger.AddComponent<RewardHole>();
                rewardHole.Initialize(hole, hole.basePoints, label, glow);
                refs.Holes[i] = rewardHole;
            }

            for (int i = 0; i < config.holes.Length - 1; i++)
            {
                float boundary = (config.holes[i].positionX + config.holes[i + 1].positionX) * 0.5f;
                Theme.RoundedRect(holeRoot, $"Divider_{i}", new Vector2(boundary, -5.02f), new Vector2(0.03f, 1.18f), Theme.Ink.Alpha(0.2f), 2, 4);
            }
        }

        private static GameObject CreateRail(Transform parent, string name, Vector2 a, Vector2 b, float thickness, bool collider,
            Color color, int order, float unused)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (a + b) * 0.5f;
            go.transform.localRotation = Quaternion.Euler(0, 0, angle);

            var shadowGo = new GameObject("RailShadow");
            shadowGo.transform.SetParent(go.transform, false);
            shadowGo.transform.localPosition = new Vector3(0.025f, -0.05f, 0f);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = Theme.Rounded(12);
            shadow.drawMode = SpriteDrawMode.Sliced;
            shadow.size = new Vector2(length + 0.05f, thickness + 0.07f);
            shadow.color = Theme.Shadow.Alpha(0.35f);
            shadow.sortingOrder = order - 1;

            var visual = new GameObject("RailVisual");
            visual.transform.SetParent(go.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Theme.Rounded(12);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(length, thickness);
            renderer.color = color;
            renderer.sortingOrder = order;

            if (collider)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(length + thickness * 0.6f, Mathf.Max(0.10f, thickness * 0.8f));
                box.sharedMaterial = _wallMaterial;
            }
            return go;
        }

        private static LineRenderer CreateArcLine(Transform parent, string name, Vector3[] points,
            float width, Color color, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 8;
            line.numCornerVertices = 4;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = order;
            line.generateLightingData = false;
            return line;
        }

        private static void SetSimpleDiameter(SpriteRenderer renderer, float diameter)
        {
            if (renderer == null) return;
            float size = renderer.sprite != null ? renderer.sprite.bounds.size.x : 1.28f;
            float scale = diameter / Mathf.Max(0.0001f, size);
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
