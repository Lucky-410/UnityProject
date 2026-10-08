# UGUI 页面管理

UIManager 是常驻单例。启动场景使用 Assets/Resources/UI/RelicfallUI.prefab；直接运行其他场景时按需创建。页面和控件初始化一次，显隐使用 GameObject.SetActive，不重复加载预制体。

Canvas 使用 Screen Space Overlay，CanvasScaler 按 1280×720 参考尺寸缩放，并适配安全区域。EventSystem 使用 InputSystemUIInputModule。

| 页面 | 内容 |
| --- | --- |
| MainMenu / Controls | 主菜单、继续旅程和操作说明 |
| HUD / Inventory | 生命、冲刺进度、装备栏、背包格和物品详情 |
| Boss | 首领名称、阶段和血量 |
| GameOverlay | 暂停、死亡和胜利菜单 |
| Notice / PickupPrompt | 操作反馈与拾取目标 |
| Loading | Addressables 进度、错误和重试 |
| DamageNumbers | 固定容量的伤害文字 |

MainMenuUI、GameUIScreen、InventoryUI、BossEncounterUI 在启停时绑定或解绑场景数据。InventoryPresenter 按数据变化刷新背包；DamageNumberPresenter 单独管理伤害文字的复用和移动。

打开背包会关闭 Gameplay 输入；暂停、死亡和胜利页面会阻止游戏操作。菜单初始无选中项，第一次方向导航才建立焦点。关闭页面时清除其中的 EventSystem 选择。

返回主菜单后清除场景引用、装备缓存和图标引用，避免常驻 Canvas 持有上一关卡资源。

手动生成入口为“Tools/Relicfall/生成 UGUI 页面预制体”。布局修改后执行此菜单更新预制体。按钮参数可在 UguiFeedbackButton 的 Inspector 中调整。
