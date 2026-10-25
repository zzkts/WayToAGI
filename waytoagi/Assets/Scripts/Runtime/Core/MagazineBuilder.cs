using System.Collections.Generic;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 发射阶段的弹仓规则：
    /// 商店阶段购买的所有付费弹珠全部按配置顺序装填，最后补上本阶段免费的标准弹珠。
    /// 弹仓在进入发射阶段时一次性形成，发射一颗扣除一颗；打空后回到商店阶段。
    /// </summary>
    public static class MagazineBuilder
    {
        public static List<BallDef> Build(GameConfig config, SaveData save)
        {
            var magazine = new List<BallDef>();
            if (config == null || config.balls == null) return magazine;

            BallDef standard = null;
            foreach (var ball in config.balls)
            {
                if (ball == null) continue;
                if (ball.alwaysAvailable)
                {
                    standard = ball;
                    continue;
                }

                int count = save != null ? save.GetStock(ball.id) : 0;
                for (int i = 0; i < count; i++) magazine.Add(ball);
            }

            if (standard != null)
            {
                int freeCount = config.economy != null ? config.economy.freeStandardBallsPerPhase : 0;
                for (int i = 0; i < freeCount; i++) magazine.Add(standard);

                // 分裂技能：发射阶段额外生成标准分裂弹，不消耗商店库存。
                int splitBalls = save != null ? Mathf.Clamp(save.splitLevel, 0, UpgradeCatalog.MaxBallSkillLevel) : 0;
                for (int i = 0; i < splitBalls; i++) magazine.Add(standard);
            }
            return magazine;
        }

        public static Dictionary<string, int> CountByBall(IEnumerable<BallDef> magazine)
        {
            var counts = new Dictionary<string, int>();
            if (magazine == null) return counts;
            foreach (var ball in magazine)
            {
                if (ball == null) continue;
                counts.TryGetValue(ball.id, out int current);
                counts[ball.id] = current + 1;
            }
            return counts;
        }
    }
}
