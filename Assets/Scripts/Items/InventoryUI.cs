using Relicfall.Combat;
using Relicfall.Player;
using Relicfall.UI;
using UnityEngine;
using System;

namespace Relicfall.Items
{
    [RequireComponent(typeof(Inventory))]
    public sealed class InventoryUI : MonoBehaviour
    {
        private Inventory inventory;
        private Health health;
        private PlayerInputReader playerInput;
        private EquipmentController equipment;
        private PlayerMotor motor;
        private InventoryViewData view;
        private bool open;
        private int selected, filter;
        private readonly System.Collections.Generic.List<int> visibleSlots = new();
        public bool IsOpen => open;
        public bool IsDead => health == null || health.IsDead;
        public int SelectedSlot => selected;
        public int Filter => filter;
        public float DashReady => motor != null ? motor.DashReadyRatio : 1;
        public event Action ViewChanged;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            health = GetComponent<Health>();
            playerInput = GetComponent<PlayerInputReader>();
            equipment = GetComponent<EquipmentController>();
            motor = GetComponent<PlayerMotor>();
            Sprite[] portraits = Resources.LoadAll<Sprite>("pixramen-valley-asset-pack-v2/pixramen-valley-asset-pack-v2/dark-knight/png/1x/dark-knight-idle/dark-knight-idle1");
            view = new InventoryViewData { Slots = new InventorySlot[inventory.Capacity],
                Portrait = portraits.Length > 0 ? portraits[0] : null };
        }
        private void OnEnable()
        {
            inventory.OnFeedback += ShowFeedback;
            inventory.OnChanged += Changed;
            health.OnHealthChanged += HealthChanged;
            equipment.OnWeaponChanged += WeaponChanged;
            UIManager.Instance.BindInventory(this);
        }
        private void OnDisable()
        {
            if (inventory != null) inventory.OnFeedback -= ShowFeedback;
            if (inventory != null) inventory.OnChanged -= Changed;
            if (health != null) health.OnHealthChanged -= HealthChanged;
            if (equipment != null) equipment.OnWeaponChanged -= WeaponChanged;
            UIManager.Existing?.UnbindInventory(this);
            open = false;
        }
        private void Changed() => ViewChanged?.Invoke();
        private void HealthChanged(int current, int maximum) => Changed();
        private void WeaponChanged(WeaponData weapon, int slot) => Changed();
        public void Close() => SetOpen(false);
        private void SetOpen(bool value) => UIManager.Instance.SetInventoryOpen(this, value);
        public void ApplyOpen(bool value)
        {
            open = value;
            if (value) GetComponent<PlayerActionController>()?.CancelForInterface();
        }
        public void ApplyInputGate(bool blocked)
        {
            if (playerInput != null) playerInput.enabled = !blocked && !open && !IsDead;
            if (inventory != null) inventory.enabled = !blocked && !IsDead;
        }

        private void Update()
        {
            UIInputRouter keys = UIManager.Existing != null ? UIManager.Existing.Inputs : null;
            if (keys == null || GameUIScreen.BlocksGameplay || SceneTransition.IsLoading || IsDead) return;
            if (keys.ConsumeToggle()) SetOpen(!open);
            while (keys.TryConsumeInventory(out InventoryUIAction action)) Handle(action);
            if (!open) return;
            int step = keys.ConsumeStep();
            if (step != 0)
            {
                visibleSlots.Clear();
                for (int i = 0; i < inventory.Capacity; i++)
                    if (RelicfallInventoryView.Matches(inventory.GetSlot(i), filter)) visibleSlots.Add(i);
                if (visibleSlots.Count > 0)
                {
                    int cursor = Mathf.Max(0, visibleSlots.IndexOf(selected));
                    selected = visibleSlots[(cursor + step % visibleSlots.Count + visibleSlots.Count) % visibleSlots.Count];
                    Changed();
                }
            }
        }

        public InventoryViewData ReadViewData()
        {
            view.Health = health.CurrentHealth;
            view.MaxHealth = health.MaxHealth;
            view.DashReady = motor != null ? motor.DashReadyRatio : 1;
            view.FirstWeapon = equipment.Slot1;
            view.FirstName = inventory.EquippedWeaponName(0);
            view.SecondWeapon = equipment.Slot2;
            view.SecondName = inventory.EquippedWeaponName(1);
            view.ActiveWeapon = equipment.ActiveSlot;
            view.Potion = inventory.EquippedPotion;
            view.PotionCount = view.Potion != null ? inventory.Count(view.Potion) : 0;
            for (int i = 0; i < inventory.Capacity; i++) view.Slots[i] = inventory.GetSlot(i);
            return view;
        }
        public void SelectSlot(int index)
        {
            if (open && index >= 0 && index < inventory.Capacity &&
                RelicfallInventoryView.Matches(inventory.GetSlot(index), filter)) { selected = index; Changed(); }
        }
        public void SetFilter(int value)
        {
            filter = Mathf.Clamp(value, 0, 2);
            Changed();
            if (RelicfallInventoryView.Matches(inventory.GetSlot(selected), filter)) return;
            for (int i = 0; i < inventory.Capacity; i++)
                if (RelicfallInventoryView.Matches(inventory.GetSlot(i), filter)) { selected = i; return; }
        }
        public void Handle(InventoryUIAction action)
        {
            if (IsDead || GameUIScreen.BlocksGameplay || SceneTransition.IsLoading) return;
            bool choice = RelicfallInventoryView.Matches(inventory.GetSlot(selected), filter);
            switch (action)
            {
                case InventoryUIAction.Open: SetOpen(true); break;
                case InventoryUIAction.Close: Close(); break;
                case InventoryUIAction.Equip: if (open && choice) inventory.Equip(selected); break;
                case InventoryUIAction.Drop: if (open && choice) inventory.Drop(selected); break;
                case InventoryUIAction.Unequip: if (open) inventory.UnequipWeapon(); break;
                case InventoryUIAction.FirstWeapon: SelectWeaponSlot(0); break;
                case InventoryUIAction.SecondWeapon: SelectWeaponSlot(1); break;
                case InventoryUIAction.Potion: inventory.UsePotion(); break;
            }
        }
        private void SelectWeaponSlot(int index)
        {
            if (open) equipment.SelectSlot(index);
            else equipment.SwitchTo(index);
        }
        private void ShowFeedback(string message) => UIManager.Instance.ShowNotice(message);
#if UNITY_EDITOR
        public void Configure(Sprite panel) { }
#endif
    }
}
