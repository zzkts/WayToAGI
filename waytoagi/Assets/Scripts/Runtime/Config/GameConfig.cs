using System;
using UnityEngine;

namespace Pachinko
{
    [Serializable]
    public class PhysicsSettings
    {
        public float gravity = 9.8f;
        public float pinBounciness = 0.85f;
        public float ballBounciness = 0.45f;
        public float wallBounciness = 0.35f;
        public float barBounciness = 0.7f;
        public float plungerBounciness = 0.6f;
        public float ballMass = 0.2f;
        public float plungerMass = 5f;
        public float ballRadius = 0.1f;
    }

    [Serializable]
    public class LaunchSettings
    {
        public float minBallSpeed = 6f;
        public float maxBallSpeed = 14f;
        public float maxPullWorld = 1.2f;
        public float plungerSpeedFactor = 0.6f;
        public float plungerReturnDelay = 0.16f;

        public float PlungerSpeedAt(float ratio)
        {
            float ballSpeed = Mathf.Lerp(minBallSpeed, maxBallSpeed, Mathf.Clamp01(ratio));
            return ballSpeed * Mathf.Max(0.05f, plungerSpeedFactor);
        }
    }

    [Serializable]
    public class TiltSettings
    {
        public float deadZone = 0.05f;
        public float fullScale = 0.35f;
        public float maxHorizontalGravity = 6f;
        public float smoothing = 10f;
        public float sensorSmoothing = 12f;
        public bool invert = false;
    }

    [Serializable]
    public class EconomySettings
    {
        public int startingCredits = 0;
        public int maxBallsPerShopVisit = 10;
        public int freeStandardBallsPerPhase = 3;
    }

    [Serializable]
    public class BallDef
    {
        public string id;
        public string displayName;
        public string description;
        public int multiplier = 1;
        public string color = "#FFFFFF";
        public string outlineColor = "#333333";
        public int unlockPrice;
        public int unitPrice;
        public bool alwaysAvailable;
    }

    [Serializable]
    public class HoleDef
    {
        public string id;
        public string displayName;
        public int multiplier = 1;
        public int basePoints = 100;
        public int upgradeBasePrice = 300;
        public float upgradePriceGrowth = 1.6f;
        public int upgradePointsPerLevel = 50;
        public int maxUpgradeLevel = 3;
        public string color = "#E9A93E";
        public float positionX;
        /// <summary>
        /// 空穴标记。空穴仍会接住弹珠，但不给分并中断连锁。
        /// 使用反向布尔值，旧配置缺少该字段时默认按奖励洞处理。
        /// </summary>
        public bool noReward;

        public bool HasReward => !noReward;

        public int PointsAtLevel(int level)
        {
            return basePoints + Mathf.Clamp(level, 0, maxUpgradeLevel) * upgradePointsPerLevel;
        }

        public int UpgradePriceAtLevel(int level)
        {
            return Mathf.RoundToInt(upgradeBasePrice * Mathf.Pow(upgradePriceGrowth, Mathf.Clamp(level, 0, maxUpgradeLevel)));
        }
    }

    [Serializable]
    public class GameConfig
    {
        public string version = "2.0";
        public PhysicsSettings physics = new PhysicsSettings();
        public LaunchSettings launch = new LaunchSettings();
        public TiltSettings tilt = new TiltSettings();
        public EconomySettings economy = new EconomySettings();
        public BallDef[] balls;
        public HoleDef[] holes;

        public int MaxVolleyCapacity => economy != null
            ? economy.freeStandardBallsPerPhase + economy.maxBallsPerShopVisit
            : 0;

        public BallDef GetBall(string id)
        {
            if (balls == null) return null;
            foreach (var ball in balls)
            {
                if (ball != null && ball.id == id) return ball;
            }
            return null;
        }

        public HoleDef GetHole(string id)
        {
            if (holes == null) return null;
            foreach (var hole in holes)
            {
                if (hole != null && hole.id == id) return hole;
            }
            return null;
        }

        /// <summary>返回所有奖励洞，保持从左到右的布局顺序。空穴不参与商店升级。</summary>
        public HoleDef[] GetRewardHoles()
        {
            if (holes == null) return Array.Empty<HoleDef>();
            var result = new System.Collections.Generic.List<HoleDef>();
            foreach (var hole in holes)
            {
                if (hole != null && hole.HasReward) result.Add(hole);
            }
            return result.ToArray();
        }

