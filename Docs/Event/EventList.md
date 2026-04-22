# 事件列表 (EventList)

## 概述

本文件记录游戏中所有可能触发的事件配置。按类型分类，每项包含ID、名称、描述和效果。

## 事件类型

| 类型 | 说明 | 触发场景 |
|------|------|----------|
| `Random` | 随机事件 | 旅行中随机触发 |
| `Choice` | 选择事件 | 玩家决策点 |
| `Combat` | 战斗事件 | 遭遇敌人 |
| `Trade` | 交易事件 | 遇到商人 |
| `Discovery` | 探索事件 | 发现隐藏内容 |
| `Story` | 剧情事件 | 主线/支线剧情 |

---

## 随机事件 (Random)

### Core.Event.WildAnimal

| 属性 | 值 |
|------|-----|
| ID | Core.Event.WildAnimal |
| 名称 | 野兽袭击 |
| 类型 | Random |
| 描述 | 旅途中有机会遭遇野生动物 |
| 效果 | 30%几率触发战斗，70%几率获得皮革×2 |

*野生动物可能是威胁，也可能是资源。*

### Core.Event.LostTraveler

| 属性 | 值 |
|------|-----|
| ID | Core.Event.LostTraveler |
| 名称 | 迷路的旅人 |
| 类型 | Random |
| 描述 | 遇到迷失方向的旅行者 |
| 效果 | 给予食物恢复好感度，获得金币×10作为感谢 |

*帮助他人可能会带来意想不到的回报。*

### Core.Event.WeatherChange

| 属性 | 值 |
|------|-----|
| ID | Core.Event.WeatherChange |
| 名称 | 天气突变 |
| 类型 | Random |
| 描述 | 原本晴朗的天空突然转阴 |
| 效果 | 接下来3次行动食物消耗+100%，或支付50金币购买雨具避免 |

*变幻莫测的天气是旅行者的常敌。*

---

## 选择事件 (Choice)

### Core.Event.Shortcut

| 属性 | 值 |
|------|-----|
| ID | Core.Event.Shortcut |
| 名称 | 岔路抉择 |
| 类型 | Choice |
| 描述 | 面前出现了两条道路 |
| 效果 | 选择捷径：节省1天时间，但70%几率遭遇野兽；选择大道：安全但多花费100金币 |

*每条路都有它的代价。*

### Core.Event.AbandonedCamp

| 属性 | 值 |
|------|-----|
| ID | Core.Event.AbandonedCamp |
| 名称 | 废弃营地 |
| 类型 | Choice |
| 描述 | 发现一个被遗弃的营地，物资散落一地 |
| 效果 | 搜索：30%几率获得随机装备，70%几率触发陷阱损失10HP；直接离开：无事发生 |

*未知的诱惑往往伴随着风险。*

### Core.Event.MysteriousStranger

| 属性 | 值 |
|------|-----|
| ID | Core.Event.MysteriousStranger |
| 名称 | 神秘陌生人 |
| 类型 | Choice |
| 描述 | 一个戴着兜帽的人向你招手 |
| 效果 | 交谈：可能获得任务线索或被骗走财物；无视离开：安全但错过机会 |

*命运总是垂青勇敢者，或者愚者。*

---

## 战斗事件 (Combat)

### Core.Event.BanditAmbush

| 属性 | 值 |
|------|-----|
| ID | Core.Event.BanditAmbush |
| 名称 | 匪徒伏击 |
| 类型 | Combat |
| 描述 | 匪徒从树林中窜出，拦住了去路 |
| 效果 | 战胜：获得金币×50、经验×30；战败：损失当前金币的20%，传送回最近检查点 |

*拦路抢劫是荒野常见的生存法则。*

### Core.Event.GuardPatrol

| 属性 | 值 |
|------|-----|
| ID | Core.Event.GuardPatrol |
| 名称 | 守卫巡逻 |
| 类型 | Combat |
| 描述 | 一队守卫发现了你的踪迹 |
| 效果 | 战胜：获得声望×20、钥匙碎片×1；战败：入狱24小时或支付罚金200金币 |

*正义与邪恶往往只在一线之隔。*

### Core.Event.MonsterLair

| 属性 | 值 |
|------|-----|
| ID | Core.Event.MonsterLair |
| 名称 | 怪物巢穴 |
| 类型 | Combat |
| 描述 | 误入了怪物的领地，必须战斗才能脱身 |
| 效果 | 战胜：获得怪物掉落物×3、经验×50；战败：损失15%最大生命值 |

*有时候退路并不存在。*

---

## 交易事件 (Trade)

### Core.Event.WanderingMerchant

| 属性 | 值 |
|------|-----|
| ID | Core.Event.WanderingMerchant |
| 名称 | 流浪商人 |
| 类型 | Trade |
| 描述 | 一个背着大包的商人向你招手 |
| 效果 | 可用金币购买随机物品，或出售不需要的道具；商人好感度影响价格折扣 |

*每个商人都有一段故事。*

### Core.Event.BlackMarket

| 属性 | 值 |
|------|-----|
| ID | Core.Event.BlackMarket |
| 名称 | 黑市交易 |
| 类型 | Trade |
| 描述 | 通过暗号进入隐秘的黑市 |
| 效果 | 可购买稀有物品和任务道具，但价格高昂且有受骗风险 |

*黑暗中有你想要的一切，只要你付得起代价。*

---

## 探索事件 (Discovery)

### Core.Event.HiddenCache

| 属性 | 值 |
|------|-----|
| ID | Core.Event.HiddenCache |
| 名称 | 隐藏宝箱 |
| 类型 | Discovery |
| 描述 | 观察环境时发现了一处隐蔽的藏宝点 |
| 效果 | 开启宝箱：获得随机装备×1、金币×30；有陷阱风险，40%几率损失10HP |

*细心观察总能发现惊喜。*

### Core.Event.AncientRuins

| 属性 | 值 |
|------|-----|
| ID | Core.Event.AncientRuins |
| 名称 | 古代遗迹 |
| 类型 | Discovery |
| 描述 | 穿过密林后发现了一片古老的建筑遗迹 |
| 效果 | 探索遗迹：获得古代遗物碎片×1、经验×20；可触发后续剧情事件 |

*过去的辉煌总是埋在尘土之下。*

---

## 剧情事件 (Story)

### Core.Event.PrologueEnd

| 属性 | 值 |
|------|-----|
| ID | Core.Event.PrologueEnd |
| 名称 | 序章完结 |
| 类型 | Story |
| 描述 | 完成新手教程，迎接真正的冒险 |
| 效果 | 解锁完整地图、获得初始装备礼包、触发第一个主线任务 |

*故事从这里正式开始。*

### Core.Event.LegendaryHero

| 属性 | 值 |
|------|-----|
| ID | Core.Event.LegendaryHero |
| 名称 | 传说中的英雄 |
| 类型 | Story |
| 描述 | 遇见传说中的英雄，聆听他的教诲 |
| 效果 | 获得英雄馈赠（随机传说装备×1）、解锁英雄支线任务线 |

*每个人都有自己的传奇。*

---

## 统计

| 类型 | 数量 |
|------|------|
| Random | 3 |
| Choice | 3 |
| Combat | 3 |
| Trade | 2 |
| Discovery | 2 |
| Story | 2 |
| **总计** | **15** |