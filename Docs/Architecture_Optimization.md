# 程序结构与逻辑优化

## 角色动作

`Assets/Scripts/Player/PlayerActionController.cs` 统一管理 Free、Attack、Dash、Hurt、Dead 的进入和中断规则；Grounded、Rising、Falling 是独立移动状态。移动脚本继续负责刚体，战斗脚本继续负责攻击帧和连击窗口。

- 冲刺可打断攻击，立即关闭攻击碰撞盒并清空连击缓存。
- 冲刺期间拒绝新攻击；受伤期间拒绝攻击、冲刺和跳跃，保留受击冲量。
- 死亡禁止动作，恢复进度时重置动作、物理和动画状态。
- 打开背包清理正在进行的攻击/冲刺，避免装备变化影响旧攻击。
- 游戏中的武器切换在自由动作状态执行；背包仍可选择空的 1、2 号装备栏。

Animator `Assets/Animations/Player/PlayerMovement.controller` 已整理为 Locomotion、Combat、Actions 三个子状态机。攻击播放使用缓存的完整状态路径哈希；逐帧动画保留原有素材与攻击事件。

## 集中拾取

`PlayerPickupDetector.cs` 在玩家侧统一处理目标搜索和拾取。`WorldItem.cs` 负责物品展示、活动物品登记和拾取完成；UI 读取相同的目标缓存。每帧最多进行一次常规目标扫描，成功拾取后清空目标，避免一次按键拾取多个物品。

因此 N 个地面物品的目标扫描由重复遍历全部物品，改为一次 O(N) 遍历。物品资源引用和数量直接交给 Inventory.TryAdd，成功入包后才删除地面实例。

## UI 与输入

`UIManager` 保留常驻单例、页面显隐和场景协调。`InventoryPresenter.cs` 订阅背包、生命、装备和选择变化；界面可见且数据变化时读取完整显示数据，冲刺进度独立刷新。

`UIInputRouter.cs` 使用独立 InputActionMap 集中处理 I、E、H、F/G/R、背包数字键、方向导航、Escape 和 F5。玩家 Gameplay Map 继续负责移动、跳跃、冲刺、攻击和游戏内武器切换。上下文切换会清空待处理命令，背包打开时关闭 Gameplay 输入；UGUI 原生输入模块继续处理按钮导航和提交。

菜单初始清空焦点，首次方向键才选择有效按钮，保留用户要求的交互效果。

## 时间与 AI

`Assets/Scripts/Feedback/GameTime.cs` 是 Time.timeScale 的统一写入入口。暂停请求优先于短暂命中停顿，停顿超时不会解除仍有效的暂停；换场景时清理短暂请求。

`PlayerContext.cs` 登记场景玩家与生命引用。`EnemyPerception.cs` 负责目标有效性、距离/高度/遮挡和前方地面/墙体检测，EnemyBrain 负责状态转换与攻击执行。

Boss 攻击前摇、持续时间、伤害和范围移入 `Assets/Data/Boss/DragonKnightAttacks.asset`，可在 Inspector 调整。恢复进度后，敌人与 Boss 从安全的巡逻/追击状态继续，不恢复攻击动画的中间帧。

## 新版存档

继续使用 JsonUtility，新写入版本为 2，兼容版本 1。ItemData、WeaponData 用资源 GUID 作为稳定 ID，保留旧名称别名；场景物品和固定敌人生成点配置独立稳定 ID。

新增记录：

- 地上丢弃/掉落物品的实例 ID、物品 ID、数量和位置。
- 固定敌人的 ID、位置、剩余血量与死亡结果。
- Boss 血量、是否开始遭遇、第二阶段和是否击败。

地图版本改变时仍恢复玩家物品/装备，世界位置与遭遇进度从新地图开始。旧版已拾取物品标识可迁移到新的场景 ID。

写入先生成同目录临时文件，再原子替换；上一份存档保留在原路径加 `.bak` 的文件中。主文件损坏或缺少必需数据时尝试有效备份。真实存档路径保持不变。

## 配置与验证

手动菜单“Tools/Relicfall/配置角色状态与存档标识”补充玩家/敌人组件、资源标识及动画分组，可重复执行；它不自动进入或退出游戏。

回归记录在 `Docs/ArchitectureValidation/Result.txt`，测试 JSON 和备份也位于该目录。验证使用单独测试文件，不改写真实玩家存档。临时检查脚本在验证完成后移出 Assets。

以上针对重复扫描、数据刷新和职责边界；没有将本次检查等同于 Profiler 的帧率/内存收益测量。

本次普通主菜单进入关卡后，56 项功能回归全部通过。测试包含 InputSystem 注入的键盘事件、原生 UGUI 提交、真实 Physics2D 遮挡、JSON 写入和备份恢复。后台输入设置在检查结束后恢复，临时键盘移除。真实存档最后写入时间保持 2026-10-06 22:02:40，大小 1004 字节。
