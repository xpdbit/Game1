# 存档系统规范 (SaveSystem)

## 概述

存档系统负责游戏数据的持久化，包括玩家数据、旅行进度、背包物品、模块状态等。系统在适当时机自动保存，并在游戏启动时自动加载。

## 存档数据结构

### 根对象 SaveData

```csharp
[Serializable]
public class SaveData
{
    public int version;              // 存档版本号，用于迁移
    public long timestamp;           // 存档时间戳
    public PlayerSaveData player;     // 玩家数据
    public TravelSaveData travel;    // 旅行数据
    public InventorySaveData inventory; // 背包数据
    public ModuleCollectionSaveData modules; // 模块数据
    public EventSaveData events;      // 事件系统数据
}
```

### PlayerSaveData（玩家数据）

```csharp
[Serializable]
public class PlayerSaveData
{
    public string id;                 // 玩家ID
    public string actorName;         // 角色名称
    public int level;               // 等级

    // 属性
    public int maxHp;
    public int currentHp;
    public int attack;
    public int defense;
    public float speed;

    // 货币
    public int gold;

    // 位置
    public string currentLocationId;
    public string nextLocationId;
}
```

### TravelSaveData（旅行数据）

```csharp
[Serializable]
public class TravelSaveData
{
    public string mapSeed;           // 地图种子
    public int currentNodeIndex;     // 当前节点索引
    public int maxNodeIndex;         // 最大到达节点
    public float travelProgress;     // 当前旅行进度(0~1)
    public float travelTimeRequired; // 所需时间
    public int totalTP;              // 总旅行点
    public int milestoneCount;       // 里程碑计数
}
```

### InventorySaveData（背包数据）

```csharp
[Serializable]
public class InventorySaveData
{
    public int maxSlotCount;        // 最大槽位数
    public float maxWeight;          // 最大重量
    public List<ItemInstanceSaveData> items; // 物品列表
}

[Serializable]
public class ItemInstanceSaveData
{
    public string templateId;        // 模板ID
    public int instanceId;          // 实例ID
    public int amount;              // 数量
}
```

### ModuleCollectionSaveData（模块数据）

```csharp
[Serializable]
public class ModuleCollectionSaveData
{
    public List<ModuleSaveData> modules;
}

[Serializable]
public class ModuleSaveData
{
    public string moduleId;         // 模块ID
    public string moduleType;       // 模块类型名
    // 各种模块特有字段（需要序列化）
}
```

### EventSaveData（事件系统数据）

```csharp
[Serializable]
public class EventSaveData
{
    // 事件队列状态
    public List<string> pendingEventIds; // 待处理事件ID列表

    // 事件树状态
    public string currentTreeId;        // 当前事件树ID
    public string currentNodeId;        // 当前节点ID
    public List<string> treeHistory;    // 节点历史栈
    public EventTreeState treeState;     // 事件树状态

    // 事件链状态
    public string currentChainId;       // 当前事件链ID
    public string chainCurrentNodeId;   // 当前节点ID
    public Dictionary<string, string> chainFlags; // 事件链标志
}
```

## 存档时机

### 自动保存时机

| 时机 | 触发条件 | 说明 |
|------|----------|------|
| 旅行完成 | 到达新节点 | 保存玩家状态、旅行进度 |
| 事件完成 | 事件结果应用后 | 保存背包、模块变化 |
| 里程碑触发 | ProgressManager milestones | 保存TP和里程碑 |
| 定时保存 | 每5分钟 | 增量保存 |

### 手动保存

- 玩家可以通过UI触发手动保存
- 退出游戏前自动保存

## 存档位置

```
%APPDATA%/Game1/saves/
├── save_001.json    # 自动存档
├── save_002.json    # 手动存档1
├── save_003.json    # 手动存档2
└── autosave.json    # 当前进度
```

## 存档版本管理

```csharp
// 版本迁移示例
if (saveData.version < 2)
{
    // 从v1迁移到v2
    saveData.player.maxHp = saveData.player.maxHp ?? 20; // 新增字段
    saveData.version = 2;
}

if (saveData.version < 3)
{
    // 从v2迁移到v3
    // 重新计算属性
    saveData.version = 3;
}
```

## 初始化顺序

正确的初始化顺序：

