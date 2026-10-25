using System;
using UnityEngine;

namespace Pachinko
{
    [Serializable]
    public class BallStockEntry
    {
        public string ballId;
        public int count;
    }

    [Serializable]
    public class HoleProgressEntry
    {
        public string holeId;
        public int level;
    }

    [Serializable]
    public class HoleSkillEntry
    {
        public string holeId;
        public int level;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 4;
        public int totalScore;
        public int credits;
        public int bestSingleScore;
        public int bestVolleyScore;
        public int milestoneIndex;
        public int emberLevel;
        public int frostLevel;
        public int gildLevel;
        public int splitLevel;
        public int bounceLevel;
        public int magnetLevel;
        public string[] unlockedBalls;
        public BallStockEntry[] ballStock;
        public HoleProgressEntry[] holeProgress;
        public HoleSkillEntry[] holeSkills;
        public float tiltCalibration;
        public bool tiltInverted;

        public static SaveData CreateNew(GameConfig config)
        {
            var data = new SaveData
            {
                version = 4,
                totalScore = 0,
                credits = Mathf.Max(0, config.economy.startingCredits),
                bestSingleScore = 0,
                bestVolleyScore = 0,
                milestoneIndex = 0,
                emberLevel = 0,
                frostLevel = 0,
                gildLevel = 0,
                splitLevel = 0,
                bounceLevel = 0,
                magnetLevel = 0,
                unlockedBalls = new[] { "ball_01" },
                ballStock = Array.Empty<BallStockEntry>(),
                holeProgress = Array.Empty<HoleProgressEntry>(),
                holeSkills = Array.Empty<HoleSkillEntry>(),
                tiltCalibration = 0f,
                tiltInverted = config.tilt != null && config.tilt.invert
            };
            data.EnsureIntegrity(config);
            return data;
        }

        public void EnsureIntegrity(GameConfig config)
        {
            if (version < 4) version = 4;
            totalScore = Mathf.Max(0, totalScore);
            credits = Mathf.Max(0, credits);
            bestSingleScore = Mathf.Max(0, bestSingleScore);
            bestVolleyScore = Mathf.Max(0, bestVolleyScore);
            milestoneIndex = Mathf.Clamp(milestoneIndex, 0, ScoreRules.MilestoneCount);
            emberLevel = Mathf.Clamp(emberLevel, 0, UpgradeCatalog.MaxEnchantLevel);
            frostLevel = Mathf.Clamp(frostLevel, 0, UpgradeCatalog.MaxEnchantLevel);
            gildLevel = Mathf.Clamp(gildLevel, 0, UpgradeCatalog.MaxEnchantLevel);
            splitLevel = Mathf.Clamp(splitLevel, 0, UpgradeCatalog.MaxBallSkillLevel);
            bounceLevel = Mathf.Clamp(bounceLevel, 0, UpgradeCatalog.MaxBallSkillLevel);
            magnetLevel = Mathf.Clamp(magnetLevel, 0, UpgradeCatalog.MaxBallSkillLevel);

            if (unlockedBalls == null || unlockedBalls.Length == 0)
            {
                unlockedBalls = new[] { "ball_01" };
            }
            else if (Array.IndexOf(unlockedBalls, "ball_01") < 0)
            {
                Array.Resize(ref unlockedBalls, unlockedBalls.Length + 1);
                unlockedBalls[unlockedBalls.Length - 1] = "ball_01";
            }

            if (ballStock == null) ballStock = Array.Empty<BallStockEntry>();
            if (holeProgress == null) holeProgress = Array.Empty<HoleProgressEntry>();
            if (holeSkills == null) holeSkills = Array.Empty<HoleSkillEntry>();

            if (config == null || config.balls == null || config.holes == null) return;

            foreach (var ball in config.balls)
            {
                if (ball == null) continue;
                EnsureStockEntry(ball.id);
                if (ball.alwaysAvailable && !IsBallUnlocked(ball.id))
                {
                    AddUnlockedBall(ball.id);
                }
            }
            foreach (var hole in config.holes)
            {
                if (hole == null) continue;
                GetHoleLevel(hole.id);
                GetHoleSkillLevel(hole.id);
            }
        }

