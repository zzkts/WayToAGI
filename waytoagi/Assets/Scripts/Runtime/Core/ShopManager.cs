using System;
using UnityEngine;

namespace Pachinko
{
    public struct ShopResult
    {
        public bool success;
        public string message;
        public ShopResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }

    /// <summary>
    /// 购买弹珠、解锁弹珠、升级奖励洞。所有数值来自 game_config.json。
    /// </summary>
    public class ShopManager
    {
        private readonly GameConfig _config;
        private readonly SaveData _save;
        private readonly bool _persistToDisk;

        public int ShopVisitPurchaseCount { get; private set; }
        public int MaxPurchasePerVisit => _config.economy.maxBallsPerShopVisit;
        public int FreeStandardBalls => _config.economy.freeStandardBallsPerPhase;
        public int Credits => _save.credits;
        public SaveData Save => _save;
        public event Action Changed;

        public ShopManager(GameConfig config, SaveData save, bool persistToDisk = true)
        {
            _config = config;
            _save = save;
            _persistToDisk = persistToDisk;
            ShopVisitPurchaseCount = 0;
        }

        public void BeginShopVisit()
        {
            ShopVisitPurchaseCount = 0;
            Changed?.Invoke();
        }

        public void EndShopVisit()
        {
        }

        public bool IsUnlocked(string ballId)
        {
            var ball = _config.GetBall(ballId);
            if (ball == null) return false;
            return ball.alwaysAvailable || _save.IsBallUnlocked(ballId);
        }

        public int StockOf(string ballId)
        {
            var ball = _config.GetBall(ballId);
            if (ball == null) return 0;
            return ball.alwaysAvailable ? int.MaxValue : _save.GetStock(ballId);
        }

        public int HoleLevel(string holeId)
        {
            return _save.GetHoleLevel(holeId);
        }

        public int EnchantLevel(EnchantId id)
        {
            switch (id)
            {
                case EnchantId.Ember: return _save.emberLevel;
                case EnchantId.Frost: return _save.frostLevel;
                case EnchantId.Gild: return _save.gildLevel;
                default: return 0;
            }
        }

        public int BallSkillLevel(BallSkillId id)
        {
            switch (id)
            {
                case BallSkillId.Split: return _save.splitLevel;
                case BallSkillId.Bounce: return _save.bounceLevel;
                case BallSkillId.Magnet: return _save.magnetLevel;
                default: return 0;
            }
        }

        public int HoleSkillLevel(string holeId)
        {
            return _save.GetHoleSkillLevel(holeId);
        }

        public int EnchantUpgradePrice(EnchantId id)
        {
            return UpgradeCatalog.EnchantCost(id, EnchantLevel(id));
        }

        public int BallSkillUpgradePrice(BallSkillId id)
        {
            return UpgradeCatalog.BallSkillCost(id, BallSkillLevel(id));
        }

        public int HoleSkillUpgradePrice(string holeId)
        {
            return UpgradeCatalog.HoleSkillCost(holeId, HoleSkillLevel(holeId));
        }

        public bool IsEnchantMaxed(EnchantId id)
        {
            return EnchantLevel(id) >= UpgradeCatalog.MaxEnchantLevel;
        }

        public bool IsBallSkillMaxed(BallSkillId id)
        {
            return BallSkillLevel(id) >= UpgradeCatalog.MaxBallSkillLevel;
        }

        public bool IsHoleSkillMaxed(string holeId)
        {
            return HoleSkillLevel(holeId) >= UpgradeCatalog.MaxHoleSkillLevel;
        }

        public int HolePoints(HoleDef hole)
        {
            return hole.PointsAtLevel(HoleLevel(hole.id));
        }

        public int UpgradePrice(HoleDef hole)
        {
            return hole.UpgradePriceAtLevel(HoleLevel(hole.id));
        }

        public bool IsHoleMaxed(HoleDef hole)
        {
            return HoleLevel(hole.id) >= hole.maxUpgradeLevel;
        }

        public ShopResult TryUnlockBall(string ballId)
        {
            var ball = _config.GetBall(ballId);
            if (ball == null) return new ShopResult(false, "找不到该弹珠");
            if (ball.alwaysAvailable || IsUnlocked(ballId)) return new ShopResult(false, "该弹珠已解锁");
            if (_save.credits < ball.unlockPrice) return new ShopResult(false, $"积分不足，还差 {ball.unlockPrice - _save.credits}");

            _save.credits -= ball.unlockPrice;
            _save.AddUnlockedBall(ballId);
            Persist();
            return new ShopResult(true, $"已解锁「{ball.displayName}」");
        }

