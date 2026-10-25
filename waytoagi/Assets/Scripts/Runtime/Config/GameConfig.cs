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

    /// <summary>
    /// V3 取消弹珠购买与库存：标准弹珠无限供应，每次齐射基础装填若干颗，可永久升级容量。
    /// 可用积分与累计得分同步累加（1:1），只有兑换会减少可用积分。
    /// </summary>
    [Serializable]
    public class EconomySettings
    {
        public int startingCredits = 0;
        public int marblesPerVolley = 3;
        public int extraMarbleBasePrice = 5000;
        public float extraMarblePriceGrowth = 1.6f;
        public int startingBaffleCount = 1;
        public int enchantBasePrice = 8000;
        public float enchantPriceGrowth = 2.0f;
    }

    /// <summary>动态循环：每次循环的齐射次数随机，并随循环数缓慢增加。</summary>
    [Serializable]
    public class LoopSettings
    {
        public int minVolleys = 2;
        public int maxVolleys = 3;
        public int maxVolleysCap = 6;
        public int growthEveryLoops = 4;
        public int earlyThreshold = 3000;
        public int earlyUntilLoop = 4;
        public int midThreshold = 15000;
        public int midUntilLoop = 10;
        public float lateThresholdGrowth = 1.08f;
        public int goalScore = 1000000;
    }

    [Serializable]
    public class ExplosionSettings
    {
        public int unlockPrice = 30000;
        public int triggerCollisions = 10;
        /// <summary>世界单位；1 单位 = 100 设计像素，1.2 即 120px 半径。</summary>
        public float radius = 1.2f;
        public float force = 8f;
    }

    [Serializable]
    public class BaffleSettings
    {
        public int maxCount = 3;
        public int reduceCost = 12000;
        /// <summary>左右往返总幅度（世界单位）。1.2 = 120px，单侧位移为一半。</summary>
        public float amplitude = 1.2f;
        public float period = 2f;
    }

    [Serializable]
    public class EnchantDef
    {
        public string id;
        public string displayName;
        public string description;
        public int multiplier = 1;
        public string color = "#FFFFFF";
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
    }

    [Serializable]
    public class HoleDef
    {
        public string id;
        public string displayName;
        public int multiplier = 1;
        public int basePoints = 100;
        public string color = "#E9A93E";
        public float positionX;
        /// <summary>
        /// 空穴标记。空穴仍会接住弹珠，但不给分并中断连击。
        /// 使用反向布尔值，旧配置缺少该字段时默认按奖励洞处理。
        /// </summary>
        public bool noReward;

        public bool HasReward => !noReward;
    }

    [Serializable]
    public class GameConfig
    {
        public const int MaxVolleySize = 16;

        public string version = "3.0";
        public PhysicsSettings physics = new PhysicsSettings();
        public LaunchSettings launch = new LaunchSettings();
        public TiltSettings tilt = new TiltSettings();
        public EconomySettings economy = new EconomySettings();
        public LoopSettings loop = new LoopSettings();
        public ExplosionSettings explosion = new ExplosionSettings();
        public BaffleSettings baffle = new BaffleSettings();
        public BallDef[] balls;
        public EnchantDef[] enchants;
        public HoleDef[] holes;

        public int VolleySize => economy != null ? Mathf.Clamp(economy.marblesPerVolley, 1, MaxVolleySize) : 3;
        public int MaxBaffleCount => baffle != null ? Mathf.Clamp(baffle.maxCount, 0, 3) : 0;
        public int MaxEnchantTier => enchants != null ? enchants.Length : 0;

        public BallDef GetBall(string id)
        {
            if (balls == null) return null;
            foreach (var ball in balls)
            {
                if (ball != null && ball.id == id) return ball;
            }
            return null;
        }

        /// <summary>标准弹珠：V3 中唯一且无限供应的白球。</summary>
        public BallDef GetStandardBall()
        {
            var standard = GetBall("ball_01");
            if (standard != null) return standard;
            return balls != null && balls.Length > 0 ? balls[0] : null;
        }

        public EnchantDef GetEnchant(int tier)
        {
            if (enchants == null || tier <= 0 || tier > enchants.Length) return null;
            return enchants[tier - 1];
        }

        /// <summary>第 tier 次附魔解锁的价格（tier 从 0 开始，对应第一档附魔）。</summary>
        public int EnchantPrice(int tier)
        {
            if (economy == null) return 8000;
            return Mathf.RoundToInt(economy.enchantBasePrice * Mathf.Pow(Mathf.Max(1f, economy.enchantPriceGrowth), Mathf.Max(0, tier)));
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

        /// <summary>返回所有奖励洞，保持从左到右的布局顺序。空穴不参与兑换。</summary>
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

            economy.startingCredits = Mathf.Max(0, economy.startingCredits);
            economy.marblesPerVolley = Mathf.Clamp(economy.marblesPerVolley, 1, MaxVolleySize);
            economy.extraMarbleBasePrice = Mathf.Max(1, economy.extraMarbleBasePrice);
            economy.extraMarblePriceGrowth = Mathf.Max(1f, economy.extraMarblePriceGrowth);
            economy.enchantBasePrice = Mathf.Max(1, economy.enchantBasePrice);
            economy.enchantPriceGrowth = Mathf.Max(1f, economy.enchantPriceGrowth);

            loop.minVolleys = Mathf.Clamp(loop.minVolleys, 1, 12);
            loop.maxVolleys = Mathf.Clamp(loop.maxVolleys, loop.minVolleys, 12);
            loop.maxVolleysCap = Mathf.Clamp(loop.maxVolleysCap, loop.maxVolleys, 12);
            loop.growthEveryLoops = Mathf.Max(1, loop.growthEveryLoops);
            loop.earlyThreshold = Mathf.Max(0, loop.earlyThreshold);
            loop.earlyUntilLoop = Mathf.Max(0, loop.earlyUntilLoop);
            loop.midThreshold = Mathf.Max(loop.earlyThreshold, loop.midThreshold);
            loop.midUntilLoop = Mathf.Max(loop.earlyUntilLoop, loop.midUntilLoop);
            loop.lateThresholdGrowth = Mathf.Max(1f, loop.lateThresholdGrowth);
            loop.goalScore = Mathf.Max(1, loop.goalScore);

            explosion.unlockPrice = Mathf.Max(1, explosion.unlockPrice);
            explosion.triggerCollisions = Mathf.Clamp(explosion.triggerCollisions, 1, 100);
            explosion.radius = Mathf.Clamp(explosion.radius, 0.2f, 5f);
            explosion.force = Mathf.Clamp(explosion.force, 0.1f, 50f);

            baffle.maxCount = Mathf.Clamp(baffle.maxCount, 0, 3);
            baffle.reduceCost = Mathf.Max(1, baffle.reduceCost);
            baffle.amplitude = Mathf.Clamp(baffle.amplitude, 0f, 5f);
            baffle.period = Mathf.Max(0.2f, baffle.period);
            economy.startingBaffleCount = Mathf.Clamp(economy.startingBaffleCount, 0, baffle.maxCount);

            if (balls == null || balls.Length == 0)
            {
                throw new Exception("game_config.json: balls 不能为空");
            }
            if (holes == null || holes.Length == 0)
            {
                throw new Exception("game_config.json: holes 至少需要配置一个奖励洞");
            }
            if (GetStandardBall() == null || GetStandardBall().id != "ball_01")
            {
                throw new Exception("game_config.json: 缺少标准弹珠 ball_01");
            }

            if (enchants != null)
            {
                foreach (var enchant in enchants)
                {
                    if (enchant == null) throw new Exception("game_config.json: enchants 中存在空项");
                    enchant.multiplier = Mathf.Max(1, enchant.multiplier);
                }
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
                }
                else
                {
                    hole.multiplier = 0;
                    hole.basePoints = 0;
                }
            }
            if (rewardCount == 0) throw new Exception("game_config.json: 至少需要配置一个 noReward=false 的奖励洞");
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
