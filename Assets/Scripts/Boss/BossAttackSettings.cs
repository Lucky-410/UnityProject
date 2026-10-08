using UnityEngine;

namespace Relicfall.Boss
{
    [CreateAssetMenu(menuName = "Relicfall/Boss 攻击配置")]
    public sealed class BossAttackSettings : ScriptableObject
    {
        [Min(0.01f)] public float slashWindup = 0.30f, enragedSlashWindup = 0.20f;
        [Min(0.01f)] public float flameWindup = 0.48f, enragedFlameWindup = 0.33f;
        [Min(0.01f)] public float slashDuration = 0.38f, flameDuration = 0.85f, flameInterval = 0.23f;
        [Min(1)] public int slashDamage = 17, enragedSlashDamage = 22, flameDamage = 7, enragedFlameDamage = 9;
        [Min(0.01f)] public float slashReach = 0.90f, flameReach = 1.85f, enragedFlameReach = 2.25f;
    }
}
