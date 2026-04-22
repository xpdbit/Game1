# 物品列表 (ItemList)

## 概述

本文件记录游戏中所有可收集的物品配置。按类型分类，每项包含ID、名称、描述和属性。

## 物品类型

| 类型 | 说明 | 关键属性 |
|------|------|----------|
| `Money` | 货币 | - |
| `Food` | 食物 | foodCalorific (热量kJ) |
| `Weapon` | 武器 | damage (伤害) |
| `Armor` | 护甲 | armor (防御) |
| `Consumable` | 消耗品 | effect |
| `Material` | 材料 | - |
| `QuestItem` | 任务物品 | - |

---

## 货币 (Money)

### Core.Item.GoldCoin

| 属性 | 值 |
|------|-----|
| ID | Core.Item.GoldCoin |
| 名称 | 金币 |
| 类型 | Money |
| 重量 | 0.0 kg |
| 堆叠上限 | 99 |

*游戏中的基础货币单位。*

---

## 食物 (Food)

### Core.Item.Bacon

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Bacon |
| 名称 | 培根 |
| 类型 | Food |
| 重量 | 0.5 kg |
| 热量 | 1200 kJ |
| 堆叠上限 | 20 |

*高热量食物，迅速恢复体力。*

### Core.Item.Cabbage

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Cabbage |
| 名称 | 卷心菜 |
| 类型 | Food |
| 重量 | 1.0 kg |
| 热量 | 400 kJ |
| 堆叠上限 | 30 |

*普通蔬菜，轻微恢复体力。*

### Core.Item.DriedMeat

| 属性 | 值 |
|------|-----|
| ID | Core.Item.DriedMeat |
| 名称 | 肉干 |
| 类型 | Food |
| 重量 | 0.3 kg |
| 热量 | 800 kJ |
| 堆叠上限 | 20 |

*便携高热量食物，适合旅行。*

### Core.Item.Fruit

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Fruit |
| 名称 | 水果 |
| 类型 | Food |
| 重量 | 0.2 kg |
| 热量 | 200 kJ |
| 堆叠上限 | 50 |

*新鲜水果，轻度恢复。*

---

## 武器 (Weapon)

### Core.Item.ShortBlade

| 属性 | 值 |
|------|-----|
| ID | Core.Item.ShortBlade |
| 名称 | 短刀 |
| 类型 | Weapon |
| 重量 | 2.0 kg |
| 伤害 | 5 |
| 堆叠上限 | 1 |

*基础近战武器，轻便易用。*

### Core.Item.LongSword

| 属性 | 值 |
|------|-----|
| ID | Core.Item.LongSword |
| 名称 | 长剑 |
| 类型 | Weapon |
| 重量 | 4.0 kg |
| 伤害 | 12 |
| 堆叠上限 | 1 |

*标准近战武器，伤害更高。*

### Core.Item.Bow

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Bow |
| 名称 | 弓 |
| 类型 | Weapon |
| 重量 | 1.5 kg |
| 伤害 | 8 |
| 堆叠上限 | 1 |

*远程武器，攻击距离远。*

### Core.Item.Staff

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Staff |
| 名称 | 法杖 |
| 类型 | Weapon |
| 重量 | 2.5 kg |
| 伤害 | 6 |
| 堆叠上限 | 1 |

*魔法武器，可使用技能。*

---

## 护甲 (Armor)

### Core.Item.LeatherArmor

| 属性 | 值 |
|------|-----|
| ID | Core.Item.LeatherArmor |
| 名称 | 皮甲 |
| 类型 | Armor |
| 重量 | 5.0 kg |
| 防御 | 3 |
| 堆叠上限 | 1 |

*基础护甲，轻便灵活。*

### Core.Item.ChainMail

| 属性 | 值 |
|------|-----|
| ID | Core.Item.ChainMail |
| 名称 | 锁子甲 |
| 类型 | Armor |
| 重量 | 10.0 kg |
| 防御 | 8 |
| 堆叠上限 | 1 |

*中级护甲，提供较好防护。*

---

## 材料 (Material)

### Core.Item.Wood

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Wood |
| 名称 | 木材 |
| 类型 | Material |
| 重量 | 3.0 kg |
| 堆叠上限 | 50 |

*基础建筑材料。*

### Core.Item.IronOre

| 属性 | 值 |
|------|-----|
| ID | Core.Item.IronOre |
| 名称 | 铁矿石 |
| 类型 | Material |
| 重量 | 5.0 kg |
| 堆叠上限 | 30 |

*用于冶炼金属的原料。*

### Core.Item.IronIngot

| 属性 | 值 |
|------|-----|
| ID | Core.Item.IronIngot |
| 名称 | 铁锭 |
| 类型 | Material |
| 重量 | 4.0 kg |
| 堆叠上限 | 30 |

*精炼后的金属，可制作装备。*

### Core.Item.Coal

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Coal |
| 名称 | 煤炭 |
| 类型 | Material |
| 重量 | 2.0 kg |
| 堆叠上限 | 50 |

*燃料，用于熔炉和锻造。*

### Core.Item.Firewood

| 属性 | 值 |
|------|-----|
| ID | Core.Item.Firewood |
| 名称 | 木柴 |
| 类型 | Material |
| 重量 | 1.0 kg |
| 堆叠上限 | 50 |

*基础燃料，用于生火。*

---

## 消耗品 (Consumable)

### Core.Item.HealthPotion

| 属性 | 值 |
|------|-----|
| ID | Core.Item.HealthPotion |
| 名称 | 生命药剂 |
| 类型 | Consumable |
| 重量 | 0.5 kg |
| 效果 | 恢复20HP |
| 堆叠上限 | 10 |

*立即恢复生命值。*

### Core.Item.SpeedPotion

| 属性 | 值 |
|------|-----|
| ID | Core.Item.SpeedPotion |
| 名称 | 速度药剂 |
| 类型 | Consumable |
| 重量 | 0.3 kg |
| 效果 | 移动速度+50% |
| 持续时间 | 30秒 |
| 堆叠上限 | 10 |

*短时间内提升移动速度。*

---

## 任务物品 (QuestItem)

### Core.ItemancientArtifact

| 属性 | 值 |
|------|-----|
| ID | Core.Item.AncientArtifact |
| 名称 | 古代遗物 |
| 类型 | QuestItem |
| 重量 | 1.0 kg |
| 堆叠上限 | 1 |

*神秘的古代物品，任务关键道具。*

### Core.Item.MapFragment

| 属性 | 值 |
|------|-----|
| ID | Core.Item.MapFragment |
| 名称 | 地图碎片 |
| 类型 | QuestItem |
| 重量 | 0.1 kg |
| 堆叠上限 | 5 |

*地图的碎片，收集后可显示隐藏区域。*

---

## 统计

| 类型 | 数量 |
|------|------|
| Money | 1 |
| Food | 4 |
| Weapon | 4 |
| Armor | 2 |
| Material | 5 |
| Consumable | 2 |
| QuestItem | 2 |
| **总计** | **20** |