        public void Validate()
        {
            physics.gravity = Mathf.Max(0.1f, physics.gravity);
            physics.ballRadius = Mathf.Clamp(physics.ballRadius, 0.03f, 0.3f);
            physics.ballMass = Mathf.Max(0.01f, physics.ballMass);
            physics.plungerMass = Mathf.Max(physics.ballMass, physics.plungerMass);
            launch.maxPullWorld = Mathf.Clamp(launch.maxPullWorld, 0.2f, 3f);
            launch.minBallSpeed = Mathf.Clamp(launch.minBallSpeed, 1f, 30f);
            launch.maxBallSpeed = Mathf.Clamp(launch.maxBallSpeed, launch.minBallSpeed, 40f);
            launch.plungerSpeedFactor = Mathf.Clamp(launch.plungerSpeedFactor, 0.1f, 2f);
            tilt.deadZone = Mathf.Clamp(tilt.deadZone, 0f, 0.5f);
            tilt.fullScale = Mathf.Max(tilt.fullScale, tilt.deadZone + 0.01f);
            tilt.maxHorizontalGravity = Mathf.Clamp(tilt.maxHorizontalGravity, 0f, 20f);
            tilt.smoothing = Mathf.Max(1f, tilt.smoothing);
            tilt.sensorSmoothing = Mathf.Max(1f, tilt.sensorSmoothing);
            economy.maxBallsPerShopVisit = Mathf.Max(1, economy.maxBallsPerShopVisit);
            economy.freeStandardBallsPerPhase = Mathf.Clamp(economy.freeStandardBallsPerPhase, 1, 20);
            economy.maxBallsPerShopVisit = Mathf.Clamp(economy.maxBallsPerShopVisit, 1, 20);

            if (balls == null || balls.Length == 0)
            {
                throw new Exception("game_config.json: balls 不能为空");
            }
            if (holes == null || holes.Length == 0)
            {
                throw new Exception("game_config.json: holes 至少需要配置一个奖励洞");
            }
            var seenHoleIds = new System.Collections.Generic.HashSet<string>();
            float lastX = float.NegativeInfinity;
            int rewardCount = 0;
            foreach (var hole in holes)
            {
                if (hole == null) throw new Exception("game_config.json: holes 中存在空项");
                if (!seenHoleIds.Add(hole.id)) throw new Exception($"game_config.json: 奖励洞 id 重复 {hole.id}");
                if (hole.positionX < lastX) throw new Exception("game_config.json: holes 必须按 positionX 从左到右排序");
                lastX = hole.positionX;
                if (hole.HasReward)
                {
                    rewardCount++;
                    hole.basePoints = Mathf.Max(0, hole.basePoints);
                    hole.multiplier = Mathf.Max(0, hole.multiplier);
                    hole.maxUpgradeLevel = Mathf.Max(0, hole.maxUpgradeLevel);
                    hole.upgradeBasePrice = Mathf.Max(1, hole.upgradeBasePrice);
                    hole.upgradePointsPerLevel = Mathf.Max(0, hole.upgradePointsPerLevel);
                }
                else
                {
                    // 空穴不结算积分，也不提供升级入口。
                    hole.multiplier = 0;
                    hole.basePoints = 0;
                    hole.maxUpgradeLevel = 0;
                    hole.upgradePointsPerLevel = 0;
                }
            }
            if (rewardCount == 0) throw new Exception("game_config.json: 至少需要配置一个 noReward=false 的奖励洞");
            if (GetBall("ball_01") == null)
            {
                throw new Exception("game_config.json: 缺少保底弹珠 ball_01");
            }
            if (GetBall("ball_01").alwaysAvailable == false)
            {
                Debug.LogWarning("game_config.json: ball_01 应为免费保底弹珠，已强制设为无限供应。");
                GetBall("ball_01").alwaysAvailable = true;
                GetBall("ball_01").unitPrice = 0;
                GetBall("ball_01").unlockPrice = 0;
            }
        }

    }

    public static class ConfigLoader
    {
        public const string FileName = "game_config.json";

        public static GameConfig Load(out string source)
        {
            string persistentPath = System.IO.Path.Combine(Application.persistentDataPath, FileName);
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);

            // 桌面/编辑器：StreamingAssets 可直接改文件调参，优先级最高。
            try
            {
                if (System.IO.File.Exists(streamingPath))
                {
                    string json = System.IO.File.ReadAllText(streamingPath);
                    var config = JsonUtility.FromJson<GameConfig>(json);
                    if (config != null)
                    {
                        config.Validate();
                        TryWritePersistentCopy(json, persistentPath);
                        source = streamingPath;
                        return config;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Config] StreamingAssets 配置读取失败，将继续尝试本地配置：{e.Message}");
            }

            // 移动端/打包后：允许把配置放到 persistentDataPath 中，改数值无需重新打包。
            try
            {
                if (System.IO.File.Exists(persistentPath))
                {
                    string json = System.IO.File.ReadAllText(persistentPath);
                    var config = JsonUtility.FromJson<GameConfig>(json);
                    if (config != null)
                    {
                        config.Validate();
                        source = persistentPath;
                        return config;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Config] 本地配置读取失败，将使用 Resources 内置配置：{e.Message}");
            }

            var asset = Resources.Load<TextAsset>("Config/game_config");
            if (asset == null)
            {
                throw new Exception("找不到游戏配置：StreamingAssets/game_config.json、persistentDataPath 和 Resources/Config/game_config.json 均不可用");
            }

            var fallback = JsonUtility.FromJson<GameConfig>(asset.text);
            if (fallback == null)
            {
                throw new Exception("game_config.json 解析失败");
            }
            fallback.Validate();
            TryWritePersistentCopy(asset.text, persistentPath);
            source = "Resources/Config/game_config.json";
            return fallback;
        }

        private static void TryWritePersistentCopy(string json, string path)
        {
            try
            {
                if (System.IO.File.Exists(path)) return;
                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Config] 写入本地配置副本失败：{e.Message}");
            }
        }
    }
}
