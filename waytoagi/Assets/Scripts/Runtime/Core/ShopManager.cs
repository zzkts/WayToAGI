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
    /// V3 兑换系统：只保留附魔解锁、引爆技巧、减少挡板三项永久兑换。
    /// 标准弹珠无限供应，因此不再有弹珠购买、库存与奖励洞升级。
    /// </summary>
    public class ShopManager
    {
        private readonly GameConfig _config;
        private readonly SaveData _save;
        private readonly bool _persistToDisk;

        public int Credits => _save.credits;
        public SaveData Save => _save;
        public event System.Action Changed;

        public ShopManager(GameConfig config, SaveData save, bool persistToDisk = true)
        {
            _config = config;
            _save = save;
            _persistToDisk = persistToDisk;
        }

        public int EnchantTier => _save.enchantTier;
        public int MaxEnchantTier => _config.MaxEnchantTier;
        public EnchantDef ActiveEnchant => _config.GetEnchant(_save.enchantTier);
        public EnchantDef NextEnchant => _config.GetEnchant(_save.enchantTier + 1);
        public int BallMultiplier => ActiveEnchant != null ? Mathf.Max(1, ActiveEnchant.multiplier) : 1;
        public bool IsEnchantMaxed => _save.enchantTier >= _config.MaxEnchantTier;
        public int EnchantPrice => _config.EnchantPrice(_save.enchantTier);

        public bool ExplosionUnlocked => _save.explosionUnlocked;
        public int ExplosionPrice => _config.explosion.unlockPrice;

        public int BaffleCount => _save.baffleCount;
        public int MaxBaffleCount => _config.MaxBaffleCount;
        public int BaffleReducePrice => _config.baffle.reduceCost;
        public bool CanReduceBaffle => _save.baffleCount > 0;

        /// <summary>解锁下一档附魔；附魔会替换弹珠颜色、拖尾颜色与得分倍率。</summary>
        public ShopResult TryUnlockNextEnchant()
        {
            if (IsEnchantMaxed) return new ShopResult(false, "附魔已全部解锁");
            var next = NextEnchant;
            if (next == null) return new ShopResult(false, "没有可解锁的附魔");
            int price = EnchantPrice;
            if (_save.credits < price)
            {
                return new ShopResult(false, $"可用积分不足，还差 {price - _save.credits:N0}");
            }

            _save.credits -= price;
            _save.enchantTier++;
            Persist();
            return new ShopResult(true, $"已解锁「{next.displayName}」· 弹珠倍率 ×{next.multiplier}");
        }

        /// <summary>解锁引爆：每累计碰撞一定次数，冲击附近弹珠。</summary>
        public ShopResult TryUnlockExplosion()
        {
            if (_save.explosionUnlocked) return new ShopResult(false, "引爆技巧已解锁");
            int price = ExplosionPrice;
            if (_save.credits < price)
            {
                return new ShopResult(false, $"可用积分不足，还差 {price - _save.credits:N0}");
            }

            _save.credits -= price;
            _save.explosionUnlocked = true;
            Persist();
            return new ShopResult(true, $"已解锁「引爆技巧」· 每 {_config.explosion.triggerCollisions} 次碰撞触发");
        }

        /// <summary>花费积分移除一块挡板，最低减到 0 块。</summary>
        public ShopResult TryReduceBaffle()
        {
            if (!CanReduceBaffle) return new ShopResult(false, "已经没有挡板可以移除");
            int price = BaffleReducePrice;
            if (_save.credits < price)
            {
                return new ShopResult(false, $"可用积分不足，还差 {price - _save.credits:N0}");
            }

            _save.credits -= price;
            _save.baffleCount = Mathf.Max(0, _save.baffleCount - 1);
            Persist();
            return new ShopResult(true, $"已移除一块挡板，剩余 {_save.baffleCount} 块");
        }

        private void Persist()
        {
            if (_persistToDisk) SaveSystem.Save(_save);
            Changed?.Invoke();
        }
    }
}
