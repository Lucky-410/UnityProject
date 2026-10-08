using UnityEngine;

namespace Relicfall.Enemies
{
    [DefaultExecutionOrder(-200)]
    public sealed class GamePoolBootstrap : MonoBehaviour
    {
        [SerializeField] private EnemyPool enemies;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private VFXPool effects;

        private void Awake()
        {
            if (enemies == null || projectiles == null || effects == null)
            {
                Debug.LogError("对象池配置不完整。", this);
                return;
            }
            enemies.Initialize();
            projectiles.Initialize();
            effects.Initialize();
        }

#if UNITY_EDITOR
        public void Configure(EnemyPool enemyPool, ProjectilePool projectilePool, VFXPool effectPool)
        {
            enemies = enemyPool;
            projectiles = projectilePool;
            effects = effectPool;
        }
#endif
    }
}
