using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Boss
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossEncounter : MonoBehaviour
    {
        [SerializeField] private DragonKnightBoss boss;
        public bool Started { get; private set; }
        public void RestoreStarted(bool started) => Started = started;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Started || boss == null) return;
            PlayerMotor player = other.GetComponentInParent<PlayerMotor>();
            if (player == null) return;
            Started = true;
            boss.Activate(player);
        }

#if UNITY_EDITOR
        public void Configure(DragonKnightBoss target) => boss = target;
#endif
    }
}
