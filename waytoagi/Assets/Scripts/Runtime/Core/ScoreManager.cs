using UnityEngine;

namespace Pachinko
{
    public class ScoreResult
    {
        public BallDef ball;
        public HoleDef hole;
        public int ballMultiplier;
        public int holeMultiplier;
        public int holePoints;
        public int comboMultiplier;
        public int comboCount;
        public int subtotal;
        public int total;
        public bool isNewBest;

        public string Formula => $"{ballMultiplier} × {holeMultiplier} × {holePoints} = {subtotal:N0}";

        public string MultiplierText => comboMultiplier > 1
            ? $"同洞连击 ×{comboMultiplier}（第 {comboCount} 连）"
            : "同洞连击 ×1";
    }

    /// <summary>
    /// 纯计分层：负责 V3 的基础公式与同洞连击。
    /// 弹珠倍率来自当前附魔，洞口倍率与基础分来自配置，额外系统（连锁/狂热/头奖）已移除。
    /// </summary>
    public class ScoreManager
    {
        private readonly SaveData _save;

        public string LastHoleId { get; private set; }
        public int Streak { get; private set; }
        public int TotalScore => _save.totalScore;
        public int Credits => _save.credits;
        public int BestSingleScore => _save.bestSingleScore;
        public int BestVolleyScore => _save.bestVolleyScore;
        public int BestLoopScore => _save.bestLoopScore;
        public SaveData Save => _save;

        public ScoreManager(SaveData save)
        {
            _save = save;
            LastHoleId = null;
            Streak = 0;
        }

        public int ComboMultiplier => ScoreRules.ComboMultiplierFor(Streak);

        /// <summary>结算一颗落入奖励洞的弹珠，并同步累加累计得分与可用积分。</summary>
        public ScoreResult Score(BallDef ball, HoleDef hole, int ballMultiplier, int currentHolePoints)
        {
            if (ball == null || hole == null || !hole.HasReward)
            {
                return new ScoreResult();
            }

            if (LastHoleId == hole.id)
            {
                Streak++;
            }
            else
            {
                LastHoleId = hole.id;
                Streak = 1;
            }

            int safeBallMultiplier = Mathf.Max(1, ballMultiplier);
            int safeHoleMultiplier = Mathf.Max(0, hole.multiplier);
            int safePoints = Mathf.Max(0, currentHolePoints);
            int combo = ComboMultiplier;
            int subtotal = safeBallMultiplier * safeHoleMultiplier * safePoints;
            int total = subtotal * combo;

            bool newBest = total > _save.bestSingleScore;
            _save.totalScore += total;
            _save.credits += ScoreRules.CreditsFor(total);
            if (newBest) _save.bestSingleScore = total;

            return new ScoreResult
            {
                ball = ball,
                hole = hole,
                ballMultiplier = safeBallMultiplier,
                holeMultiplier = safeHoleMultiplier,
                holePoints = safePoints,
                comboMultiplier = combo,
                comboCount = Streak,
                subtotal = subtotal,
                total = total,
                isNewBest = newBest
            };
        }

        /// <summary>漏球或落入空穴：同洞连击中断。</summary>
        public void RegisterMiss()
        {
            LastHoleId = null;
            Streak = 0;
        }

        /// <summary>新的循环/新的尝试开始时清空连击。</summary>
        public void ResetCombo()
        {
            RegisterMiss();
        }

        /// <summary>记录本次齐射得分，返回是否刷新最高单次齐射。</summary>
        public bool RecordVolleyScore(int volleyScore)
        {
            if (volleyScore <= _save.bestVolleyScore) return false;
            _save.bestVolleyScore = volleyScore;
            return true;
        }

        /// <summary>记录本次循环尝试得分，返回是否刷新最高循环得分。</summary>
        public bool RecordLoopScore(int loopScore)
        {
            if (loopScore <= _save.bestLoopScore) return false;
            _save.bestLoopScore = loopScore;
            return true;
        }
    }
}
