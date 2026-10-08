# 坠遗之境 · Roguelite Demo

Unity 2D 动作游戏。玩家可以移动、跳跃、冲刺、进行三段连击，通过背包装备两把武器，并在战斗中切换。关卡包含巡逻敌人、远程敌人和双阶段 Boss。

## 打开项目

1. 使用 Unity Hub 添加此项目目录。
2. 安装 `ProjectSettings/ProjectVersion.txt` 指定的 Unity 版本（当前为 6000.5.7f1）。
3. 等待 Unity 根据 `Packages/manifest.json` 恢复依赖。
4. 打开 `Assets/Scenes/BootScene.unity` 并运行，进入主菜单和游戏。

项目使用 Input System、UGUI、Addressables、Cinemachine 和 URP。仓库保留源码、素材及 `.meta` 文件；Unity 缓存和构建产物由本地重新生成。

## 操作

| 按键 | 操作 |
| --- | --- |
| A / D | 移动 |
| W | 跳跃 |
| J | 攻击、连击 |
| Shift | 冲刺 |
| 1 / 2 / Q | 武器切换 |
| E | 拾取当前物品 |
| I | 打开/关闭背包 |
| H | 使用快捷栏药水 |
| F / G / R | 背包内装备、丢弃、卸下 |
| F5 | 保存进度 |
| Escape | 关闭背包或暂停/继续 |

击杀后按 F5 或在暂停菜单保存。主菜单“继续旅程”和死亡菜单“读取存档”恢复最近一次保存，包括已击败的敌人；“开始旅程”和暂停菜单“重新开始”会开启新游戏。

## 程序结构

- 角色：`PlayerMotor`、`PlayerCombat`、`PlayerActionController`、`PlayerAnimationController`。
- 拾取与背包：`PlayerPickupDetector`、`WorldItem`、`Inventory`、`EquipmentController`。
- UI：`UIManager` 单例、`InventoryPresenter` 事件刷新、`UIInputRouter` 输入映射、`UguiFeedbackButton` 交互反馈。
- 对象池：`SceneObjectPool<T>` 管理实例复用和缓存容量，敌人、投射物、特效各自管理业务状态。
- 敌人：`EnemyBrain`、`EnemyPerception`、固定生成点和敌人/投射物/特效对象池。
- Boss：`DragonKnightBoss`，攻击参数位于 `Assets/Data/Boss/DragonKnightAttacks.asset`。
- 存档：`GameSaveController`、`JsonSaveManager`；版本 2 支持稳定 ID、地面物品、固定敌人和 Boss 进度，兼容版本 1。
- 场景加载：`LoadingManager` 使用 Addressables。
- 音效与时间：`AudioDirector`、`CombatFeedback`、`GameTime`。

代码职责见 [程序结构](Docs/Architecture_Optimization.md)。UI 见 [页面管理](Docs/UGUI_Migration.md)、[按钮交互](Docs/UI_Interaction.md)和[美术与平台配置](Docs/UI_Art_and_Platforms.md)。资源生命周期和内存调整见 [资源与内存](Docs/Resource_Lifetime.md)。

## 构建

Unity 菜单 `Tools/Relicfall/构建资源内容` 构建 Addressables 内容；`Tools/Relicfall/构建 Windows 版本` 生成 Windows 游戏。

素材作者的说明、credits 和许可文件保留在各素材包原目录中。
