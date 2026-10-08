using Relicfall.Boss;
using Relicfall.Combat;
using Relicfall.Items;
using Relicfall.Player;
using UnityEngine;

namespace Relicfall.UI
{
    public enum GameOverlay { None, Pause, DeathPending, Death, Victory }

    public sealed class GameUIScreen : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private Inventory inventory;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private DragonKnightBoss boss;
        public static bool BlocksGameplay => UIManager.BlocksGameplay;

        private void OnEnable()
        {
            if (playerHealth != null) playerHealth.OnDeath += ShowDeath;
            if (boss != null) boss.OnVictory += ShowVictory;
            UIManager.Instance.BindGame(this);
        }
        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.OnDeath -= ShowDeath;
            if (boss != null) boss.OnVictory -= ShowVictory;
            UIManager.Existing?.UnbindGame(this);
        }
        private void ShowDeath() => UIManager.Instance.SetOverlay(GameOverlay.DeathPending);
        private void ShowVictory() => UIManager.Instance.SetOverlay(GameOverlay.Victory);
        public void ApplyGameplayGate(bool blocked, bool bagOpen)
        {
            bool alive = playerHealth != null && !playerHealth.IsDead;
            if (playerInput != null) playerInput.enabled = !blocked && !bagOpen && alive;
            if (inventory != null) inventory.enabled = !blocked && alive;
        }
#if UNITY_EDITOR
        public void Configure(Health health, PlayerInputReader input, Inventory items,
            InventoryUI bag, DragonKnightBoss enemy)
        {
            playerHealth = health;
            playerInput = input;
            inventory = items;
            inventoryUI = bag;
            boss = enemy;
        }
#endif
    }
}
