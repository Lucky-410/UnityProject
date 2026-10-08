using Relicfall.Combat;
using Relicfall.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Relicfall.UI
{
    public sealed class InventoryUguiView : MonoBehaviour
    {
        private sealed class SlotView
        {
            public UguiFeedbackButton Button;
            public Image Icon;
            public Text Count;
            private Sprite lastSprite;
            private int lastCount = int.MinValue;
            private bool lastSelected, initialized;
            public void Refresh(Sprite sprite, int count, bool selected)
            {
                if (initialized && lastSprite == sprite && lastCount == count && lastSelected == selected) return;
                initialized = true;
                lastSprite = sprite;
                lastCount = count;
                lastSelected = selected;
                UguiTheme.Icon(Icon, sprite);
                Count.text = count >= 0 ? count.ToString() : "";
                Button.SetChosen(selected);
            }
        }

        public CanvasGroup Hud { get; private set; }
        public CanvasGroup Bag { get; private set; }
        private Slider healthBar, dashBar, bagHealth;
        private Text healthText, dashText, capacityText, bagCapacity, weaponName, weaponStats;
        private Text itemName, itemDetail, equipText;
        private Image portrait, itemIcon;
        private Button equipButton, unequipButton, dropButton;
        private readonly SlotView[] hudQuick = new SlotView[3];
        private readonly SlotView[] bagQuick = new SlotView[3];
        private readonly SlotView[] slots = new SlotView[12];
        private readonly Button[] filters = new Button[3];
        private UIManager owner;
        private int lastHealth = -1, lastMaxHealth = -1, lastUsed = -1, lastCapacity = -1;
        private int lastSelected = -1, lastFilter = -1, lastActive = -1, lastPotionCount = -1;
        private bool dashWasReady, hudRefreshed, bagWasVisible;
        private WeaponData lastFirst, lastSecond;
        private ItemData lastPotion;
        private readonly ItemData[] lastItems = new ItemData[12];
        private readonly int[] lastCounts = new int[12];

        private SlotView CreateSlot(Transform parent, string name, float x, float y, float w, float h,
            string key, System.Action click)
        {
            var button = (UguiFeedbackButton)UguiTheme.Button(parent, name, x, y, w, h, "", click);
            button.image.sprite = UguiTheme.Style("Small Bg");
            var icon = UguiTheme.Image(UguiTheme.Place(button.transform, "Icon", 12, 12, w - 24, h - 24), Color.white);
            UguiTheme.Label(button.transform, "Key", 6, 2, 44, 20, key, 14, UguiTheme.Muted);
            Text count = UguiTheme.Label(button.transform, "Count", 4, h - 23, w - 10, 20,
                "", 14, UguiTheme.Pale, TextAnchor.MiddleRight);
            button.transform.Find("StateFrame").SetAsLastSibling();
            button.transform.Find("ChosenMark").SetAsLastSibling();
            return new SlotView { Button = button, Icon = icon, Count = count };
        }

        public void Build(UIManager manager, Transform safeArea)
        {
            owner = manager;
            Hud = UguiTheme.Page(safeArea, "HUD");
            healthBar = UguiTheme.Bar(Hud.transform, "HealthBar", 66, 28, 244, 30, UguiTheme.Red);
            healthText = UguiTheme.Label(Hud.transform, "Health", 76, 29, 224, 28, "", 14,
                UguiTheme.Pale, TextAnchor.MiddleCenter);
            UguiTheme.Art(Hud.transform, "HealthIcon", 24, 25, 36, 32, "Icons/Heart");
            dashBar = UguiTheme.Bar(Hud.transform, "DashBar", 66, 71, 206, 28, UguiTheme.Blue);
            dashText = UguiTheme.Label(Hud.transform, "Dash", 74, 72, 190, 26, "", 14,
                UguiTheme.Pale, TextAnchor.MiddleCenter);
            UguiTheme.Art(Hud.transform, "DashIcon", 28, 69, 28, 30, "Icons/Energy");
            Button bagOpen = UguiTheme.Button(Hud.transform, "OpenBag", -182, 25, 158, 40,
                "行囊 · I", () => owner.InventoryAction(InventoryUIAction.Open));
            var openRect = (RectTransform)bagOpen.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(1, 1);
            capacityText = bagOpen.transform.Find("Label").GetComponent<Text>();
            RectTransform quick = UguiTheme.Panel(Hud.transform, "QuickSlots", -194, -114, 388, 92);
            quick.anchorMin = quick.anchorMax = new Vector2(0.5f, 0);
            hudQuick[0] = CreateSlot(quick, "Weapon1", 14, 12, 68, 68, "1", () => owner.InventoryAction(InventoryUIAction.FirstWeapon));
            hudQuick[1] = CreateSlot(quick, "Weapon2", 88, 12, 68, 68, "2", () => owner.InventoryAction(InventoryUIAction.SecondWeapon));
            hudQuick[2] = CreateSlot(quick, "Potion", 176, 12, 68, 68, "H", () => owner.InventoryAction(InventoryUIAction.Potion));
            UguiTheme.Button(quick, "OpenBag", 262, 20, 110, 49, "行囊 · I", () => owner.InventoryAction(InventoryUIAction.Open));

            Bag = UguiTheme.Page(safeArea, "Inventory", true);
            RectTransform container = UguiTheme.Place(Bag.transform, "Content", -476, -257, 952, 514,
                new Vector2(0.5f, 0.5f));
            RectTransform gear = UguiTheme.Panel(container, "Equipment", 0, 0, 268, 506);
            UguiTheme.Header(gear, "Heading", 12, 10, 244, 44, "旅者装备");
            UguiTheme.Art(gear, "PortraitFrame", 39, 72, 190, 190, "Round Bg B");
            portrait = UguiTheme.Image(UguiTheme.Place(gear, "Portrait", 92, 100, 88, 138), Color.white);
            UguiTheme.Label(gear, "HealthHeading", 28, 286, 212, 25, "当前生命", 14, UguiTheme.Muted);
            bagHealth = UguiTheme.Bar(gear, "Health", 24, 314, 220, 27, UguiTheme.Red);
            bagQuick[0] = CreateSlot(gear, "Weapon1", 24, 356, 62, 62, "1", () => owner.InventoryAction(InventoryUIAction.FirstWeapon));
            bagQuick[1] = CreateSlot(gear, "Weapon2", 100, 356, 62, 62, "2", () => owner.InventoryAction(InventoryUIAction.SecondWeapon));
            bagQuick[2] = CreateSlot(gear, "Potion", 180, 356, 62, 62, "H", () => owner.InventoryAction(InventoryUIAction.Potion));
            weaponName = UguiTheme.Label(gear, "WeaponName", 24, 429, 220, 26, "", 18, UguiTheme.Gold);
            weaponStats = UguiTheme.Label(gear, "WeaponStats", 24, 460, 220, 25, "", 14, UguiTheme.Muted);

            RectTransform bag = UguiTheme.Panel(container, "Bag", 284, 0, 658, 506);
            UguiTheme.Header(bag, "Heading", 20, 10, 566, 44, "行囊");
            UguiTheme.Button(bag, "Close", 608, 9, 36, 34, "×", () => owner.InventoryAction(InventoryUIAction.Close));
            string[] names = { "全部", "武器", "补给" };
            for (int i = 0; i < filters.Length; i++)
            {
                int index = i;
                filters[i] = UguiTheme.Button(bag, "Filter" + i, 20 + i * 146, 62, 138, 38,
                    names[i], () => owner.SetInventoryFilter(index));
            }
            bagCapacity = UguiTheme.Label(bag, "Capacity", 472, 65, 166, 30, "", 14,
                UguiTheme.Muted, TextAnchor.MiddleRight);
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                slots[i] = CreateSlot(bag, "Slot" + i, 20 + (i % 6) * 104, 113 + (i / 6) * 90,
                    94, 80, (i + 1).ToString("00"), () => owner.SelectInventorySlot(index));
            }
            itemIcon = UguiTheme.Image(UguiTheme.Place(bag, "ItemIcon", 30, 326, 48, 48), Color.white);
            itemName = UguiTheme.Label(bag, "ItemName", 104, 312, 514, 31, "", 18, UguiTheme.Gold);
            itemDetail = UguiTheme.Label(bag, "ItemDetail", 104, 350, 514, 51, "", 14, UguiTheme.Muted);
            equipButton = UguiTheme.Button(bag, "Equip", 20, 416, 198, 42, "装备 · F", () => owner.InventoryAction(InventoryUIAction.Equip));
            equipText = equipButton.transform.Find("Label").GetComponent<Text>();
            unequipButton = UguiTheme.Button(bag, "Unequip", 228, 416, 198, 42, "卸下武器 · R", () => owner.InventoryAction(InventoryUIAction.Unequip));
            dropButton = UguiTheme.Button(bag, "Drop", 436, 416, 198, 42, "丢弃一件 · G", () => owner.InventoryAction(InventoryUIAction.Drop));
            UguiTheme.Label(bag, "Help", 20, 471, 614, 23,
                "方向键选择  ·  1 / 2 选择装备栏  ·  I / Esc 关闭", 14, UguiTheme.Muted, TextAnchor.MiddleCenter);
            // 背包方向键沿用物品格选择，不同时触发 UGUI 的空间导航。
            foreach (Button button in Bag.GetComponentsInChildren<Button>(true))
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            foreach (Button button in Hud.GetComponentsInChildren<Button>(true))
                button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        public void Refresh(InventoryViewData data, int selected, int filter)
        {
            bool healthChanged = lastHealth != data.Health || lastMaxHealth != data.MaxHealth;
            int used = data.UsedSlots;
            bool capacityChanged = lastUsed != used || lastCapacity != data.Slots.Length;
            if (Hud.gameObject.activeSelf)
            {
                if (healthChanged || !hudRefreshed)
                {
                    healthBar.SetValueWithoutNotify((float)data.Health / Mathf.Max(1, data.MaxHealth));
                    healthText.text = $"生命  {data.Health} / {data.MaxHealth}";
                }
                RefreshDash(data.DashReady);
                if (capacityChanged || !hudRefreshed) capacityText.text = $"行囊 {used}/{data.Slots.Length} · I";
                RefreshQuick(hudQuick, data);
                hudRefreshed = true;
            }
            else hudRefreshed = false;
            lastHealth = data.Health;
            lastMaxHealth = data.MaxHealth;
            lastUsed = used;
            lastCapacity = data.Slots.Length;
            if (!Bag.gameObject.activeSelf) { bagWasVisible = false; return; }
            if (healthChanged || !bagWasVisible)
                bagHealth.SetValueWithoutNotify((float)data.Health / Mathf.Max(1, data.MaxHealth));
            bool changed = !bagWasVisible || capacityChanged || lastSelected != selected || lastFilter != filter ||
                lastActive != data.ActiveWeapon || lastFirst != data.FirstWeapon || lastSecond != data.SecondWeapon ||
                lastPotion != data.Potion || lastPotionCount != data.PotionCount;
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = i < data.Slots.Length ? data.Slots[i] : null;
                ItemData item = slot != null ? slot.Item : null;
                int count = slot != null ? slot.Count : 0;
                changed |= lastItems[i] != item || lastCounts[i] != count;
                lastItems[i] = item;
                lastCounts[i] = count;
            }
            bagWasVisible = true;
            if (!changed) return;
            lastSelected = selected;
            lastFilter = filter;
            lastActive = data.ActiveWeapon;
            lastFirst = data.FirstWeapon;
            lastSecond = data.SecondWeapon;
            lastPotion = data.Potion;
            lastPotionCount = data.PotionCount;
            bagCapacity.text = $"{used} / {data.Slots.Length} 格";
            RefreshQuick(bagQuick, data);
            UguiTheme.Icon(portrait, data.Portrait);
            WeaponData equipped = data.ActiveWeapon == 0 ? data.FirstWeapon : data.SecondWeapon;
            weaponName.text = equipped != null ? (data.ActiveWeapon == 0 ? data.FirstName : data.SecondName)
                : $"{data.ActiveWeapon + 1} 号武器栏 · 空";
            weaponStats.text = equipped != null ? $"伤害 {equipped.Damage}  ·  攻速 {equipped.AttackSpeed:0.0}"
                : "选择背包武器后装备";
            int visible = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                bool show = i < data.Slots.Length && RelicfallInventoryView.Matches(data.Slots[i], filter);
                if (slots[i].Button.gameObject.activeSelf != show) slots[i].Button.gameObject.SetActive(show);
                if (!show) continue;
                var rect = (RectTransform)slots[i].Button.transform;
                rect.anchoredPosition = new Vector2(20 + (visible % 6) * 104, -(113 + (visible / 6) * 90));
                InventorySlot slot = data.Slots[i];
                slots[i].Refresh(slot.IsEmpty ? null : slot.Item.Icon, slot.IsEmpty ? -1 : slot.Count, i == selected);
                visible++;
            }
            for (int i = 0; i < filters.Length; i++)
            {
                ((UguiFeedbackButton)filters[i]).SetChosen(filter == i);
            }
            InventorySlot chosen = selected >= 0 && selected < data.Slots.Length ? data.Slots[selected] : null;
            bool choice = chosen != null && !chosen.IsEmpty && RelicfallInventoryView.Matches(chosen, filter);
            UguiTheme.Icon(itemIcon, choice ? chosen.Item.Icon : null);
            itemName.text = choice ? chosen.Item.DisplayName : visible == 0 ? "暂无这类物品" : "选择一件物品";
            itemDetail.text = !choice ? "先选左侧 1 / 2 号武器栏，再选择武器装备。" :
                chosen.Item.Kind == ItemKind.Weapon && chosen.Item.Weapon != null ?
                $"武器 · 伤害 {chosen.Item.Weapon.Damage} · 攻速 {chosen.Item.Weapon.AttackSpeed:0.0}\n装备至 {data.ActiveWeapon + 1} 号武器栏，旧武器放回行囊。" :
                chosen.Item.Kind == ItemKind.Potion ? $"补给 · 恢复 {chosen.Item.HealAmount} 点生命\n放入快捷栏后，按 H 使用。" : "材料 · 可携带或丢弃";
            equipText.text = choice && chosen.Item.Kind == ItemKind.Potion ? "放入快捷栏" : $"装备至 {data.ActiveWeapon + 1} 号栏 · F";
            equipButton.interactable = choice && chosen.Item.Kind != ItemKind.Material;
            unequipButton.interactable = equipped != null;
            dropButton.interactable = choice;
        }

        private static void RefreshQuick(SlotView[] quick, InventoryViewData data)
        {
            quick[0].Refresh(data.FirstWeapon?.Icon, -1, data.ActiveWeapon == 0);
            quick[1].Refresh(data.SecondWeapon?.Icon, -1, data.ActiveWeapon == 1);
            quick[2].Refresh(data.Potion?.Icon, data.PotionCount, false);
        }
        public void RefreshDash(float ratio)
        {
            if (!Hud.gameObject.activeInHierarchy) return;
            dashBar.SetValueWithoutNotify(ratio);
            bool ready = ratio >= 1;
            if (ready != dashWasReady || !hudRefreshed) dashText.text = ready ? "冲刺就绪 · Shift" : "冲刺恢复中";
            dashWasReady = ready;
        }
    }
}
