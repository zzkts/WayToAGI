using System;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// V5 存档：动态循环进度 + 附魔/引爆/挡板三项永久兑换 + 倾斜校准。
    /// 弹珠库存、洞口升级、旧附魔与技能等级等 V2 字段已删除。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 5;
        public int totalScore;
        public int credits;
        public int bestSingleScore;
        public int bestVolleyScore;
        public int bestLoopScore;
        public int loopIndex = 1;
        public bool goalReached;
        public int enchantTier;
        public bool explosionUnlocked;
        public int baffleCount = 1;
        public float tiltCalibration;
        public bool tiltInverted;

        public static SaveData CreateNew(GameConfig config)
        {
            var data = new SaveData
            {
                version = 5,
                totalScore = 0,
                credits = Mathf.Max(0, config.economy.startingCredits),
                bestSingleScore = 0,
                bestVolleyScore = 0,
                bestLoopScore = 0,
                loopIndex = 1,
                goalReached = false,
                enchantTier = 0,
                explosionUnlocked = false,
                baffleCount = Mathf.Clamp(config.economy.startingBaffleCount, 0, config.MaxBaffleCount),
                tiltCalibration = 0f,
                tiltInverted = config.tilt != null && config.tilt.invert
            };
            data.EnsureIntegrity(config);
            return data;
        }

        public void EnsureIntegrity(GameConfig config)
        {
            // V4 及更早的存档没有循环与兑换字段，保留累计得分/可用积分后按新规则补齐。
            if (version < 5)
            {
                version = 5;
                loopIndex = 1;
                enchantTier = 0;
                explosionUnlocked = false;
                baffleCount = config != null && config.economy != null
                    ? Mathf.Clamp(config.economy.startingBaffleCount, 0, config.MaxBaffleCount)
                    : 1;
                goalReached = false;
            }

            totalScore = Mathf.Max(0, totalScore);
            credits = Mathf.Max(0, credits);
            bestSingleScore = Mathf.Max(0, bestSingleScore);
            bestVolleyScore = Mathf.Max(0, bestVolleyScore);
            bestLoopScore = Mathf.Max(0, bestLoopScore);
            loopIndex = Mathf.Max(1, loopIndex);
            enchantTier = Mathf.Clamp(enchantTier, 0, config != null ? config.MaxEnchantTier : 0);
            baffleCount = Mathf.Clamp(baffleCount, 0, config != null ? config.MaxBaffleCount : 0);
            if (config != null && config.loop != null && totalScore >= config.loop.goalScore)
            {
                goalReached = true;
            }
        }
    }

    public static class SaveSystem
    {
        // 沿用 V2 的键名，确保旧存档可以迁移到 V5，而不是静默丢失。
        private const string PrefsKey = "pachinko_mvp_save_v2";
        private static bool _warned;

        public static SaveData Load(GameConfig config)
        {
            SaveData data = null;
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                try
                {
                    data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PrefsKey));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Save] 存档解析失败，将创建新存档：{e.Message}");
                }
            }

            if (data == null)
            {
                data = SaveData.CreateNew(config);
            }
            data.EnsureIntegrity(config);
            return data;
        }

        public static void Save(SaveData data)
        {
            if (data == null) return;
            try
            {
                PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[Save] 本地存档写入失败：{e.Message}");
                }
            }
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        public static bool HasSave()
        {
            return PlayerPrefs.HasKey(PrefsKey);
        }
    }
}
