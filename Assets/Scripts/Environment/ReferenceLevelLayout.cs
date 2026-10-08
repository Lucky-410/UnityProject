using UnityEngine;

namespace Relicfall.Environment
{
    // 关卡布局版本用于阻止旧地图存档把玩家放进重建后的墙体。
    public sealed class ReferenceLevelLayout : MonoBehaviour
    {
        public const int Revision = 2;
    }
}
