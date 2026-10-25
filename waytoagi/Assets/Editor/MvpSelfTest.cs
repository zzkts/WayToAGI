using System;
using System.Collections.Generic;
using System.IO;
using Pachinko;
using UnityEditor;
using UnityEngine;

namespace PachinkoEditor
{
    public static class MvpSelfTest
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll(bool exitWhenDone)
        {
            _passed = 0;
            _failed = 0;
            Debug.Log("========== 弹珠机 V3 数值自检 ==========");

            try
            {
                string path = "Assets/StreamingAssets/game_config.json";
                var config = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));
                config.Validate();

                ConfigTest(config);
                HoleRandomizationTest(config);
                AudioTest();
                ScoreTest(config);
                ComboResetTest(config);
                LoopTest(config);
                ShopTest(config);
                MagazineTest(config);
                SaveRoundTripTest(config);
                EconomyTest(config);

                Debug.Log($"[SELFTEST] 完成：{_passed} 通过，{_failed} 失败");
            }
            catch (Exception e)
            {
                _failed++;
                Debug.LogError("[SELFTEST] 异常：" + e);
            }

            if (_failed == 0)
            {
                Debug.Log("[SELFTEST] RESULT: PASS");
            }
            else
            {
                Debug.LogError("[SELFTEST] RESULT: FAIL");
            }
            return _failed == 0;
        }

        private static void ConfigTest(GameConfig config)
        {
            Check("配置版本为 3.0", config.version == "3.0");
            Check("V3 只保留一颗无限标准弹珠", config.balls != null && config.balls.Length == 1 &&
                config.GetStandardBall() != null && config.GetStandardBall().id == "ball_01" &&
                config.GetStandardBall().multiplier == 1);
            Check("每次齐射基础装填 3 颗", config.VolleySize == 3 && config.economy.marblesPerVolley == 3);
            Check("额外弹珠首价 5000、每颗 ×1.6、上限 16 颗",
                config.economy.extraMarbleBasePrice == 5000 &&
                Mathf.Approximately(config.economy.extraMarblePriceGrowth, 1.6f) &&
                GameConfig.MaxVolleySize == 16);
            Check("初始没有可用积分", config.economy.startingCredits == 0);
            Check("初始挡板 1 块、最多 3 块", config.economy.startingBaffleCount == 1 && config.MaxBaffleCount == 3);

            Check("配置有八个洞", config.holes != null && config.holes.Length == 8);
            Check("八个洞全部位于发射道隔板左侧", config.holes[7].positionX < BoardGeometry.LauncherWallX - 0.15f);
            Check("配置模板为 4 奖励洞 + 4 空穴",
                config.GetRewardHoles().Length == 4 &&
                !config.holes[1].HasReward && !config.holes[3].HasReward &&
                !config.holes[5].HasReward && !config.holes[7].HasReward);
            Check("四奖励洞倍率为 1/3/8/12",
                config.holes[0].multiplier == 1 && config.holes[2].multiplier == 3 &&
                config.holes[4].multiplier == 8 && config.holes[6].multiplier == 12);
            Check("四奖励洞基础积分为 50/120/260/300",
                config.holes[0].basePoints == 50 && config.holes[2].basePoints == 120 &&
                config.holes[4].basePoints == 260 && config.holes[6].basePoints == 300);
            Check("空穴不产生积分", config.holes[1].multiplier == 0 && config.holes[1].basePoints == 0);

            Check("两档附魔为 ×2 / ×5", config.enchants != null && config.enchants.Length == 2 &&
                config.GetEnchant(1).multiplier == 2 && config.GetEnchant(2).multiplier == 5);
            Check("附魔首档价格 8000、次档 16000",
                config.EnchantPrice(0) == 8000 && config.EnchantPrice(1) == 16000);

            Check("循环阈值 1~4 为 3000", config.loop.earlyThreshold == 3000 && config.loop.earlyUntilLoop == 4);
            Check("循环阈值 5~10 为 15000", config.loop.midThreshold == 15000 && config.loop.midUntilLoop == 10);
            Check("第 11 循环起每循环 ×1.08", Mathf.Approximately(config.loop.lateThresholdGrowth, 1.08f));
            Check("通关目标为 1000000", config.loop.goalScore == 1000000);
            Check("引爆：30000 积分 / 10 次碰撞 / 120px / 力度 8",
                config.explosion.unlockPrice == 30000 &&
                config.explosion.triggerCollisions == 10 &&
                Mathf.Approximately(config.explosion.radius, 1.2f) &&
                Mathf.Approximately(config.explosion.force, 8f));
            Check("挡板：移除 12000 / 幅度 120px / 周期 2 秒", config.baffle.reduceCost == 12000 &&
                Mathf.Approximately(config.baffle.amplitude, 1.2f) &&
                Mathf.Approximately(config.baffle.period, 2f));
        }

        private static void HoleRandomizationTest(GameConfig config)
        {
            int slotCount = config.holes.Length;
            int rewardCount = config.GetRewardHoles().Length;
            bool countsOk = true;
            bool varied = false;
            int firstMask = -1;
            var slotHits = new int[slotCount];

            for (int seed = 0; seed < 128; seed++)
            {
                bool[] selected = HoleRandomizer.SelectRewardSlots(slotCount, rewardCount, new System.Random(seed));
                int count = 0;
                int mask = 0;
                for (int i = 0; i < selected.Length; i++)
                {
                    if (!selected[i]) continue;
                    count++;
                    mask |= 1 << i;
                    slotHits[i]++;
                }
                if (count != rewardCount) countsOk = false;
                if (seed == 0) firstMask = mask;
                else if (mask != firstMask) varied = true;
            }

            bool allSlotsReachable = true;
            for (int i = 0; i < slotHits.Length; i++)
            {
                if (slotHits[i] == 0) allSlotsReachable = false;
            }

            Check("每次齐射重新分布恰好 4 个奖励洞", rewardCount == 4 && countsOk);
            Check("随机分布会改变奖励洞位置", varied);
            Check("8 个洞位都可能成为奖励洞", allSlotsReachable);
        }

        private static void AudioTest()
        {
            string configPath = "Assets/StreamingAssets/audio_config.json";
            Check("音频配置存在", File.Exists(configPath));
            if (!File.Exists(configPath)) return;

            var audio = JsonUtility.FromJson<AudioConfig>(File.ReadAllText(configPath));
            Check("音效配置 14 条", audio != null && audio.sfx != null && audio.sfx.Length == 14);
            Check("BGM 配置 3 条", audio != null && audio.bgm != null && audio.bgm.Length == 3);
            if (audio == null || audio.sfx == null || audio.bgm == null) return;

            Check("音频文件数量与配置一致",
                Directory.GetFiles("Assets/Resources/Audio/SFX", "*.mp3").Length == audio.sfx.Length &&
                Directory.GetFiles("Assets/Resources/Audio/Music", "*.mp3").Length == audio.bgm.Length);

            var sfxIds = new HashSet<string>();
            bool sfxFilesOk = true;
            bool sfxImportOk = true;
            foreach (var def in audio.sfx)
            {
                if (def == null || string.IsNullOrEmpty(def.id) || string.IsNullOrEmpty(def.file))
                {
                    sfxFilesOk = false;
                    continue;
                }
                sfxIds.Add(def.id);

                string path = "Assets/Resources/Audio/SFX/" + def.file;
                if (!File.Exists(path))
                {
                    sfxFilesOk = false;
                    Debug.LogError($"[SELFTEST] 缺少音效文件：{path}");
                    continue;
                }
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null) sfxImportOk = false;

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                AudioImporterSampleSettings settings = default(AudioImporterSampleSettings);
                if (importer != null) settings = importer.defaultSampleSettings;
                if (settings.loadType != AudioClipLoadType.DecompressOnLoad ||
                    settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    sfxImportOk = false;
                    Debug.LogError($"[SELFTEST] 音效导入设置不符合标准：{def.file}");
                }
            }

            string[] requiredSfx =
            {
                "score_base", "combo_01", "combo_02", "combo_03", "combo_04", "combo_05",
                "launch", "hit_peg", "hit_baffle", "explode", "ui_click", "ui_unlock", "fail", "win"
            };
            bool allSfxHooked = true;
            foreach (var id in requiredSfx)
            {
                if (!sfxIds.Contains(id))
                {
                    allSfxHooked = false;
                    Debug.LogError($"[SELFTEST] 配置缺少事件音效 ID：{id}");
                }
            }
            Check("全部音效文件存在且 ID 唯一", sfxFilesOk && sfxIds.Count == audio.sfx.Length);
            Check("全部音效已按交付标准导入", sfxImportOk);
            Check("落洞 / 连击 / 发射 / 碰撞 / 爆炸 / UI / 胜负音效已接线", allSfxHooked);

            var bgmIds = new HashSet<string>();
            bool bgmFilesOk = true;
            bool bgmImportOk = true;
            bool bgmRulesOk = true;
            foreach (var def in audio.bgm)
            {
                if (def == null || string.IsNullOrEmpty(def.id) || string.IsNullOrEmpty(def.file))
                {
                    bgmFilesOk = false;
                    continue;
                }
                bgmIds.Add(def.id);

                string path = "Assets/Resources/Audio/Music/" + def.file;
                if (!File.Exists(path))
                {
                    bgmFilesOk = false;
                    Debug.LogError($"[SELFTEST] 缺少 BGM 文件：{path}");
                    continue;
                }
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null) bgmImportOk = false;

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                AudioImporterSampleSettings settings = default(AudioImporterSampleSettings);
                if (importer != null) settings = importer.defaultSampleSettings;
                if (settings.loadType != AudioClipLoadType.Streaming)
                {
                    bgmImportOk = false;
                    Debug.LogError($"[SELFTEST] BGM 导入设置不符合标准：{def.file}");
                }
                if (!def.loop || def.fadeInMs <= 0 || def.fadeOutMs < 0) bgmRulesOk = false;
            }

            Check("三首 BGM 已配置 main / menu / shop",
                bgmIds.Contains("main_loop") && bgmIds.Contains("menu_loop") && bgmIds.Contains("shop_loop"));
            Check("BGM 文件存在且循环 / 淡入淡出有效", bgmFilesOk && bgmImportOk && bgmRulesOk);
            Check("BGM 电平比音效低约 10 dB",
                audio.bgm[0].volumeDb <= -12f && audio.sfx[0].volumeDb >= -2f);
        }

        private static void ScoreTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var score = new ScoreManager(data);
            var ball = config.GetStandardBall();
            var hole = config.holes[6];

            var r1 = score.Score(ball, hole, 1, hole.basePoints);
            Check("首球 1×12×300=3600", r1.subtotal == 3600 && r1.total == 3600);
            Check("首球同洞连击 ×1", r1.comboMultiplier == 1 && r1.comboCount == 1);
            Check("累计得分与可用积分 1:1", data.totalScore == 3600 && data.credits == 3600);
            Check("最高单次记录更新为 3600", data.bestSingleScore == 3600);

            var r2 = score.Score(ball, hole, 1, hole.basePoints);
            Check("同洞第 2 次连击 ×2，总分 7200", r2.comboMultiplier == 2 && r2.total == 7200);
            Check("累计得分累加到 10800", data.totalScore == 10800 && data.credits == 10800);

            var switched = score.Score(ball, config.holes[0], 1, config.holes[0].basePoints);
            Check("换洞后同洞连击从 ×1 重新开始", switched.comboMultiplier == 1 && switched.comboCount == 1);
            Check("换洞得分按新洞计算 1×1×50=50", switched.total == 50);
        }

        private static void ComboResetTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var score = new ScoreManager(data);
            var ball = config.GetStandardBall();
            var hole = config.holes[0];

            score.Score(ball, hole, 1, hole.basePoints);
            score.Score(ball, hole, 1, hole.basePoints);
            score.Score(ball, hole, 1, hole.basePoints);
            var fourth = score.Score(ball, hole, 1, hole.basePoints);
            var fifth = score.Score(ball, hole, 1, hole.basePoints);
            Check("同洞连击第 4 次达到 ×8", fourth.comboMultiplier == 8);
            Check("同洞连击封顶 ×8", fifth.comboMultiplier == 8 && fifth.comboCount == 5);

            score.RegisterMiss();
            var afterMiss = score.Score(ball, hole, 1, hole.basePoints);
            Check("漏球/空穴后连击归零", afterMiss.comboMultiplier == 1 && afterMiss.comboCount == 1);
            Check("归零后首球得分回到基础值 50", afterMiss.total == 50);
        }

        private static void LoopTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var loop = new LoopManager(config, data, 12345);

            Check("第 1 循环阈值为 3000", loop.ThresholdFor(1) == 3000);
            Check("第 4 循环仍为 3000", loop.ThresholdFor(4) == 3000);
            Check("第 5 循环提升到 15000", loop.ThresholdFor(5) == 15000);
            Check("第 10 循环仍为 15000", loop.ThresholdFor(10) == 15000);
            Check("第 11 循环按 1.08 倍递增为 16200", loop.ThresholdFor(11) == 16200);
            Check("第 12 循环阈值为 17496", loop.ThresholdFor(12) == 17496);
            Check("通关目标 1000000", loop.GoalScore == 1000000);

            loop.BeginAttempt();
            Check("首次循环齐射次数为 2~3 次", loop.VolleysThisAttempt >= 2 && loop.VolleysThisAttempt <= 3);
            loop.AddScore(3000);
            for (int i = 0; i < loop.VolleysThisAttempt; i++) loop.RegisterVolley();
            Check("齐射全部完成", loop.AttemptComplete);
            Check("3000 分达到首循环阈值", loop.MeetsThreshold);

            var failData = SaveData.CreateNew(config);
            failData.loopIndex = 5;
            var failLoop = new LoopManager(config, failData, 7);
            failLoop.BeginAttempt();
            failLoop.AddScore(14999);
            for (int i = 0; i < failLoop.VolleysThisAttempt; i++) failLoop.RegisterVolley();
            Check("14999 分未达到第 5 循环阈值", !failLoop.MeetsThreshold);

            var goalData = SaveData.CreateNew(config);
            goalData.totalScore = 1000000;
            goalData.EnsureIntegrity(config);
            var goalLoop = new LoopManager(config, goalData, 1);
            goalLoop.BeginAttempt();
            Check("通关后循环永远视为达标", goalData.goalReached && goalLoop.MeetsThreshold);
            Check("通关进度为 100%", Mathf.Approximately(goalLoop.GoalProgress, 1f));
        }

        private static void ShopTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            data.credits = 200000;
            var shop = new ShopManager(config, data, false);

            var first = shop.TryUnlockNextEnchant();
            Check("解锁第一档附魔成功", first.success && shop.EnchantTier == 1 && shop.BallMultiplier == 2);
            Check("第一档附魔扣费 8000", data.credits == 192000);

            var second = shop.TryUnlockNextEnchant();
            Check("解锁第二档附魔成功", second.success && shop.EnchantTier == 2 && shop.BallMultiplier == 5);
            Check("第二档附魔扣费 16000", data.credits == 176000);
            Check("附魔全部解锁后无法继续", shop.IsEnchantMaxed && !shop.TryUnlockNextEnchant().success);

            var explosion = shop.TryUnlockExplosion();
            Check("解锁引爆扣费 30000", explosion.success && shop.ExplosionUnlocked && data.credits == 146000);
            Check("引爆不会重复扣费", !shop.TryUnlockExplosion().success && data.credits == 146000);

            var reduce = shop.TryReduceBaffle();
            Check("移除挡板扣费 12000", reduce.success && shop.BaffleCount == 0 && data.credits == 134000);
            Check("没有挡板时无法继续移除", !shop.CanReduceBaffle && !shop.TryReduceBaffle().success);

            var extraMarble = shop.TryBuyExtraMarble();
            Check("购买额外弹珠后齐射数量增加", extraMarble.success && shop.CurrentVolleySize == 4 && data.credits == 129000);
            Check("第二颗额外弹珠价格按 1.6 倍递增", shop.ExtraMarblePrice == 8000);
            var secondExtraMarble = shop.TryBuyExtraMarble();
            Check("再次购买额外弹珠成功", secondExtraMarble.success && shop.CurrentVolleySize == 5 && data.credits == 121000);

            var cappedData = SaveData.CreateNew(config);
            cappedData.extraMarblesPerVolley = GameConfig.MaxVolleySize - config.VolleySize;
            cappedData.credits = 1000000;
            var cappedShop = new ShopManager(config, cappedData, false);
            Check("齐射达到 16 颗后不能继续购买", !cappedShop.CanBuyExtraMarble &&
                !cappedShop.TryBuyExtraMarble().success && cappedData.credits == 1000000);

            var poor = SaveData.CreateNew(config);
            var poorShop = new ShopManager(config, poor, false);
            Check("积分不足时无法兑换", !poorShop.TryUnlockNextEnchant().success &&
                !poorShop.TryUnlockExplosion().success && !poorShop.TryReduceBaffle().success &&
                !poorShop.TryBuyExtraMarble().success &&
                poor.credits == 0);
        }

        private static void MagazineTest(GameConfig config)
        {
            var magazine = MagazineBuilder.Build(config);
            Check("无限弹仓基础装填 3 颗", magazine.Count == 3);
            Check("弹仓全部是标准白球", magazine.TrueForAll(b => b.id == "ball_01"));
            Check("额外购买数量会加入弹仓", MagazineBuilder.Build(config, 2).Count == 5);
            Check("弹仓数量最多为 16 颗", MagazineBuilder.Build(config, GameConfig.MaxVolleySize).Count == GameConfig.MaxVolleySize);
        }

        private static void SaveRoundTripTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            data.totalScore = 67890;
            data.credits = 12345;
            data.bestSingleScore = 9876;
            data.bestVolleyScore = 45678;
            data.bestLoopScore = 7890;
            data.loopIndex = 7;
            data.goalReached = false;
            data.enchantTier = 1;
            data.extraMarblesPerVolley = 4;
            data.explosionUnlocked = true;
            data.baffleCount = 2;
            data.tiltCalibration = 0.12f;
            data.tiltInverted = true;

            string json = JsonUtility.ToJson(data);
            var copy = JsonUtility.FromJson<SaveData>(json);
            copy.EnsureIntegrity(config);
            Check("存档往返保留累计得分", copy.totalScore == 67890);
            Check("存档往返保留可用积分", copy.credits == 12345);
            Check("存档往返保留最高齐射/循环", copy.bestVolleyScore == 45678 && copy.bestLoopScore == 7890);
            Check("存档往返保留循环进度", copy.loopIndex == 7);
            Check("存档往返保留额外弹珠升级", copy.extraMarblesPerVolley == 4);
            Check("存档往返保留附魔/引爆/挡板",
                copy.enchantTier == 1 && copy.explosionUnlocked && copy.baffleCount == 2);
            Check("存档往返保留倾斜校准", Mathf.Approximately(copy.tiltCalibration, 0.12f) && copy.tiltInverted);

            var legacy = new SaveData { version = 4, totalScore = 123456, credits = 5000 };
            legacy.EnsureIntegrity(config);
            Check("旧存档迁移到 V6 后循环从第 1 轮开始", legacy.version == 6 && legacy.loopIndex == 1);
            Check("旧存档迁移后兑换进度重置",
                legacy.enchantTier == 0 && !legacy.explosionUnlocked &&
                legacy.baffleCount == config.economy.startingBaffleCount && legacy.extraMarblesPerVolley == 0);
        }

        private static void EconomyTest(GameConfig config)
        {
            Check("积分折算为得分的 100%", ScoreRules.CreditsFor(1000) == 1000);
            Check("同洞连击上限为 ×8", ScoreRules.ComboMultiplierFor(4) == 8 &&
                ScoreRules.ComboMultiplierFor(99) == ScoreRules.ComboCap);

            // 设计假设的八洞命中分布（奖励洞与空穴交替），用于验证单发期望收益。
            float[] probabilities = { 0.18f, 0.16f, 0.18f, 0.15f, 0.13f, 0.10f, 0.06f, 0.04f };
            float expectedStandard = 0f;
            for (int i = 0; i < config.holes.Length && i < probabilities.Length; i++)
            {
                expectedStandard += probabilities[i] * config.holes[i].multiplier * config.holes[i].basePoints;
            }

            Check("标准白球单发期望收益在 500~650 之间", expectedStandard >= 500f && expectedStandard <= 650f);
            Check("附魔 ×2 后单发期望收益翻倍到 1000 以上", expectedStandard * 2f >= 1000f && expectedStandard * 2f <= 1300f);
        }

        private static void Check(string name, bool condition)
        {
            if (condition)
            {
                _passed++;
                Debug.Log($"[SELFTEST] PASS · {name}");
            }
            else
            {
                _failed++;
                Debug.LogError($"[SELFTEST] FAIL · {name}");
            }
        }
    }
}
