using UnityEngine;
using UnityEngine.UI;

namespace Pachinko
{
    /// <summary>
    /// 单个底部洞位的可重绑定视觉。洞位坐标固定，奖励洞会在每次齐射前重新分布，
    /// 因此洞口、光晕、编号与积分标签都需要跟着当前 HoleDef 刷新。
    /// </summary>
    public class HoleVisual
    {
        public SpriteRenderer Bay;
        public SpriteRenderer BayInner;
        public SpriteRenderer BayEdge;
        public SpriteRenderer Ring;
        public SpriteRenderer Mouth;
        public SpriteRenderer Inner;
        public SpriteRenderer Sheen;
        public SpriteRenderer Glow;
        public Text NumberLabel;
        public Text PointsLabel;
        public Image Pill;

        public void Apply(HoleDef def)
        {
            bool reward = def != null && def.HasReward;
            Color color = def != null ? Theme.Hex(def.color) : Theme.Hex("#6E655C");

            if (Bay != null) Bay.color = color.Alpha(reward ? 0.16f : 0.07f);
            if (BayInner != null) BayInner.color = Theme.Ink.Alpha(0.075f);
            if (BayEdge != null) BayEdge.color = color.Alpha(reward ? 0.65f : 0.28f);
            if (Ring != null) Ring.color = reward ? Theme.Darken(color, 0.06f) : Theme.Hex("#5B5148");
            if (Mouth != null) Mouth.color = Theme.Ink;
            if (Inner != null) Inner.color = Theme.Hex("#1D130D");
            if (Sheen != null) Sheen.color = color.Alpha(reward ? 0.28f : 0.12f);
            if (NumberLabel != null) NumberLabel.color = Theme.Cream.Alpha(reward ? 0.88f : 0.42f);
            if (PointsLabel != null) PointsLabel.color = Theme.Cream.Alpha(reward ? 1f : 0.62f);
            if (Pill != null) Pill.color = Theme.Ink.Alpha(reward ? 0.9f : 0.4f);

            if (Glow != null)
            {
                Glow.gameObject.SetActive(reward);
                Glow.color = color.Alpha(0.3f);
            }
        }
    }
}
