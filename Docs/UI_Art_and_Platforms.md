# UI 美术与单向平台优化

2026-10-08

## UI 素材接入

现有 `Assets/Resources/PixelUIkit/Pixel UI kit/PNGs` 中的 8 张素材已接入真实 UGUI 预制体：

| 素材 | 用途 |
| --- | --- |
| Base Bg 1 | 主菜单、背包、暂停等金边面板 |
| Base Bg 3 | 普通按钮 |
| Header | 页面标题条 |
| Small Bg | 背包物品格、快捷装备栏 |
| Round Bg B | 背包角色头像框 |
| Progress Bar Top | 生命、冲刺、Boss 和加载进度条外框 |
| Icons/Heart、Icons/Energy | HUD 生命和冲刺图标 |

`UguiTheme.cs` 保留素材原色，并按素材配置九宫格边距；图标保持宽高比。物品格的选中反馈现由 `UguiFeedbackButton` 的金色边框与角标统一控制，详见 `Docs/UI_Interaction.md`。预制体在 `Assets/Resources/UI/RelicfallUI.prefab`，仍由 `UIManager` 单例管理。

`InventoryUguiView.cs` 在页面可见时刷新，并缓存物品、数量和选中状态；数据不变时不重新布局背包或改写图标。`UIManager.cs` 不再每帧关闭再打开拾取提示。

## 单向平台

`GameScene` 中 13 组确认适合上穿的单层悬空台阶，共 94 个瓦片，已从 `Ground_Tilemap` 拆到 `OneWayPlatforms_Tilemap`。瓦片资源、颜色、变换、渲染材质和排序保持一致。

新 Tilemap 使用静态 Rigidbody2D、TilemapCollider2D、CompositeCollider2D 和 PlatformEffector2D。复合碰撞器开启 Used By Effector，效应器开启 Use One Way、Use One Way Grouping；Surface Arc 为 160°，排除水平侧面碰撞。合并相邻瓦片的碰撞边，减少台阶内部接缝。

多层地面、墙体、洞顶和斜坡保持实体碰撞。`GroundDetector.cs` 对单向平台的接触和向下 Cast 增加上升速度与脚底位置判断，避免穿越时误判支撑。

`Assets/Editor/RelicfallLevelTools.cs` 的手动菜单“Tools/Relicfall/配置单向悬空平台”可重复执行；先检查指定瓦片位置，避免地图修改后迁移部分瓦片。该工具不自动运行，不控制运行模式。

## 验证证据

- `Docs/UIValidation/ArtAndPlatforms.txt`：8 张 UI 素材的预制体引用，以及隔离 PhysicsScene2D 中真实平台瓦片的向上穿越、上方落地、侧边和脚部支撑检查。
- `Docs/UIValidation/ArtRuntime.txt`：主菜单进入游戏后的加载、单例、HUD、背包选择 2 号栏及关闭后输入恢复记录。
- `Docs/UIValidation/ArtHud.png`、`ArtInventory.png`：实际游戏画面。

平台物理检查使用与关卡相同的 Grid 单元尺寸；检查场景独立于正常关卡，不写存档。临时检查脚本在验证结束后移出 Assets。

本次 15 项素材/碰撞检查全部通过：平台落地脚底为 1.616（表面约 1.600，包含物理接触间距），侧向穿越结束 X 为 10.700。正常游戏 HUD 可见，背包选择 2 号栏及关闭后玩家输入恢复均通过。
