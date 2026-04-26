# Game1 API设计文档

**项目**: Game1 - Unity 6 挂机放置类游戏
**创建时间**: 2026-04-26

---

## 一、Core API

### 1.1 GameMain

```csharp
public sealed class GameMain : MonoBehaviour
{
    // 单例访问
    public static GameMain instance { get; }

    // 初始化
    public void Initialize();

    // 系统访问
    public T GetSystem<T>() where T : class;
    public PlayerActor player { get; }
    public GameLoopManager gameLoop { get; }
}
```

### 1.2 GameLoopManager

```csharp
public class GameLoopManager : MonoBehaviour
{
    // 单例
    public static GameLoopManager instance { get; }

    // 初始化
    public void Initialize();

    // 注册系统
    public void RegisterSystem(IModule system);
    public void UnregisterSystem(IModule system);

    // Tick控制
    public void StartLoop();
    public void StopLoop();
    public void PauseLoop();
    public void ResumeLoop();

    // 时间缩放
    public float timeScale { get; set; }
}
```

### 1.3 SaveManager

```csharp
public class SaveManager
{
    // 单例
    public static SaveManager instance { get; }

    // 基础操作
    public bool CreateSave(string saveName, GameSaveData data);
    public GameSaveData LoadSave(string saveName);
    public bool DeleteSave(string saveName);
    public bool SaveExists(string saveName);
    public List<SaveSlotInfo> GetAllSaves();

    // 自动存档
    public void AutoSave();
    public void SetAutoSaveInterval(float intervalSeconds);

    // 云存档
    public Task<bool> CloudSyncAsync();
}
```

### 1.4 EventBus

```csharp
public static class EventBus<T> where T : struct
{
    public static void Subscribe(Action<T> callback);
    public static void Unsubscribe(Action<T> callback);
    public static void Publish(T eventData);
    public static void Clear();
}
```

---

## 二、Modules API

### 2.1 IModule接口

```csharp
public interface IModule
{
    string moduleName { get; }
    bool isActive { get; }

    void Initialize(PlayerActor player);
    void Tick(float deltaTime);
    void Activate();
    void Deactivate();
}
```

### 2.2 CombatModule

```csharp
public interface ICombatModule : IModule
{
    // 战斗执行
    void ExecuteCombat(CombatContext context);
    void ExecuteMultiEnemyCombat(List<CombatantData> enemies);

    // 统计
    CombatStatistics GetStatistics();
    void ResetStatistics();

    // 加成
    void ApplyPlayerBonuses(PlayerActor player);
}

public class CombatContext
{
    public CombatantData attacker;
    public CombatantData defender;
    public bool isCritical;
    public float finalDamage;
    public List<CombatEvent> events;
}

public class CombatStatistics
{
    public int totalDamage;
    public int totalCrits;
    public int totalAttacks;
    public float averageDamage;
}
```

### 2.3 InventoryModule

```csharp
public interface IInventoryModule : IModule
{
    // 物品操作
    InventoryOperationResult AddItem(InventoryItemData item);
    InventoryOperationResult RemoveItem(string templateId, int quantity);
    InventoryItemData GetItem(string templateId);
    bool HasItem(string templateId, int quantity = 1);

    // 查询
    int GetItemCount(string templateId);
    int GetUsedSlots();
    IReadOnlyList<InventoryItemData> GetAllItems();

    // 容量
    int maxSlots { get; }
    bool IsFull { get; }
}
```

### 2.4 TeamModule

```csharp
public interface ITeamModule : IModule
{
    // 成员管理
    TeamOperationResult AddMember(TeamMemberData member);
    TeamOperationResult RemoveMember(string actorId);
    TeamMemberData GetMember(string actorId);
    IReadOnlyList<TeamMemberData> GetAllMembers();

    // 容量
    int maxMembers { get; }
    int currentMembers { get; }
    bool IsFull { get; }

    // 职业加成
    float GetJobBonus(JobType job);

    // 队伍属性
    int GetTotalAttack();
    int GetTotalDefense();
}
```

### 2.5 TravelModule

```csharp
public interface ITravelModule : IModule
{
    // 旅行状态
    TravelState currentState { get; }
    float currentProgress { get; }
    float travelRate { get; } // TP/秒

    // 旅行控制
    void StartTravel();
    void EndTravel();
    void ProgressNode();

    // 离线收益
    void CalculateOfflineProgress(TimeSpan offlineTime);
}
```

### 2.6 IdleModule

```csharp
public interface IIdleModule : IModule
{
    // 收益
    float idleMultiplier { get; set; }
    float bonusMultiplier { get; set; }

    // 计算
    float CalculateOfflineEarnings(TimeSpan offlineTime);
    void ClaimIdleReward();

    // 状态
    DateTime lastClaimTime { get; }
    bool CanClaim { get; }
}
```

### 2.7 SkillModule

