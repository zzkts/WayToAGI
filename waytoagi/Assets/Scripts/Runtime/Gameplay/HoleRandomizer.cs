using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 每次齐射前把配置里的奖励洞模板重新分配到 8 个固定洞位。
    /// 洞位自身的编号与坐标不变，所以同洞连击仍按「同一物理洞位」计算；
    /// 奖励倍率、基础分与颜色随模板一起洗牌，空穴填满其余洞位。
    /// </summary>
    public static class HoleRandomizer
    {
        private static readonly System.Random SharedRandom = new System.Random();

        public static void Redistribute(RewardHole[] holes, GameConfig config, System.Random random = null)
        {
            if (holes == null || holes.Length == 0 || config == null || config.holes == null) return;
            if (random == null) random = SharedRandom;

            var rewards = new List<HoleDef>();
            HoleDef empty = null;
            foreach (var def in config.holes)
            {
                if (def == null) continue;
                if (def.HasReward) rewards.Add(def);
                else if (empty == null) empty = def;
            }

            int rewardCount = Mathf.Min(rewards.Count, holes.Length);
            Shuffle(rewards, random);
            bool[] rewardSlots = SelectRewardSlots(holes.Length, rewardCount, random);

            int rewardIndex = 0;
            var summary = new StringBuilder("[Holes] 本次分布：");
            for (int i = 0; i < holes.Length; i++)
            {
                var hole = holes[i];
                if (hole == null || hole.Def == null) continue;

                if (rewardSlots[i] && rewardIndex < rewards.Count)
                {
                    CopyReward(rewards[rewardIndex++], hole.Def);
                }
                else
                {
                    CopyEmpty(empty, hole.Def);
                }

                hole.ApplyDefinition();
                if (i > 0) summary.Append(" · ");
                summary.Append($"{i + 1}:{(hole.HasReward ? "×" + hole.Def.multiplier : "空")}");
            }
            Debug.Log(summary.ToString());
        }

        /// <summary>随机选出 rewardCount 个洞位，返回按物理顺序排列的标记。供自检复用。</summary>
        public static bool[] SelectRewardSlots(int slotCount, int rewardCount, System.Random random)
        {
            slotCount = Mathf.Max(0, slotCount);
            rewardCount = Mathf.Clamp(rewardCount, 0, slotCount);
            var selected = new bool[slotCount];
            if (slotCount == 0 || rewardCount == 0) return selected;
            if (random == null) random = SharedRandom;

            var order = new int[slotCount];
            for (int i = 0; i < slotCount; i++) order[i] = i;
            for (int i = slotCount - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = order[i];
                order[i] = order[j];
                order[j] = temp;
            }
            for (int i = 0; i < rewardCount; i++) selected[order[i]] = true;
            return selected;
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>只复制奖励属性，保留洞位自己的 id / 名称 / positionX。</summary>
        private static void CopyReward(HoleDef source, HoleDef target)
        {
            target.noReward = false;
            target.multiplier = Mathf.Max(0, source.multiplier);
            target.basePoints = Mathf.Max(0, source.basePoints);
            target.color = source.color;
        }

        private static void CopyEmpty(HoleDef source, HoleDef target)
        {
            target.noReward = true;
            target.multiplier = 0;
            target.basePoints = 0;
            target.color = source != null ? source.color : "#6E655C";
        }
    }
}
