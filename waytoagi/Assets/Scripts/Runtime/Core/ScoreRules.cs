using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// V3 计分规则：本次得分 = 弹珠倍率 × 奖励洞倍率 × 洞口基础积分 × 同洞连击。
    /// 同洞连击从 ×1 开始，连续命中同一奖励洞依次 ×2 / ×4 / ×8，封顶 ×8；
    /// 落入别的奖励洞、空穴或漏球都会归零。
    /// 得分按 1:1 同时计入累计得分与可用积分，兑换只扣可用积分。
    /// </summary>
    public static class ScoreRules
    {
        public const int ComboCap = 8;

        public static int ComboMultiplierFor(int streak)
        {
            if (streak <= 1) return 1;
            int exponent = Mathf.Min(streak - 1, 3);
            return Mathf.Min(1 << exponent, ComboCap);
        }

        public static int CreditsFor(int score)
        {
            return Mathf.Max(0, score);
        }
    }
}
