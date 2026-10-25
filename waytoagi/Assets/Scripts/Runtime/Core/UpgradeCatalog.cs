using UnityEngine;

namespace Pachinko
{
    public enum EnchantId
    {
        Ember,
        Frost,
        Gild
    }

    public enum BallSkillId
    {
        Split,
        Bounce,
        Magnet
    }

    public enum HoleSkillId
    {
        None,
        Bounty,
        Aegis,
        SplitEcho,
        JackpotCore
    }

    /// <summary>
    /// 商店升级的硬编码数值目录。
    /// 设计分层：附魔 = 命中后的元素转化；弹珠技能 = 飞行行为；奖励洞技能 = 落洞后的资源转化。
    /// 三条线互不直接改写同一个乘区，只通过触发/资源转换产生协同。
    /// </summary>
    public static class UpgradeCatalog
    {
        public const int MaxEnchantLevel = 5;
        public const int MaxBallSkillLevel = 3;
        public const int MaxHoleSkillLevel = 3;
        public const int EmberThreshold = 3;

        public static string EnchantName(EnchantId id)
        {
            switch (id)
            {
                case EnchantId.Ember: return "炽焰";
                case EnchantId.Frost: return "霜缚";
                case EnchantId.Gild: return "金辉";
                default: return "";
            }
        }

        public static string EnchantDescription(EnchantId id, int level)
        {
            switch (id)
            {
                case EnchantId.Ember:
                    return level <= 0
                        ? "命中积累余烬，每 3 层触发一次爆发。"
                        : $"命中积累余烬；每 3 层爆发 {EmberBurstBase(level):N0}×弹珠倍率。";
                case EnchantId.Frost:
                    return level <= 0
                        ? "提高倾斜控球力度，不直接增加得分。"
                        : $"倾斜控球力度 +{level * 10}%。";
                case EnchantId.Gild:
                    return level <= 0
                        ? "提高所有得分折算的积分比例。"
                        : $"积分获取 +{level * 10}%。";
                default: return "";
            }
        }

        public static int EnchantCost(EnchantId id, int currentLevel)
        {
            return Mathf.RoundToInt(1500f * Mathf.Pow(1.8f, Mathf.Clamp(currentLevel, 0, MaxEnchantLevel)));
        }

        public static string BallSkillName(BallSkillId id)
        {
            switch (id)
            {
                case BallSkillId.Split: return "分裂";
                case BallSkillId.Bounce: return "弹跳";
                case BallSkillId.Magnet: return "磁引";
                default: return "";
            }
        }

        public static string BallSkillDescription(BallSkillId id, int level)
        {
            switch (id)
            {
                case BallSkillId.Split:
                    return level <= 0
                        ? "发射阶段额外生成分裂弹珠。"
                        : $"每轮额外生成 {level} 颗分裂标准弹珠。";
                case BallSkillId.Bounce:
                    return level <= 0
                        ? "提高弹珠与钉柱碰撞后的反弹。"
                        : $"弹珠反弹 +{level * 8}%。";
                case BallSkillId.Magnet:
                    return level <= 0
                        ? "弹珠缓慢向高倍率洞偏移。"
                        : $"向高倍率洞的牵引力 {level:0.0} 级。";
                default: return "";
            }
        }

        public static int BallSkillCost(BallSkillId id, int currentLevel)
        {
            return Mathf.RoundToInt(2000f * Mathf.Pow(1.8f, Mathf.Clamp(currentLevel, 0, MaxBallSkillLevel)));
        }

        public static HoleSkillId HoleSkillFor(string holeId)
        {
            switch (holeId)
            {
                case "hole_01": return HoleSkillId.Bounty;
                case "hole_03": return HoleSkillId.Aegis;
                case "hole_05": return HoleSkillId.SplitEcho;
                case "hole_07": return HoleSkillId.JackpotCore;
                default: return HoleSkillId.None;
            }
        }

        public static string HoleSkillName(HoleSkillId id)
        {
            switch (id)
            {
                case HoleSkillId.Bounty: return "赏金";
                case HoleSkillId.Aegis: return "稳态护盾";
                case HoleSkillId.SplitEcho: return "裂变回响";
                case HoleSkillId.JackpotCore: return "头奖核心";
                default: return "";
            }
        }

        public static string HoleSkillDescription(HoleSkillId id, int level)
        {
            switch (id)
            {
                case HoleSkillId.Bounty:
                    return level <= 0
                        ? "本洞命中额外获得积分。"
                        : $"本洞命中额外 +{(80 + 40 * level) * 1:N0}×弹珠倍率积分。";
                case HoleSkillId.Aegis:
                    return level <= 0
                        ? "每个发射阶段抵挡漏球断链。"
                        : $"每个发射阶段获得 {level} 次护盾，抵挡漏球断链。";
                case HoleSkillId.SplitEcho:
                    return level <= 0
                        ? "本洞命中为下一轮充能分裂弹。"
                        : $"本洞命中后，下一轮额外 +{level} 颗分裂弹。";
                case HoleSkillId.JackpotCore:
                    return level <= 0
                        ? "强化 7 号洞头奖。"
                        : $"7 号洞头奖 +{level * 25}%。";
                default: return "";
            }
        }

        public static int HoleSkillCost(string holeId, int currentLevel)
        {
            return Mathf.RoundToInt(1500f * Mathf.Pow(1.8f, Mathf.Clamp(currentLevel, 0, MaxHoleSkillLevel)));
        }

        public static int EmberBurstBase(int emberLevel)
        {
            return 150 + 150 * Mathf.Max(0, emberLevel);
        }

        public static float FrostAuthorityMultiplier(int level)
        {
            return 1f + 0.10f * Mathf.Max(0, level);
        }

        public static float GildCreditMultiplier(int level)
        {
            return 1f + 0.10f * Mathf.Max(0, level);
        }

        public static float BounceMultiplier(int level)
        {
            return 1f + 0.08f * Mathf.Max(0, level);
        }

        public static float MagnetAcceleration(int level)
        {
            return Mathf.Max(0, level);
        }

        public static int BountyCredits(int level, BallDef ball)
        {
            if (level <= 0) return 0;
            int ballMultiplier = ball != null ? Mathf.Max(1, ball.multiplier) : 1;
            return (80 + 40 * level) * ballMultiplier;
        }

        public static int ShieldCount(int level)
        {
            return Mathf.Max(0, level);
        }

        public static int SplitEchoCount(int level)
        {
            return Mathf.Max(0, level);
        }

        public static float JackpotMultiplier(int level)
        {
            return 1f + 0.25f * Mathf.Max(0, level);
        }
    }
}
