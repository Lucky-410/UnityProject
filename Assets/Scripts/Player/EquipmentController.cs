using System;
using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Player
{
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerInputReader))]
    public sealed class EquipmentController : MonoBehaviour
    {
        [SerializeField] private WeaponData slot1;
        [SerializeField] private WeaponData slot2;
        [SerializeField] private WeaponRenderer weaponRenderer;
        [SerializeField, Range(0, 1)] private int activeSlot;

        private PlayerCombat combat;
        private PlayerInputReader input;
        private Health health;
        private PlayerActionController actions;

        public int ActiveSlot => activeSlot;
        public WeaponData Slot1 => slot1;
        public WeaponData Slot2 => slot2;
        public WeaponData CurrentWeapon => activeSlot == 0 ? slot1 : slot2;
        public event Action<WeaponData, int> OnWeaponChanged;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
            actions = GetComponent<PlayerActionController>();
            ApplyCurrent();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (input.ConsumeSlot1()) SwitchTo(0);
            else if (input.ConsumeSlot2()) SwitchTo(1);
            else if (input.ConsumeWeaponSwitch()) SwitchTo(1 - activeSlot);
        }

        public bool SwitchTo(int index)
        {
            if (actions != null && !actions.CanSwitchWeapon) return false;
            if (index < 0 || index > 1) return false;
            WeaponData next = index == 0 ? slot1 : slot2;
            if (next == null) return false;
            return SelectSlot(index);
        }

        // 背包需要先选择装备目标，因此空武器栏也允许选中。
        public bool SelectSlot(int index)
        {
            if (index < 0 || index > 1) return false;
            activeSlot = index;
            ApplyCurrent();
            return true;
        }

        public WeaponData EquipWeapon(int index, WeaponData weapon)
        {
            if (index < 0 || index > 1 || weapon == null) return null;
            WeaponData previous = index == 0 ? slot1 : slot2;
            if (index == 0) slot1 = weapon;
            else slot2 = weapon;
            if (activeSlot == index) ApplyCurrent();
            return previous;
        }

        public WeaponData UnequipWeapon(int index)
        {
            if (index < 0 || index > 1) return null;
            WeaponData previous = index == 0 ? slot1 : slot2;
            if (index == 0) slot1 = null;
            else slot2 = null;
            if (activeSlot == index)
            {
                activeSlot = (index == 0 && slot2 != null) ? 1 : 0;
                ApplyCurrent();
            }
            return previous;
        }

        private void ApplyCurrent()
        {
            WeaponData weapon = CurrentWeapon;
            combat.SetWeapon(weapon);
            if (weaponRenderer != null) weaponRenderer.SetWeapon(weapon);
            OnWeaponChanged?.Invoke(weapon, activeSlot);
        }

        public void RestoreLoadout(WeaponData first, WeaponData second, int selected)
        {
            slot1 = first;
            slot2 = second;
            activeSlot = selected == 1 ? 1 : 0;
            ApplyCurrent();
        }

#if UNITY_EDITOR
        public void Configure(WeaponData first, WeaponData second, WeaponRenderer renderer)
        {
            slot1 = first;
            slot2 = second;
            weaponRenderer = renderer;
            activeSlot = 0;
        }
#endif
    }
}
