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
        public int chainMultiplier;
        public int chainCount;
        public bool feverActive;
        public int feverMultiplier;
        public int jackpotBonus;
        public int emberBonus;
        public int bountyCredits;
        public int subtotal;
        public int total;
        public bool isNewBest;

        public string Formula => $"{ballMultiplier} × {holeMultiplier} × {holePoints} = {subtotal}";

        public string MultiplierText
        {
            get
            {
                string text = "";
                if (comboMultiplier > 1) text = $"同洞 ×{comboMultiplier}";
                if (chainCount > 0)
                {
                    if (text.Length > 0) text += "  ·  ";
                    text += $"连锁 ×{chainMultiplier}";
                }
                if (feverActive)
                {
                    if (text.Length > 0) text += "  ·  ";
                    text += $"狂热 ×{feverMultiplier}";
                }
                if (jackpotBonus > 0)
                {
                    if (text.Length > 0) text += "  ·  ";
                    text += $"头奖 +{jackpotBonus:N0}";
                }
                if (emberBonus > 0)
                {
                    if (text.Length > 0) text += "  ·  ";
                    text += $"余烬爆发 +{emberBonus:N0}";
                }
                if (bountyCredits > 0)
                {
                    if (text.Length > 0) text += "  ·  ";
                    text += $"赏金 +{bountyCredits:N0}";
                }
                return text;
            }
        }
    }

    /// <summary>
    /// 纯规则层：负责得分公式、同洞连击、全局连锁、狂热与 7 号洞头奖。
    /// 所有冲分数值均由 ScoreRules 硬编码。
    /// </summary>
    public class ScoreManager
    {
        private readonly SaveData _save;
        private int _chainCount;
        private bool _feverActive;
        private bool _jackpotAwarded;
        private int _emberStacks;

        public string LastHoleId { get; private set; }
        public int Streak { get; private set; }
        public int ChainCount => _chainCount;
        public int EmberStacks => _emberStacks;
        public int ChainMultiplier => _chainCount <= 0 ? 1 : Mathf.Min(_chainCount, ScoreRules.ChainCap);
        public bool FeverActive => _feverActive;
        public int FeverMultiplier => _feverActive ? ScoreRules.FeverMultiplier : 1;
        public int TotalScore => _save.totalScore;
        public int Credits => _save.credits;
        public int BestSingleScore => _save.bestSingleScore;
        public int BestVolleyScore => _save.bestVolleyScore;
        public SaveData Save => _save;

        public ScoreManager(SaveData save)
        {
            _save = save;
            LastHoleId = null;
            Streak = 0;
            _chainCount = 0;
            _feverActive = false;
            _jackpotAwarded = false;
        }

        public int ComboMultiplier
        {
            get
            {
                if (Streak <= 1) return 1;
                int exponent = Mathf.Min(Streak - 1, 3);
                return 1 << exponent;
            }
        }

        public ScoreResult Score(BallDef ball, HoleDef hole, int currentHolePoints)
        {
            if (ball == null || hole == null)
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

            _chainCount++;
            if (_chainCount >= ScoreRules.FeverThreshold) _feverActive = true;

            int combo = ComboMultiplier;
            int chain = ChainMultiplier;
            int fever = FeverMultiplier;
            int subtotal = ball.multiplier * hole.multiplier * Mathf.Max(0, currentHolePoints);

            if (_save.emberLevel > 0) _emberStacks++;
            int emberBonus = 0;
            if (_emberStacks >= UpgradeCatalog.EmberThreshold)
            {
                _emberStacks -= UpgradeCatalog.EmberThreshold;
                emberBonus = UpgradeCatalog.EmberBurstBase(_save.emberLevel) * ball.multiplier;
            }

            int jackpot = 0;
            if (hole.id == "hole_07" && !_jackpotAwarded)
            {
                _jackpotAwarded = true;
                int jackpotSkill = _save.GetHoleSkillLevel("hole_07");
                jackpot = Mathf.RoundToInt(ScoreRules.JackpotBonusFor(ball) *
                                            UpgradeCatalog.JackpotMultiplier(jackpotSkill));
            }

            int total = subtotal * combo * chain * fever + jackpot + emberBonus;
            bool newBest = total > _save.bestSingleScore;
            _save.totalScore += total;
            int baseCredits = Mathf.RoundToInt(ScoreRules.CreditsFor(total) *
                                               UpgradeCatalog.GildCreditMultiplier(_save.gildLevel));
            int bountyCredits = 0;
            if (hole.id == "hole_01")
            {
                bountyCredits = UpgradeCatalog.BountyCredits(_save.GetHoleSkillLevel("hole_01"), ball);
            }
            _save.credits += baseCredits + bountyCredits;
            if (newBest) _save.bestSingleScore = total;

            var result = new ScoreResult
            {
                ball = ball,
                hole = hole,
                ballMultiplier = ball.multiplier,
                holeMultiplier = hole.multiplier,
                holePoints = currentHolePoints,
                comboMultiplier = combo,
                comboCount = Streak,
                chainMultiplier = chain,
                chainCount = _chainCount,
                feverActive = _feverActive,
                feverMultiplier = fever,
                jackpotBonus = jackpot,
                emberBonus = emberBonus,
                bountyCredits = bountyCredits,
                subtotal = subtotal,
                total = total,
                isNewBest = newBest
            };

            return result;
        }

        /// <summary>漏掉一颗球：同洞连击与连锁一起中断，狂热熄灭。</summary>
        public void RegisterMiss()
        {
            LastHoleId = null;
            Streak = 0;
            _chainCount = 0;
            _feverActive = false;
        }

        /// <summary>进入新的发射阶段时清空本轮所有连锁状态。</summary>
        public void ResetCombo()
        {
            RegisterMiss();
            _jackpotAwarded = false;
            _emberStacks = 0;
        }

        /// <summary>额外奖励加分（全中奖励等），同样按硬编码比例折算积分。</summary>
        public void AwardBonus(int points)
        {
            if (points <= 0) return;
            _save.totalScore += points;
            _save.credits += ScoreRules.CreditsFor(points);
        }

        /// <summary>记录本轮齐射得分，返回是否刷新最高单轮记录。</summary>
        public bool RecordVolleyScore(int volleyScore)
        {
            if (volleyScore <= _save.bestVolleyScore) return false;
            _save.bestVolleyScore = volleyScore;
            return true;
        }
    }
}
