using Relicfall.UI;
using UnityEngine;

namespace Relicfall.Boss
{
    public sealed class BossEncounterUI : MonoBehaviour
    {
        [SerializeField] private DragonKnightBoss boss;
        private void OnEnable() => UIManager.Instance.BindBoss(this, boss);
        private void OnDisable() => UIManager.Existing?.UnbindBoss(this);
#if UNITY_EDITOR
        public void Configure(DragonKnightBoss target) => boss = target;
#endif
    }
}
