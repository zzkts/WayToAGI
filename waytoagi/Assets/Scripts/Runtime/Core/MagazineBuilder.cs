using System.Collections.Generic;

namespace Pachinko
{
    /// <summary>
    /// V3 弹仓：标准弹珠无限供应，每次齐射固定装填 economy.marblesPerVolley 颗。
    /// 不再读取库存、付费弹珠或分裂等级。
    /// </summary>
    public static class MagazineBuilder
    {
        public static List<BallDef> Build(GameConfig config)
        {
            var magazine = new List<BallDef>();
            if (config == null) return magazine;

            var standard = config.GetStandardBall();
            if (standard == null) return magazine;

            int count = config.VolleySize;
            for (int i = 0; i < count; i++) magazine.Add(standard);
            return magazine;
        }
    }
}
