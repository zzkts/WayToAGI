using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 冲分模式的核心规则，全部硬编码，方便直接调手感：
    /// 连续命中累积连锁，第 5 次起进入狂热；全中追加 50% 奖励；7 号洞每轮首次命中触发头奖。
    /// </summary>
    public static class ScoreRules
    {
        public const int ChainCap = 5;
        public const int FeverThreshold = 5;
        public const int FeverMultiplier = 2;
        public const float AllHitBonusRate = 0.5f;
        public const float CreditRate = 0.2f;
        public const int JackpotBaseBonus = 10000;

        /// <summary>累计得分里程碑，达到后弹出冲分结算，然后继续无尽模式。</summary>
        public static readonly int[] Milestones =
        {
            250000,
            1000000,
            2500000,
            5000000,
            10000000,
            25000000,
            50000000
        };

        public static int MilestoneCount => Milestones.Length;

        public static int MilestoneAt(int index)
        {
            if (Milestones.Length == 0) return 0;
            return Milestones[Mathf.Clamp(index, 0, Milestones.Length - 1)];
        }

        public static int CreditsFor(int score)
        {
            return Mathf.RoundToInt(Mathf.Max(0, score) * CreditRate);
        }

        public static int AllHitBonusFor(int volleyScore)
        {
            return Mathf.RoundToInt(Mathf.Max(0, volleyScore) * AllHitBonusRate);
        }

        public static int JackpotBonusFor(BallDef ball)
        {
            int multiplier = ball != null ? Mathf.Max(1, ball.multiplier) : 1;
            return JackpotBaseBonus * multiplier;
        }
    }
}
