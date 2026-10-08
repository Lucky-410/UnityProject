using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Player
{
    [DefaultExecutionOrder(-160)]
    public sealed class PlayerContext : MonoBehaviour
    {
        public static PlayerContext Current { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public Health Health { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = null;
        private void Awake() { Motor = GetComponent<PlayerMotor>(); Health = GetComponent<Health>(); }
        private void OnEnable() => Current = this;
        private void OnDisable() { if (Current == this) Current = null; }
    }
}
