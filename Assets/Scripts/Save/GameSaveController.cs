using System.Collections.Generic;
using Relicfall.Combat;
using Relicfall.Items;
using Relicfall.Player;
using UnityEngine;
using Relicfall.UI;
using Relicfall.Environment;
using Relicfall.Enemies;
using Relicfall.Boss;

namespace Relicfall.Save
{
    [DefaultExecutionOrder(-170)]
    public sealed class GameSaveController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Health health;
        [SerializeField] private Inventory inventory;
        [SerializeField] private EquipmentController equipment;
        [SerializeField] private ItemData[] itemCatalog;
        [SerializeField] private WeaponData[] weaponCatalog;
        [SerializeField] private WorldItem worldItemPrefab;
        [SerializeField] private FixedEnemySpawner enemySpawner;
        [SerializeField] private DragonKnightBoss boss;
        [SerializeField] private BossEncounter bossEncounter;
        private readonly Dictionary<string, ItemData> itemsById = new();
        private readonly Dictionary<string, WeaponData> weaponsById = new();
        public GameSaveData PendingData { get; private set; }

        public static bool ContinueRequested { get; set; }
        public static GameSaveController Instance { get; private set; }
        public string Feedback { get; private set; }
        private float feedbackUntil;

        private void Awake()
        {
            Instance = this;
            PendingData = ContinueRequested ? JsonSaveManager.Read() : null;
            BuildCatalog();
        }
        private void BuildCatalog()
        {
            itemsById.Clear(); weaponsById.Clear();
            if (itemCatalog != null)
                foreach (ItemData item in itemCatalog)
                    if (item != null) { itemsById[item.StableId] = item; itemsById[item.LegacyName] = item; itemsById[item.name] = item; }
            if (weaponCatalog != null)
                foreach (WeaponData weapon in weaponCatalog)
                    if (weapon != null) { weaponsById[weapon.StableId] = weapon; weaponsById[weapon.LegacyName] = weapon; weaponsById[weapon.name] = weapon; }
        }

        private void Start()
        {
            if (!ContinueRequested)
            {
                WorldItem.ResetCollected();
                return;
            }
            ContinueRequested = false;
            GameSaveData saved = PendingData;
            if (saved == null)
            {
                WorldItem.ResetCollected();
                ShowFeedback("存档不可用，已开始新旅程");
                return;
            }
            RestoreProgress(saved);
            ShowFeedback("进度已读取");
        }

        private void Update()
        {
            if (UIManager.Existing != null && UIManager.Existing.Inputs.ConsumeSave())
                SaveNow();
            if (Time.unscaledTime > feedbackUntil) Feedback = null;
        }

        public bool SaveNow()
        {
            if (health == null || health.IsDead || player == null || inventory == null || equipment == null)
            {
                ShowFeedback("当前无法保存进度");
                return false;
            }
            bool result = JsonSaveManager.Write(CaptureProgress());
            ShowFeedback(result ? "进度已保存" : "保存失败，请查看控制台");
            return result;
        }

        public GameSaveData CaptureProgress()
        {
            var records = new List<SavedItem>();
            for (int i = 0; i < inventory.Capacity; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);
                records.Add(new SavedItem
                {
                    id = slot.IsEmpty ? string.Empty : slot.Item.StableId,
                    count = slot.IsEmpty ? 0 : slot.Count
                });
            }
            var data = new GameSaveData
            {
                levelRevision = ReferenceLevelLayout.Revision,
                position = player.position,
                health = health.CurrentHealth,
                weapon1 = equipment.Slot1 != null ? equipment.Slot1.StableId : string.Empty,
                weapon2 = equipment.Slot2 != null ? equipment.Slot2.StableId : string.Empty,
                activeWeapon = equipment.ActiveSlot,
                potion = inventory.EquippedPotion != null ? inventory.EquippedPotion.StableId : string.Empty,
                items = records.ToArray(),
                collectedWorldItems = WorldItem.CollectedIds,
                worldItems = WorldItem.CaptureRuntimeItems(),
                enemies = enemySpawner != null ? enemySpawner.CaptureProgress() : null,
                boss = boss != null ? boss.CaptureProgress(bossEncounter != null && bossEncounter.Started) : null
            };
            return data;
        }

        public void RestoreProgress(GameSaveData data)
        {
            if (data == null) return;
            // 旧地图的物品和装备仍可恢复，位置从新地图出生点开始。
            if (data.levelRevision == ReferenceLevelLayout.Revision)
                player.position = data.position;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = player.position;
            health.Restore(data.health);
            equipment.RestoreLoadout(FindWeapon(data.weapon1), FindWeapon(data.weapon2),
                data.activeWeapon);
            var items = new ItemData[inventory.Capacity];
            var counts = new int[inventory.Capacity];
            for (int i = 0; i < items.Length && data.items != null && i < data.items.Length; i++)
            {
                items[i] = FindItem(data.items[i]?.id);
                counts[i] = items[i] != null ? Mathf.Clamp(data.items[i].count, 0, items[i].MaxStack) : 0;
            }
            inventory.RestoreSlots(items, counts, FindItem(data.potion));
            player.GetComponent<PlayerActionController>()?.ResetAfterRestore();
            player.GetComponent<PlayerMotor>()?.ResetAfterRestore();
            player.GetComponent<PlayerAnimationController>()?.ResetAfterRestore();
            if (data.levelRevision == ReferenceLevelLayout.Revision)
            {
                WorldItem.RestoreCollected(data.collectedWorldItems);
                if (data.version >= 2)
                {
                    if (enemySpawner != null && enemySpawner.HasSpawned) enemySpawner.RestoreProgress(data.enemies);
                    WorldItem.RestoreRuntimeItems(data.worldItems, worldItemPrefab, FindItem);
                    if (boss != null && data.boss != null)
                    {
                        boss.RestoreProgress(data.boss, player.GetComponent<PlayerMotor>());
                        bossEncounter?.RestoreStarted(data.boss.started || data.boss.defeated);
                    }
                }
            }
            else WorldItem.ResetCollected();
            PlayerPickupDetector.Current?.Invalidate();
            Physics2D.SyncTransforms();
            UIManager.Existing?.SetOverlay(GameOverlay.None);
        }

        private ItemData FindItem(string id)
        {
            return !string.IsNullOrEmpty(id) && itemsById.TryGetValue(id, out ItemData item) ? item : null;
        }

        private WeaponData FindWeapon(string id)
        {
            return !string.IsNullOrEmpty(id) && weaponsById.TryGetValue(id, out WeaponData weapon) ? weapon : null;
        }

        private void ShowFeedback(string message)
        {
            Feedback = message;
            UIManager.Instance.ShowNotice(message);
            feedbackUntil = Time.unscaledTime + 2.5f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        public void ConfigureWorld(WorldItem worldPrefab, FixedEnemySpawner enemies, DragonKnightBoss bossTarget, BossEncounter encounter)
        { worldItemPrefab = worldPrefab; enemySpawner = enemies; boss = bossTarget; bossEncounter = encounter; }
        public void Configure(Transform target, Health life, Inventory bag,
            EquipmentController gear, ItemData[] items, WeaponData[] weapons)
        {
            player = target;
            health = life;
            inventory = bag;
            equipment = gear;
            itemCatalog = items;
            weaponCatalog = weapons;
        }
#endif
    }
}
