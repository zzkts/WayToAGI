using System;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 动态循环规则：
    /// 每次循环尝试由 2~3 次齐射组成（随循环数缓慢增加，封顶 6 次），
    /// 本次尝试累计得分达到阈值才算通关；失败可以重试当前循环。
    /// 循环成功后才推进 loopIndex，累计得分永远只增不减。
    /// </summary>
    public class LoopManager
    {
        private readonly GameConfig _config;
        private readonly SaveData _save;
        private readonly System.Random _random;

        public int LoopIndex => _save != null ? Mathf.Max(1, _save.loopIndex) : 1;
        public int Threshold => ThresholdFor(LoopIndex);
        public int GoalScore => _config.loop.goalScore;
        public int VolleysThisAttempt { get; private set; }
        public int VolleysCompleted { get; private set; }
        public int AttemptScore { get; private set; }
        public bool AttemptComplete => VolleysThisAttempt > 0 && VolleysCompleted >= VolleysThisAttempt;
        public int VolleysRemaining => Mathf.Max(0, VolleysThisAttempt - VolleysCompleted);
        public bool GoalReached => _save != null && _save.goalReached;
        public int TotalScore => _save != null ? _save.totalScore : 0;
        public int GoalRemaining => Mathf.Max(0, GoalScore - TotalScore);
        public float GoalProgress => Mathf.Clamp01(TotalScore / (float)Mathf.Max(1, GoalScore));
        public bool MeetsThreshold => GoalReached || AttemptScore >= Threshold;

        public LoopManager(GameConfig config, SaveData save, int? seed = null)
        {
            _config = config;
            _save = save;
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            VolleysThisAttempt = 0;
            VolleysCompleted = 0;
            AttemptScore = 0;
        }

        /// <summary>开始一次循环尝试；失败重试时仍使用同一个 loopIndex 与阈值。</summary>
        public void BeginAttempt()
        {
            VolleysCompleted = 0;
            AttemptScore = 0;
            VolleysThisAttempt = RollVolleyCount();
        }

        public int RollVolleyCount()
        {
            var settings = _config.loop;
            int extra = (LoopIndex - 1) / Mathf.Max(1, settings.growthEveryLoops);
            int max = Mathf.Clamp(settings.maxVolleys + extra, settings.minVolleys, settings.maxVolleysCap);
            int min = Mathf.Min(settings.minVolleys, max);
            return _random.Next(min, max + 1);
        }

        /// <summary>命中奖励洞时累加本次循环得分。</summary>
        public void AddScore(int score)
        {
            AttemptScore += Mathf.Max(0, score);
        }

        /// <summary>一次齐射结束，计入本次循环的齐射次数。</summary>
        public void RegisterVolley()
        {
            VolleysCompleted++;
        }

        public void AdvanceLoop()
        {
            if (_save == null) return;
            _save.loopIndex = LoopIndex + 1;
        }

        public int ThresholdFor(int loopIndex)
        {
            var settings = _config.loop;
            loopIndex = Mathf.Max(1, loopIndex);
            if (loopIndex <= settings.earlyUntilLoop) return settings.earlyThreshold;
            if (loopIndex <= settings.midUntilLoop) return settings.midThreshold;

            // 超过中段后，以 midThreshold 为基数按 lateThresholdGrowth 逐循环递增。
            int steps = loopIndex - settings.midUntilLoop;
            double value = settings.midThreshold * Math.Pow(settings.lateThresholdGrowth, steps);
            return (int)Math.Min(int.MaxValue, Math.Round(value));
        }
    }
}
