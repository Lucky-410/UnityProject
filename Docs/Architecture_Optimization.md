# 程序结构

项目按角色、战斗、敌人、物品、界面、音效和存档划分脚本目录。场景业务组件随场景创建和销毁；UIManager、AudioDirector、LoadingManager 常驻。

## 角色与战斗

PlayerMotor 处理刚体移动、坡面、跳跃缓冲和冲刺。PlayerActionController 集中控制 Free、Attack、Dash、Hurt、Dead 的互斥与中断规则，移动状态另分为 Grounded、Rising、Falling。

PlayerCombat 接收攻击输入，利用动画事件开启攻击盒、开放连击窗口和结束攻击。窗口内再次按 J 会推进到下一段，最多三段。冲刺和受伤会关闭旧攻击盒并清空连击缓存。EquipmentController 管理两个武器栏；战斗切换只在自由动作状态执行，背包可以选择空栏作为装备目标。

Animator 使用 Locomotion、Combat、Actions 三个子状态机。攻击播放使用完整状态路径的哈希，避免同名状态冲突。

## 敌人与对象池

EnemyPerception 读取玩家引用，并检测距离、高度、遮挡、前方墙体和落脚点。EnemyBrain 根据这些结果切换巡逻、追击、攻击、受伤和死亡状态。

DragonKnightBoss 管理追击、近战、喷火和第二阶段；BossAttackSettings 保存攻击参数。存档恢复后从安全的待机或追击状态继续。

SceneObjectPool<T> 负责实例创建、取出、归还和空闲缓存容量。EnemyPool 按敌人类型建池，ProjectilePool 管理投射物，VFXPool 管理限时特效。活动实例始终挂在池下，销毁池时会一起销毁。空闲缓存达到上限后销毁多余实例，战斗投射物不限制并发数量。

## 拾取与背包

PlayerPickupDetector 每帧最多搜索一次附近物品，HUD 读取同一目标。WorldItem 保存 ItemData 引用、数量和实例标识；拾取时把该引用和数量交给 Inventory.TryAdd，成功后立即停用地面物品并销毁。

Inventory 保存固定容量的物品格，负责堆叠、移除、装备、卸下和药水。InventoryPresenter 订阅数据变化，在界面可见且数据改变时刷新。切换场景时清空界面持有的物品和图标引用。

## 输入、界面与时间

PlayerInputReader 的 Gameplay 操作组负责移动、跳跃、攻击、冲刺和武器切换。UIInputRouter 的独立操作组负责背包、拾取、药水、暂停和存档快捷键；上下文改变时清空未消费操作。

UIManager 管理页面显隐和输入权限，InventoryUguiView 显示背包与 HUD，DamageNumberPresenter 维护固定数量的伤害文字。UguiFeedbackButton 使用 UGUI 原生点击与导航，并提供悬停、焦点、选中、按下和禁用表现。

GameTime 统一写入 Time.timeScale。暂停优先于命中停顿；解除命中停顿不会解除暂停。

## 存档

GameSaveController 收集玩家、背包、装备、地面物品、固定敌人和 Boss 进度，JsonSaveManager 使用 JsonUtility 读写版本 2，兼容版本 1。物品、武器和生成点使用稳定 ID，旧资源名称作为兼容别名。

写入先生成同目录临时文件，再原子替换，上一份保存为 .bak。地图版本不匹配时恢复物品和装备，世界位置从当前地图出生点开始。

文件位于 Application.persistentDataPath/relicfall-save.json。Unity 菜单“Tools/Relicfall/配置角色状态与存档标识”用于补齐组件、资源标识及动画分组。
