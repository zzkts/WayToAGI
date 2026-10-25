using System;
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
            Debug.Log("========== 弹珠机 MVP 数值自检 ==========");

            try
            {
                string path = "Assets/StreamingAssets/game_config.json";
                var config = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));
                config.Validate();

                ConfigTest(config);
                ScoreTest(config);
                ComboResetTest(config);
                ScoringSystemTest(config);
                UpgradeTest(config);
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
            Check("配置有三个弹珠", config.balls != null && config.balls.Length == 3);
            Check("配置有八个洞", config.holes != null && config.holes.Length == 8);
            Check("八个洞全部位于发射道隔板左侧", config.holes[7].positionX < BoardGeometry.LauncherWallX - 0.15f);
            Check("奖励洞与空穴 1:1 且交替排列",
                config.GetRewardHoles().Length == 4 &&
                config.holes[0].HasReward && !config.holes[1].HasReward &&
                config.holes[2].HasReward && !config.holes[3].HasReward &&
                config.holes[4].HasReward && !config.holes[5].HasReward &&
                config.holes[6].HasReward && !config.holes[7].HasReward);
            Check("基础弹珠免费", config.GetBall("ball_01").alwaysAvailable && config.GetBall("ball_01").unitPrice == 0);
            Check("强化弹珠倍率 ×2", config.GetBall("ball_02").multiplier == 2);
            Check("稀有弹珠倍率 ×3", config.GetBall("ball_03").multiplier == 3);
            Check("四奖励洞倍率为 1/3/8/12",
                config.holes[0].multiplier == 1 && config.holes[2].multiplier == 3 &&
                config.holes[4].multiplier == 8 && config.holes[6].multiplier == 12);
            Check("四奖励洞基础积分为 50/120/260/300",
                config.holes[0].basePoints == 50 && config.holes[2].basePoints == 120 &&
                config.holes[4].basePoints == 260 && config.holes[6].basePoints == 300);
            Check("空穴不产生积分且不可升级",
                config.holes[1].multiplier == 0 && config.holes[1].basePoints == 0 && config.holes[1].maxUpgradeLevel == 0);
            Check("默认经济配置每阶段免费装填 3 颗标准弹珠", config.economy.freeStandardBallsPerPhase == 3);
            Check("默认经济配置单次商店最多购买 5 颗", config.economy.maxBallsPerShopVisit == 5);
        }

        private static void ScoreTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var score = new ScoreManager(data);
            var ball = config.GetBall("ball_02");
            var hole = config.holes[6];

            var r1 = score.Score(ball, hole, hole.basePoints);
            Check("首球基础分 2×12×300=7200，并触发头奖 +20000",
                r1.subtotal == 7200 && r1.jackpotBonus == 20000 && r1.total == 27200);
            Check("首球连锁 ×1、同洞 ×1、未狂热",
                r1.chainMultiplier == 1 && r1.comboMultiplier == 1 && !r1.feverActive);

            var r2 = score.Score(ball, hole, hole.basePoints);
            Check("同洞第2次 ×2、连锁 ×2", r2.comboMultiplier == 2 && r2.chainMultiplier == 2);
            Check("第2球头奖不重复，总分 28800", r2.jackpotBonus == 0 && r2.total == 28800);

            var r3 = score.Score(ball, hole, hole.basePoints);
            Check("同洞第3次 ×4、连锁 ×3", r3.comboMultiplier == 4 && r3.chainMultiplier == 3 && r3.total == 86400);
            var r4 = score.Score(ball, hole, hole.basePoints);
            Check("同洞第4次 ×8、连锁 ×4", r4.comboMultiplier == 8 && r4.chainMultiplier == 4 && r4.total == 230400);

            var r5 = score.Score(ball, hole, hole.basePoints);
            Check("连续第5次命中触发狂热 ×2", r5.feverActive && r5.feverMultiplier == 2);
            Check("狂热 + 连锁 ×5 + 同洞 ×8 = 576000", r5.total == 576000);

            var r6 = score.Score(ball, hole, hole.basePoints);
            Check("第6次连锁封顶 ×5，狂热继续", r6.chainMultiplier == 5 && r6.feverActive && r6.total == 576000);
            Check("本轮累计得分正确", data.totalScore == 1524800);
            Check("积分按 20% 硬编码比例结算（含 2000 初始积分）", data.credits == 306960);
            Check("最高单次记录更新为 576000", data.bestSingleScore == 576000);
        }

        private static void ComboResetTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var score = new ScoreManager(data);
            var ball = config.GetBall("ball_01");
            var h7 = config.holes[6];
            var h1 = config.holes[0];

            score.Score(ball, h7, h7.basePoints);
            score.Score(ball, h7, h7.basePoints);
            var switched = score.Score(ball, h1, h1.basePoints);
            Check("换洞后同洞连击从 ×1 重新开始", switched.comboMultiplier == 1 && switched.comboCount == 1);
            Check("换洞不会中断全局连锁", switched.chainMultiplier == 3);
            Check("换洞得分按新洞计算", switched.subtotal == 50);

            score.RegisterMiss();
            var afterMiss = score.Score(ball, h1, h1.basePoints);
            Check("漏球后连锁与同洞连击同时归零",
                afterMiss.chainMultiplier == 1 && afterMiss.comboMultiplier == 1 && !afterMiss.feverActive);
            Check("漏球后首球得分回到基础值", afterMiss.total == 50);
        }

        private static void ScoringSystemTest(GameConfig config)
        {
            Check("第一档里程碑为 250000", ScoreRules.Milestones[0] == 250000);
            Check("共配置 7 档里程碑", ScoreRules.MilestoneCount == 7);
            Check("积分折算为得分的 20%", ScoreRules.CreditsFor(1000) == 200);
            Check("全中奖励为单轮得分的 50%", ScoreRules.AllHitBonusFor(100000) == 50000);

            var data = SaveData.CreateNew(config);
            var score = new ScoreManager(data);
            var ball = config.GetBall("ball_02");
            var hole = config.holes[6];

            var first = score.Score(ball, hole, hole.basePoints);
            Check("每轮首次命中 7 号洞触发头奖", first.jackpotBonus == 20000);

            score.RegisterMiss();
            var afterMiss = score.Score(ball, hole, hole.basePoints);
            Check("漏球不会重置本轮头奖状态", afterMiss.jackpotBonus == 0);

            score.ResetCombo();
            var nextVolley = score.Score(ball, hole, hole.basePoints);
            Check("进入下一轮后头奖重新可用", nextVolley.jackpotBonus == 20000);

            int beforeBonus = data.totalScore;
            int creditsBefore = data.credits;
            score.AwardBonus(1000);
            Check("额外奖励计入累计得分", data.totalScore == beforeBonus + 1000);
            Check("额外奖励按 20% 计入积分", data.credits == creditsBefore + 200);

            Check("最高单轮记录可刷新", score.RecordVolleyScore(5000) && data.bestVolleyScore == 5000);
            Check("较低单轮不会覆盖记录", !score.RecordVolleyScore(4000) && data.bestVolleyScore == 5000);

            data.milestoneIndex = 99;
            data.EnsureIntegrity(config);
            Check("存档钳制非法里程碑索引", data.milestoneIndex == ScoreRules.MilestoneCount);
        }

        private static void UpgradeTest(GameConfig config)
        {
            Check("初始积分 2000", SaveData.CreateNew(config).credits == 2000);
            Check("附魔/技能/洞技能价格基数正确",
                UpgradeCatalog.EnchantCost(EnchantId.Ember, 0) == 1500 &&
                UpgradeCatalog.BallSkillCost(BallSkillId.Split, 0) == 2000 &&
                UpgradeCatalog.HoleSkillCost("hole_01", 0) == 1500);
            Check("附魔上限 5 级、弹珠技能与洞技能上限 3 级",
                UpgradeCatalog.MaxEnchantLevel == 5 &&
                UpgradeCatalog.MaxBallSkillLevel == 3 &&
                UpgradeCatalog.MaxHoleSkillLevel == 3);

            var data = SaveData.CreateNew(config);
            data.credits = 100000;
            var shop = new ShopManager(config, data, false);
            shop.BeginShopVisit();
            Check("商店附魔升级成功", shop.TryUpgradeEnchant(EnchantId.Ember).success && data.emberLevel == 1);
            Check("商店弹珠技能升级成功", shop.TryUpgradeBallSkill(BallSkillId.Split).success && data.splitLevel == 1);
            Check("商店奖励洞技能升级成功", shop.TryUpgradeHoleSkill("hole_03").success && data.GetHoleSkillLevel("hole_03") == 1);

            data.splitLevel = 1;
            var splitMagazine = MagazineBuilder.Build(config, data);
            Check("分裂技能额外装填 1 颗标准弹", splitMagazine.Count == 4 &&
                splitMagazine.FindAll(b => b.id == "ball_01").Count == 4);

            var emberData = SaveData.CreateNew(config);
            emberData.emberLevel = 1;
            var emberScore = new ScoreManager(emberData);
            var b1 = config.GetBall("ball_01");
            var h1 = config.holes[0];
            emberScore.Score(b1, h1, h1.basePoints);
            emberScore.Score(b1, h1, h1.basePoints);
            var emberHit = emberScore.Score(b1, h1, h1.basePoints);
            Check("炽焰第 3 层触发爆发 +300", emberHit.emberBonus == 300);

            var bountyData = SaveData.CreateNew(config);
            bountyData.SetHoleSkillLevel("hole_01", 1);
            var bountyScore = new ScoreManager(bountyData);
            var bounty = bountyScore.Score(b1, config.holes[0], config.holes[0].basePoints);
            Check("1 号洞赏金技能额外 +120 积分", bounty.bountyCredits == 120);

            var gildData = SaveData.CreateNew(config);
            gildData.gildLevel = 1;
            int before = gildData.credits;
            var gildScore = new ScoreManager(gildData);
            var gildHit = gildScore.Score(b1, config.holes[0], config.holes[0].basePoints);
            int baseCredits = Mathf.RoundToInt(ScoreRules.CreditsFor(gildHit.total) * 1.1f);
            Check("金辉附魔使积分获取 +10%", gildData.credits - before == baseCredits);

            var jackpotData = SaveData.CreateNew(config);
            jackpotData.SetHoleSkillLevel("hole_07", 1);
            var jackpotScore = new ScoreManager(jackpotData);
            var jackpotHit = jackpotScore.Score(config.GetBall("ball_02"), config.holes[6], config.holes[6].basePoints);
            Check("头奖核心使头奖 +25%", jackpotHit.jackpotBonus == 25000);

            Check("洞技能只挂在四个奖励洞上",
                UpgradeCatalog.HoleSkillFor("hole_01") == HoleSkillId.Bounty &&
                UpgradeCatalog.HoleSkillFor("hole_03") == HoleSkillId.Aegis &&
                UpgradeCatalog.HoleSkillFor("hole_05") == HoleSkillId.SplitEcho &&
                UpgradeCatalog.HoleSkillFor("hole_07") == HoleSkillId.JackpotCore &&
                UpgradeCatalog.HoleSkillFor("hole_02") == HoleSkillId.None);
            Check("护盾与裂变技能数值", UpgradeCatalog.ShieldCount(2) == 2 && UpgradeCatalog.SplitEchoCount(2) == 2);
            Check("弹跳与磁引数值", Mathf.Approximately(UpgradeCatalog.BounceMultiplier(2), 1.16f) &&
                Mathf.Approximately(UpgradeCatalog.MagnetAcceleration(2), 2f));
        }

        private static void ShopTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            data.credits = 200000;
            var shop = new ShopManager(config, data, false);
            shop.BeginShopVisit();

            var unlock = shop.TryUnlockBall("ball_02");
            Check("解锁强化弹珠成功", unlock.success);
            Check("解锁扣费 800", data.credits == 199200);
            Check("强化弹珠进入已解锁列表", shop.IsUnlocked("ball_02"));

            var buy = shop.TryBuyBall("ball_02", 1);
            Check("购买强化弹珠成功", buy.success);
            Check("购买扣费 500 且库存为 1", data.credits == 198700 && shop.StockOf("ball_02") == 1);
            Check("商店阶段购买计数为 1", shop.ShopVisitPurchaseCount == 1);

            var hole = config.GetHole("hole_01");
            int p0 = shop.HolePoints(hole);
            var up1 = shop.TryUpgradeHole(hole);
            int p1 = shop.HolePoints(hole);
            Check("1号洞升级一次成功", up1.success && shop.HoleLevel(hole.id) == 1);
            Check("1号洞积分增加 25", p1 == p0 + 25);
            Check("1号洞升级价格 300", data.credits == 198700 - 300);

            var up2 = shop.TryUpgradeHole(hole);
            Check("1号洞升级二次价格递增为 480", up2.success && data.credits == 198700 - 300 - 480);
            var up3 = shop.TryUpgradeHole(hole);
            Check("1号洞可升到三级", up3.success && shop.HoleLevel(hole.id) == 3);
            var up4 = shop.TryUpgradeHole(hole);
            Check("1号洞三级封顶", !up4.success && shop.HoleLevel(hole.id) == 3);
            Check("满级后积分为 125", shop.HolePoints(hole) == 125);

            shop.TryBuyBall("ball_02", 4);
            Check("本次商店最多购买 5 颗", shop.ShopVisitPurchaseCount == 5);
            var over = shop.TryBuyBall("ball_02", 1);
            Check("超过 5 颗后购买被拒绝", !over.success);

            shop.BeginShopVisit();
            var unlock3 = shop.TryUnlockBall("ball_03");
            Check("解锁稀有弹珠成功", unlock3.success);
            var buy3 = shop.TryBuyBall("ball_03", 1);
            Check("稀有弹珠单价 1000", buy3.success && shop.StockOf("ball_03") == 1);
            Check("弹珠库存可消耗", data.ConsumeStock("ball_03", 1) && data.GetStock("ball_03") == 0);
            Check("库存不足时不能消耗", !data.ConsumeStock("ball_03", 1));
        }

        private static void MagazineTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            var emptyMagazine = MagazineBuilder.Build(config, data);
            Check("无付费弹珠时弹仓只有 3 颗免费标准弹珠",
                emptyMagazine.Count == 3 && emptyMagazine.TrueForAll(b => b.id == "ball_01"));

            data.AddUnlockedBall("ball_02");
            data.AddUnlockedBall("ball_03");
            data.AddStock("ball_02", 3);
            data.AddStock("ball_03", 1);
            var magazine = MagazineBuilder.Build(config, data);
            Check("购买的全部付费弹珠一起装填", magazine.Count == 7);
            Check("弹仓先装强化再装稀有，最后补免费标准",
                magazine[0].id == "ball_02" && magazine[1].id == "ball_02" && magazine[2].id == "ball_02" &&
                magazine[3].id == "ball_03" && magazine[4].id == "ball_01" && magazine[6].id == "ball_01");

            var counts = MagazineBuilder.CountByBall(magazine);
            Check("弹仓计数：强化 3 / 稀有 1 / 标准 3",
                counts["ball_02"] == 3 && counts["ball_03"] == 1 && counts["ball_01"] == 3);
        }

        private static void SaveRoundTripTest(GameConfig config)
        {
            var data = SaveData.CreateNew(config);
            data.credits = 12345;
            data.totalScore = 67890;
            data.bestSingleScore = 9876;
            data.bestVolleyScore = 45678;
            data.milestoneIndex = 3;
            data.emberLevel = 2;
            data.frostLevel = 1;
            data.gildLevel = 3;
            data.splitLevel = 2;
            data.bounceLevel = 1;
            data.magnetLevel = 3;
            data.AddUnlockedBall("ball_03");
            data.AddStock("ball_03", 4);
            data.SetHoleLevel("hole_03", 2);
            data.SetHoleSkillLevel("hole_05", 2);
            string json = JsonUtility.ToJson(data);
            var copy = JsonUtility.FromJson<SaveData>(json);
            copy.EnsureIntegrity(config);
            Check("存档 JSON 往返保留累计得分", copy.totalScore == 67890);
            Check("存档 JSON 往返保留可用积分", copy.credits == 12345);
            Check("存档 JSON 往返保留最高单轮", copy.bestVolleyScore == 45678);
            Check("存档 JSON 往返保留里程碑进度", copy.milestoneIndex == 3);
            Check("存档 JSON 往返保留附魔等级",
                copy.emberLevel == 2 && copy.frostLevel == 1 && copy.gildLevel == 3);
            Check("存档 JSON 往返保留弹珠技能等级",
                copy.splitLevel == 2 && copy.bounceLevel == 1 && copy.magnetLevel == 3);
            Check("存档 JSON 往返保留奖励洞技能等级", copy.GetHoleSkillLevel("hole_05") == 2);
            Check("存档 JSON 往返保留解锁状态", copy.IsBallUnlocked("ball_03"));
            Check("存档 JSON 往返保留弹珠库存", copy.GetStock("ball_03") == 4);
            Check("存档 JSON 往返保留奖励洞等级", copy.GetHoleLevel("hole_03") == 2);
        }

        private static void EconomyTest(GameConfig config)
        {
            // 设计假设的八洞命中分布（奖励洞与空穴交替），用于验证经济模型。实际命中率受物理与玩家倾斜影响。
            float[] probabilities = { 0.18f, 0.16f, 0.18f, 0.15f, 0.13f, 0.10f, 0.06f, 0.04f };
            float expectedStandard = 0f;
            for (int i = 0; i < config.holes.Length && i < probabilities.Length; i++)
            {
                expectedStandard += probabilities[i] * config.holes[i].multiplier * config.holes[i].basePoints;
            }

            float expectedBall2 = expectedStandard * config.GetBall("ball_02").multiplier;
            float expectedBall3 = expectedStandard * config.GetBall("ball_03").multiplier;
            Check("标准弹珠期望收益在 500~650 之间", expectedStandard >= 500f && expectedStandard <= 650f);
            Check("期望收益 ball_02 投入产出比 > 1.3", expectedBall2 / 500f >= 1.3f);
            Check("期望收益 ball_03 投入产出比 > 1.3", expectedBall3 / 1000f >= 1.3f);
            Check("默认经济配置每次购买上限为 5", config.economy.maxBallsPerShopVisit == 5);
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
