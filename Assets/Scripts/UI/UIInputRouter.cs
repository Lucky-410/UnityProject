using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Relicfall.UI
{
    // UI/全局快捷键独立于玩家 Gameplay Map；按上下文排队，每个操作只消费一次。
    public sealed class UIInputRouter : MonoBehaviour
    {
        private InputActionMap commands;
        private readonly Queue<InventoryUIAction> inventory = new();
        private bool inGame, bagOpen, blocked, loading, menus;
        private bool toggle, pickup, save, cancel, navigation;
        private int step;
        private void Awake() => Initialize();
        private void OnEnable() { Initialize(); commands.Enable(); }
        private void OnDisable() { commands?.Disable(); Clear(); }
        private void OnDestroy() => commands?.Dispose();
        public void Initialize()
        {
            if (commands != null || !Application.isPlaying) return;
            commands = new InputActionMap("InterfaceAndGlobal");
            Add("Inventory", "<Keyboard>/i", () => { if (CanGame) toggle = true; });
            Add("Pickup", "<Keyboard>/e", () => { if (CanGame && !bagOpen) pickup = true; });
            Add("Potion", "<Keyboard>/h", () => { if (CanGame) Enqueue(InventoryUIAction.Potion); });
            Add("Equip", "<Keyboard>/f", () => Bag(InventoryUIAction.Equip));
            Add("Drop", "<Keyboard>/g", () => Bag(InventoryUIAction.Drop));
            Add("Unequip", "<Keyboard>/r", () => Bag(InventoryUIAction.Unequip));
            Add("Slot1", "<Keyboard>/1", () => Bag(InventoryUIAction.FirstWeapon));
            Add("Slot2", "<Keyboard>/2", () => Bag(InventoryUIAction.SecondWeapon));
            Add("Save", "<Keyboard>/f5", () => { if (inGame && !loading) save = true; });
            InputAction back = Add("Cancel", "<Keyboard>/escape", () => { if (!loading) cancel = true; });
            back.AddBinding("<Gamepad>/buttonEast");
            InputAction move = commands.AddAction("Navigate", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/dpad");
            move.performed += context =>
            {
                Vector2 direction = context.ReadValue<Vector2>();
                if (loading || direction.sqrMagnitude < 0.1f) return;
                if (bagOpen && CanGame) step = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ?
                    (direction.x > 0 ? 1 : -1) : (direction.y > 0 ? -6 : 6);
                else if (menus) navigation = true;
            };
        }
        private InputAction Add(string name, string binding, Action action)
        {
            InputAction input = commands.AddAction(name, InputActionType.Button, binding);
            input.performed += _ => action();
            return input;
        }
        private bool CanGame => inGame && !blocked && !loading;
        private void Enqueue(InventoryUIAction action) { if (inventory.Count < 16) inventory.Enqueue(action); }
        private void Bag(InventoryUIAction action) { if (CanGame && bagOpen) Enqueue(action); }
        public void Configure(bool game, bool bag, bool block, bool load, bool menu)
        {
            if (inGame != game || bagOpen != bag || blocked != block || loading != load || menus != menu) Clear();
            inGame = game; bagOpen = bag; blocked = block; loading = load; menus = menu;
        }
        private void Clear() { toggle = pickup = save = cancel = navigation = false; step = 0; inventory.Clear(); }
        private static bool Consume(ref bool value) { bool result = value; value = false; return result; }
        public bool ConsumeToggle() => Consume(ref toggle);
        public bool ConsumePickup() => Consume(ref pickup);
        public bool ConsumeSave() => Consume(ref save);
        public bool ConsumeCancel() => Consume(ref cancel);
        public bool ConsumeNavigation() => Consume(ref navigation);
        public int ConsumeStep() { int value = step; step = 0; return value; }
        public bool TryConsumeInventory(out InventoryUIAction action)
        {
            if (inventory.Count == 0) { action = InventoryUIAction.None; return false; }
            action = inventory.Dequeue(); return true;
        }
    }
}