        public ShopResult TryBuyBall(string ballId, int quantity = 1)
        {
            var ball = _config.GetBall(ballId);
            if (ball == null) return new ShopResult(false, "找不到该弹珠");
            if (ball.alwaysAvailable) return new ShopResult(false, "标准弹珠免费无限供应，无需购买");
            if (!IsUnlocked(ballId)) return new ShopResult(false, "请先解锁该弹珠");
            if (ShopVisitPurchaseCount >= MaxPurchasePerVisit)
            {
                return new ShopResult(false, $"本次商店最多购买 {MaxPurchasePerVisit} 颗弹珠");
            }

            quantity = Mathf.Clamp(quantity, 1, MaxPurchasePerVisit - ShopVisitPurchaseCount);
            int cost = ball.unitPrice * quantity;
            if (_save.credits < cost)
            {
                return new ShopResult(false, $"积分不足，还差 {cost - _save.credits}");
            }

            _save.credits -= cost;
            _save.AddStock(ballId, quantity);
            ShopVisitPurchaseCount += quantity;
            Persist();
            return new ShopResult(true, $"装填 {ball.displayName} ×{quantity}（-{cost} 积分）");
        }

        public ShopResult TryUpgradeHole(HoleDef hole)
        {
            if (hole == null) return new ShopResult(false, "找不到该奖励洞");
            if (!hole.HasReward) return new ShopResult(false, "空穴没有可升级的奖励");
            int level = HoleLevel(hole.id);
            if (level >= hole.maxUpgradeLevel) return new ShopResult(false, "该奖励洞已满级");

            int price = UpgradePrice(hole);
            if (_save.credits < price)
            {
                return new ShopResult(false, $"积分不足，还差 {price - _save.credits}");
            }

            _save.credits -= price;
            _save.SetHoleLevel(hole.id, level + 1);
            Persist();
            return new ShopResult(true, $"{hole.displayName} 升级至 Lv.{level + 1}，积分 +{hole.upgradePointsPerLevel}");
        }

        public ShopResult TryUpgradeEnchant(EnchantId id)
        {
            int level = EnchantLevel(id);
            if (level >= UpgradeCatalog.MaxEnchantLevel) return new ShopResult(false, "该附魔已满级");
            int price = UpgradeCatalog.EnchantCost(id, level);
            if (_save.credits < price) return new ShopResult(false, $"积分不足，还差 {price - _save.credits}");

            _save.credits -= price;
            SetEnchantLevel(id, level + 1);
            Persist();
            return new ShopResult(true, $"{UpgradeCatalog.EnchantName(id)} 升至 Lv.{level + 1}");
        }

        public ShopResult TryUpgradeBallSkill(BallSkillId id)
        {
            int level = BallSkillLevel(id);
            if (level >= UpgradeCatalog.MaxBallSkillLevel) return new ShopResult(false, "该技能已满级");
            int price = UpgradeCatalog.BallSkillCost(id, level);
            if (_save.credits < price) return new ShopResult(false, $"积分不足，还差 {price - _save.credits}");

            _save.credits -= price;
            SetBallSkillLevel(id, level + 1);
            Persist();
            return new ShopResult(true, $"{UpgradeCatalog.BallSkillName(id)} 升至 Lv.{level + 1}");
        }

        public ShopResult TryUpgradeHoleSkill(string holeId)
        {
            var skill = UpgradeCatalog.HoleSkillFor(holeId);
            if (skill == HoleSkillId.None) return new ShopResult(false, "该奖励洞没有技能");
            int level = HoleSkillLevel(holeId);
            if (level >= UpgradeCatalog.MaxHoleSkillLevel) return new ShopResult(false, "该洞技能已满级");
            int price = UpgradeCatalog.HoleSkillCost(holeId, level);
            if (_save.credits < price) return new ShopResult(false, $"积分不足，还差 {price - _save.credits}");

            _save.credits -= price;
            _save.SetHoleSkillLevel(holeId, level + 1);
            Persist();
            return new ShopResult(true, $"{UpgradeCatalog.HoleSkillName(skill)} 升至 Lv.{level + 1}");
        }

        private void SetEnchantLevel(EnchantId id, int level)
        {
            switch (id)
            {
                case EnchantId.Ember: _save.emberLevel = level; break;
                case EnchantId.Frost: _save.frostLevel = level; break;
                case EnchantId.Gild: _save.gildLevel = level; break;
            }
        }

        private void SetBallSkillLevel(BallSkillId id, int level)
        {
            switch (id)
            {
                case BallSkillId.Split: _save.splitLevel = level; break;
                case BallSkillId.Bounce: _save.bounceLevel = level; break;
                case BallSkillId.Magnet: _save.magnetLevel = level; break;
            }
        }

        private void Persist()
        {
            if (_persistToDisk) SaveSystem.Save(_save);
            Changed?.Invoke();
        }
    }
}