```
1. ResourceManager.Initialize()
   └─► 加载配置（Items.xml, Events.xml等）

2. ItemManager.Initialize()
   └─► 加载物品模板

3. GameMain.instance.Initialize()
   │
   ├─► PlayerActor.Initialize()
   │
   ├─► TravelManager.Initialize(player)
   │
   ├─► EventQueue.Initialize()
   │
   └─► LoadSaveData() → 应用存档
```

### 依赖关系图

```
ResourceManager (独立)
        │
        ▼
ItemManager ──依赖──► ResourceManager
        │
        ▼
GameMain ──依赖──► ItemManager, TravelManager, EventQueue
        │
        ▼
SaveManager ──加载──► SaveData
        │
        ├──► PlayerActor (恢复玩家数据)
        ├──► InventoryDesign (恢复背包)
        ├──► TravelManager (恢复旅行状态)
        └──► EventTreeRunner (恢复事件树状态)
```

## 错误处理与容错

### 存档加载失败

```csharp
public void LoadSaveData()
{
    try
    {
        var saveData = LoadFromFile(savePath);
        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] No save data found, creating new game");
            CreateNewGame();
            return;
        }

        // 版本迁移
        if (saveData.version < CURRENT_VERSION)
        {
            MigrateSaveData(saveData);
        }

        ApplySaveData(saveData);
    }
    catch (Exception e)
    {
        Debug.LogError($"[SaveManager] Failed to load save: {e.Message}");
        // 尝试加载备份
        LoadBackup();
    }
}
```

### 存档保存失败

```csharp
public void SaveSaveData()
{
    try
    {
        var saveData = CreateSaveData();
        SaveToFile(saveData, savePath);

        // 创建备份
        CreateBackup(savePath);
    }
    catch (Exception e)
    {
        Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
        // 保存到备用位置
        SaveToFile(saveData, backupPath);
    }
}
```

## XML配置格式

存档使用JSON格式，但通过统一的序列化接口：

```json
{
    "version": 1,
    "timestamp": 1713849600000,
    "player": {
        "id": "player_001",
        "actorName": "行者",
        "level": 1,
        "maxHp": 20,
        "currentHp": 20,
        "attack": 3,
        "defense": 5,
        "speed": 1.0,
        "gold": 100,
        "currentLocationId": "loc_0_start",
        "nextLocationId": "loc_1_city"
    },
    "travel": {
        "mapSeed": "seed_12345",
        "currentNodeIndex": 0,
        "maxNodeIndex": 0,
        "travelProgress": 0.0,
        "travelTimeRequired": 15.0,
        "totalTP": 100,
        "milestoneCount": 0
    },
    "inventory": {
        "maxSlotCount": 50,
        "maxWeight": 100.0,
        "items": [
            {
                "templateId": "Core.Item.GoldCoin",
                "instanceId": 1,
                "amount": 100
            }
        ]
    },
    "modules": {
        "modules": [
            {
                "moduleId": "idle_reward",
                "moduleType": "IdleRewardModule",
                "baseRewardPerSecond": 1.0,
                "offlineRewardRate": 0.5
            }
        ]
    },
    "events": {
        "pendingEventIds": [],
        "currentTreeId": null,
        "currentNodeId": null,
        "treeHistory": [],
        "treeState": 0,
        "currentChainId": null,
        "chainCurrentNodeId": null,
        "chainFlags": {}
    }
}
```

## 调试机制

### 调试命令

| 命令 | 功能 |
|------|------|
| `save` | 手动保存 |
| `load` | 加载存档 |
| `reset` | 重置存档 |
| `export` | 导出存档为JSON |
| `import` | 从JSON导入 |

### 日志输出

```csharp
Debug.Log($"[SaveManager] Saving game at {timestamp}");
Debug.Log($"[SaveManager] Save completed: {filePath}");
Debug.Log($"[SaveManager] Loading save: {filePath}");
Debug.Log($"[SaveManager] Load completed: {saveData.player.actorName}");
Debug.LogWarning($"[SaveManager] Save version mismatch: {saveData.version} vs {CURRENT_VERSION}");
Debug.LogError($"[SaveManager] Save corrupted: {error}");
```

## 待实现功能

1. **存档加密** - 防止玩家修改存档
2. **云存档** - 同步到云端
3. **多存档槽位** - 支持多个存档
4. **存档预览** - 显示存档创建时间、游戏进度等信息