```csharp
public interface ISkillModule : IModule
{
    // 技能管理
    void LearnSkill(SkillData skill);
    void ForgetSkill(string skillId);
    SkillData GetSkill(string skillId);
    IReadOnlyList<SkillData> GetAllSkills();

    // 技能类型
    IReadOnlyList<SkillData> GetPassiveSkills();
    IReadOnlyList<SkillData> GetActiveSkills();
    IReadOnlyList<SkillData> GetUltimateSkills();

    // 技能触发
    void TriggerPassive(string triggerEvent);
    bool TriggerActive(string skillId);
    void TriggerUltimate();
}
```

### 2.8 CardModule

```csharp
public interface ICardModule : IModule
{
    // 卡牌获取
    void AcquireCard(CardData card);
    void RemoveCard(string cardId);

    // 查询
    CardData GetCard(string cardId);
    IReadOnlyList<CardData> GetAllCards();
    IReadOnlyList<CardData> GetCardsByRarity(CardRarity rarity);

    // 抽卡
    GachaResult Draw(GachaType type, int count);

    // 卡组
    void SetActiveDeck(List<string> cardIds);
    List<CardData> GetActiveDeck();
}
```

### 2.9 PrestigeModule

```csharp
public interface IPrestigeModule : IModule
{
    // 轮回点数
    int prestigePoints { get; }
    int totalPrestigePoints { get; }

    // 轮回操作
    void PerformPrestige();
    bool CanPrestige { get; }

    // 升级
    void PurchaseUpgrade(string upgradeId);
    IReadOnlyList<PrestigeUpgrade> GetAvailableUpgrades();
    IReadOnlyList<PrestigeUpgrade> GetPurchasedUpgrades();
}
```

---

## 三、Events API

### 3.1 EventManager

```csharp
public class EventManager
{
    public static EventManager instance { get; }

    // 模板加载
    void LoadEventTemplates(string xmlPath);
    EventTemplate GetTemplate(string eventId);

    // 事件链
    EventChain CreateChain(string eventId);
    void ExecuteChain(EventChain chain, Action<EventResult> onComplete);
    void CancelChain(EventChain chain);
}
```

### 3.2 EventTreeRunner

```csharp
public class EventTreeRunner
{
    // 状态
    EventTreeState state { get; }
    EventTreeTemplate currentTemplate { get; }

    // 控制
    void StartTree(EventTreeTemplate template);
    void SelectChoice(int choiceIndex);
    void GoBack();
    void Restart();
    void EndTree();

    // 事件
    event Action<EventTreeState> OnStateChanged;
    event Action<string> OnNodeEnter;
    event Action<EventResult> OnTreeComplete;
}
```

---

## 四、UI API

### 4.1 UIManager

```csharp
public class UIManager
{
    public static UIManager instance { get; }

    // 面板控制
    void ShowPanel<T>() where T : BaseUIPanel;
    void HidePanel<T>() where T : BaseUIPanel;
    void TogglePanel<T>() where T : BaseUIPanel;

    // 状态查询
    bool IsPanelVisible<T>() where T : BaseUIPanel;
    UIState currentState { get; }

    // HUD
    void UpdateHUD(HUDData data);
}
```

### 4.2 IUIPanel

```csharp
public interface IUIPanel
{
    string panelName { get; }
    bool isVisible { get; }

    void Show();
    void Hide();
    void Refresh();
}
```

---

## 五、数据结构

### 5.1 GameSaveData

```csharp
[Serializable]
public class GameSaveData
{
    public string playerName;
    public int gold;
    public int level;
    public TimeSpan playTime;
    public DateTime timestamp;

    // 模块数据
    public PlayerData player;
    public TravelData travel;
    public InventoryData inventory;
    public TeamData team;
    public SkillData skills;
    public CardData cards;
    public PrestigeData prestige;
}
```

### 5.2 InventoryItemData

```csharp
[Serializable]
public class InventoryItemData
{
    public string instanceId;
    public string templateId;
    public int quantity;
    public int slotIndex;
    public Dictionary<string, int> customData;
}
```

### 5.3 TeamMemberData

```csharp
[Serializable]
public class TeamMemberData
{
    public string actorId;
    public string name;
    public JobType job;
    public int level;

    public int currentHp;
    public int maxHp;
    public int attack;
    public int defense;

    public List<string> equipmentIds;
    public List<string> skillIds;
}
```

---

## 六、枚举定义

### 6.1 JobType

```csharp
public enum JobType
{
    Warrior,     // 战士 - 高攻击
    Merchant,   // 商贾 - 高金币
    Scholar,    // 学者 - 高经验
    Medic        // 医者 - 高治疗
}
```

### 6.2 CardRarity

```csharp
public enum CardRarity
{
    N,    // 普通
    R,    // 稀有
    SR,   // 超稀有
    SSR,  // 超超稀有
    UR,   // 终极
    GR    // 传说
}
```

### 6.3 TravelState

```csharp
public enum TravelState
{
    Idle,       // 空闲
    Traveling,  // 旅行中
    Paused,    // 暂停
    Event       // 事件中
}
```

---

## 七、文档版本

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0 | 2026-04-26 | 初始版本 |