        private BallStockEntry EnsureStockEntry(string ballId)
        {
            foreach (var entry in ballStock)
            {
                if (entry != null && entry.ballId == ballId)
                {
                    entry.count = Mathf.Max(0, entry.count);
                    return entry;
                }
            }

            var created = new BallStockEntry { ballId = ballId, count = 0 };
            Array.Resize(ref ballStock, ballStock.Length + 1);
            ballStock[ballStock.Length - 1] = created;
            return created;
        }

        public bool IsBallUnlocked(string ballId)
        {
            if (unlockedBalls == null) return ballId == "ball_01";
            return Array.IndexOf(unlockedBalls, ballId) >= 0;
        }

        public void AddUnlockedBall(string ballId)
        {
            if (IsBallUnlocked(ballId)) return;
            Array.Resize(ref unlockedBalls, unlockedBalls.Length + 1);
            unlockedBalls[unlockedBalls.Length - 1] = ballId;
        }

        public int GetStock(string ballId)
        {
            if (ballStock == null) return 0;
            foreach (var entry in ballStock)
            {
                if (entry != null && entry.ballId == ballId) return Mathf.Max(0, entry.count);
            }
            return 0;
        }

        public void AddStock(string ballId, int amount)
        {
            if (amount == 0) return;
            for (int i = 0; i < ballStock.Length; i++)
            {
                if (ballStock[i] != null && ballStock[i].ballId == ballId)
                {
                    ballStock[i].count = Mathf.Max(0, ballStock[i].count + amount);
                    return;
                }
            }
            EnsureStockEntry(ballId).count += amount;
        }

        public bool ConsumeStock(string ballId, int amount = 1)
        {
            amount = Mathf.Max(1, amount);
            for (int i = 0; i < ballStock.Length; i++)
            {
                if (ballStock[i] != null && ballStock[i].ballId == ballId)
                {
                    if (ballStock[i].count < amount) return false;
                    ballStock[i].count -= amount;
                    return true;
                }
            }
            return false;
        }

        public int GetHoleLevel(string holeId)
        {
            if (holeProgress == null) holeProgress = Array.Empty<HoleProgressEntry>();
            foreach (var entry in holeProgress)
            {
                if (entry != null && entry.holeId == holeId) return Mathf.Max(0, entry.level);
            }

            var created = new HoleProgressEntry { holeId = holeId, level = 0 };
            Array.Resize(ref holeProgress, holeProgress.Length + 1);
            holeProgress[holeProgress.Length - 1] = created;
            return 0;
        }

        public void SetHoleLevel(string holeId, int level)
        {
            for (int i = 0; i < holeProgress.Length; i++)
            {
                if (holeProgress[i] != null && holeProgress[i].holeId == holeId)
                {
                    holeProgress[i].level = Mathf.Max(0, level);
                    return;
                }
            }
            GetHoleLevel(holeId);
            SetHoleLevel(holeId, level);
        }

        public int GetHoleSkillLevel(string holeId)
        {
            if (holeSkills == null) holeSkills = Array.Empty<HoleSkillEntry>();
            foreach (var entry in holeSkills)
            {
                if (entry != null && entry.holeId == holeId)
                {
                    return Mathf.Clamp(entry.level, 0, UpgradeCatalog.MaxHoleSkillLevel);
                }
            }

            var created = new HoleSkillEntry { holeId = holeId, level = 0 };
            Array.Resize(ref holeSkills, holeSkills.Length + 1);
            holeSkills[holeSkills.Length - 1] = created;
            return 0;
        }

        public void SetHoleSkillLevel(string holeId, int level)
        {
            if (holeSkills == null) holeSkills = Array.Empty<HoleSkillEntry>();
            for (int i = 0; i < holeSkills.Length; i++)
            {
                if (holeSkills[i] != null && holeSkills[i].holeId == holeId)
                {
                    holeSkills[i].level = Mathf.Clamp(level, 0, UpgradeCatalog.MaxHoleSkillLevel);
                    return;
                }
            }
            GetHoleSkillLevel(holeId);
            SetHoleSkillLevel(holeId, level);
        }
    }

    public static class SaveSystem
    {
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
