using System.Collections.Generic;
using UnityEngine;

namespace Pachinko
{
    /// <summary>
    /// 弹仓：标准弹珠无限供应，每次齐射按基础数量加永久升级装填。
    /// 不再读取库存、付费弹珠或分裂等级。
    /// </summary>
    public static class MagazineBuilder
    {
        public static List<BallDef> Build(GameConfig config, int extraMarbles = 0)
        {
            var magazine = new List<BallDef>();
            if (config == null) return magazine;

            var standard = config.GetStandardBall();
            if (standard == null) return magazine;

            int count = Mathf.Min(GameConfig.MaxVolleySize, config.VolleySize + Mathf.Max(0, extraMarbles));
            for (int i = 0; i < count; i++) magazine.Add(standard);
            return magazine;
        }
    }
}
