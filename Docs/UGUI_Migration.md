# UGUI 与单例页面管理

日期：2026-10-08

项目所有游戏界面已迁移到 UGUI。UIManager 是跨场景单例，拥有一个 Canvas 和一个使用 InputSystemUIInputModule 的 EventSystem。CanvasScaler 按 1280×720 参考分辨率缩放，并使用安全区域定位。

## 页面预制体

`Assets/Resources/UI/RelicfallUI.prefab` 已配置在 BootScene 中。直接运行 GameScene 时，也会由单例入口加载该预制体。

层级：

```text
UIManager
├─ Canvas
│  ├─ Backdrop
│  └─ SafeArea
│     ├─ MainMenu / Controls
│     ├─ HUD / Inventory
│     ├─ Boss / DamageNumbers
│     ├─ GameOverlay
│     ├─ Notice / PickupPrompt
│     └─ Loading
└─ EventSystem
```

页面由 GameObject.SetActive 与 CanvasGroup 管理，实际控件使用 Image、RawImage、Text、Button 和 Slider。控件只在初始化时创建或绑定，随后更新数据；按钮通过 onClick 调用业务逻辑。已有预制体节点的 RectTransform 布局在运行绑定时保留。

## 脚本职责

- `UIManager.cs`：单例、页面互斥、暂停/加载输入门控、跨场景绑定与解绑、提示和伤害数字。
- `InventoryUguiView.cs`：背包与 HUD 的控件绑定、数据刷新、装备栏高亮与分类布局。
- `UguiTheme.cs`：UGUI 控件创建辅助、样式、字体和图标。
- `InventoryUI.cs`：背包快捷键与业务操作，保留空武器栏的选择能力。
- `MainMenuUI.cs`、`GameUIScreen.cs`、`BossEncounterUI.cs`：场景业务与 UIManager 的桥接。
- `LoadingManager.cs`：Addressables 加载与重试，将显示交给 UIManager。
- `GameSaveController.cs`、`CombatFeedback.cs`：通过 UIManager 显示存档反馈和伤害数字。

## 常用入口

```csharp
UIManager.Instance.InventoryAction(InventoryUIAction.Open);
UIManager.Instance.CloseInventory();
UIManager.Instance.SetOverlay(GameOverlay.Pause);
UIManager.Instance.SetOverlay(GameOverlay.None);
UIManager.Instance.ShowNotice("进度已保存");
```

暂停、死亡、胜利、加载阻止游戏操作。背包开启时只关闭玩家输入，游戏时间继续。输入的启停由管理器协调，背包开关不会干扰命中停顿。

## 验证与维护

菜单 `Tools/Relicfall/生成 UGUI 页面预制体` 重新生成默认页面并更新 BootScene。

运行验证逻辑已移出项目。编辑器工具仅保留手动生成预制体的菜单，不订阅运行模式事件，不自动开始/停止游戏，也不自动恢复场景。

验证结果：`Docs/UIValidation/Result.txt`。截图：`MainMenu.png`、`Inventory.png`、`Pause.png`、`BossAndHud.png`。

当前源码没有 OnGUI 或 GUI 绘制入口。

现有 PixelUIkit 的 8 张面板、标题、格子、头像框、进度条和图标素材已全部接入页面预制体；背包加入可见性与数据变化检查，拾取提示取消每帧开关。详见 `Docs/UI_Art_and_Platforms.md`。

按钮交互已统一到 `UguiFeedbackButton`，覆盖经过、键盘焦点、业务选中、按下与禁用，并支持暂停期间的渐变反馈。详见 `Docs/UI_Interaction.md`。

后续结构优化增加 `InventoryPresenter` 事件刷新和 `UIInputRouter` 独立快捷键映射；时间管理集中到 `GameTime`。详见 `Docs/Architecture_Optimization.md`。

此前自动验证的事件订阅未正确清理，会在正常进入运行模式时再次触发检查并退出游戏；场景恢复回调也可能在运行模式尚未退出时执行。2026-10-08 已移除这些回调，并在 UIManager 的新运行会话初始化中恢复 Time.timeScale=1，防止禁用域重载时继承上次暂停状态。

修复后以普通启动方式复测：直接 GameScene 启动，以及 BootScene → MainMenuScene → GameScene 均能保持运行；加载错误为空，12 个敌人、玩家动画与 UGUI 样式资源已就绪；玩家输入和 InputActionMap 已启用，移动逻辑、背包开关及输入恢复已验证。临时诊断代码已移出 Assets。

最新证据见 `Docs/UIValidation/Result.txt` 与 `RecoveryFromBoot.json`、`RecoveryInventoryOpen.json`、`RecoveryMovement.txt`、`Recovery.png`。原失败记录保留在 `PriorAutomationFailure.txt`。
