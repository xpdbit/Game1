#nullable enable

using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Game1.Modules.Achievement
{
    public class TaskDesign
    {
        #region Singleton
        private static TaskDesign _instance;
        public static TaskDesign instance => _instance ??= new TaskDesign();
        #endregion

        // 内部数据
        private List<TaskTemplate> _dailyTemplates = new();
        private List<TaskTemplate> _weeklyTemplates = new();
        private List<TaskInstance> _dailyTasks = new();
        private List<TaskInstance> _weeklyTasks = new();
        private Dictionary<AchievementConditionType, List<TaskInstance>> _conditionIndex = new();

        // 刷新时间（北京时间 UTC+8）
        private const int BEIJING_TIMEZONE_OFFSET = 8;
        private static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(BEIJING_TIMEZONE_OFFSET);

        // 常量
        private const int DAILY_TASK_COUNT = 3;
        private const int WEEKLY_TASK_COUNT = 2;

        // 事件
        public event Action<TaskInstance>? onTaskCompleted;
        public event Action<TaskInstance>? onRewardClaimed;
        public event Action? onTasksRefreshed;

        #region 初始化
        public void Initialize()
        {
            _dailyTemplates = new List<TaskTemplate>();
            _weeklyTemplates = new List<TaskTemplate>();
            _dailyTasks = new List<TaskInstance>();
            _weeklyTasks = new List<TaskInstance>();
            _conditionIndex = new Dictionary<AchievementConditionType, List<TaskInstance>>();

            LoadTemplates();
            CheckRefresh();
            BuildConditionIndex();
        }

        private void LoadTemplates()
        {
            // 从Resources加载TaskTemplates.xml
            var asset = Resources.Load<TextAsset>("Data/Tasks/TaskTemplates");
            if (asset == null)
            {
                Debug.LogWarning("[TaskDesign] TaskTemplates asset not found at Resources/Data/Tasks/TaskTemplates");
                return;
            }

            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(asset.text);
            var root = doc.SelectSingleNode("TaskTemplates");
            if (root == null) return;

            var nodes = root.SelectNodes("Task");
            if (nodes == null) return;

            foreach (System.Xml.XmlNode node in nodes)
            {
                var template = ParseTaskTemplate(node);
                if (template != null && !string.IsNullOrEmpty(template.id))
                {
                    if (template.category == TaskCategory.Daily)
                        _dailyTemplates.Add(template);
                    else
                        _weeklyTemplates.Add(template);
                }
            }

            Debug.Log($"[TaskDesign] Loaded {_dailyTemplates.Count} daily tasks, {_weeklyTemplates.Count} weekly tasks");
        }

        private TaskTemplate? ParseTaskTemplate(System.Xml.XmlNode node)
        {
            var categoryStr = node.Attributes["category"]?.Value ?? "Daily";
            var category = categoryStr == "Weekly" ? TaskCategory.Weekly : TaskCategory.Daily;

            var conditionTypeStr = node.Attributes["conditionType"]?.Value ?? "GoldEarned";
            var conditionType = ParseConditionType(conditionTypeStr);

            var rewardNode = node.SelectSingleNode("Reward");
            var reward = new AchievementRewardData
            {
                type = ParseRewardType(rewardNode?.Attributes["type"]?.Value ?? "Gold"),
                configId = rewardNode?.Attributes["configId"]?.Value ?? "",
                amount = int.TryParse(rewardNode?.Attributes["amount"]?.Value, out var amt) ? amt : 0
            };

            return new TaskTemplate
            {
                id = node.Attributes["id"]?.Value ?? "",
                nameTextId = node.Attributes["nameTextId"]?.Value ?? "",
                descriptionTextId = node.Attributes["descriptionTextId"]?.Value ?? "",
                category = category,
                conditionType = conditionType,
                targetValue = float.TryParse(node.Attributes["targetValue"]?.Value, out var tv) ? tv : 0,
                reward = reward,
                isActive = bool.TryParse(node.Attributes["isActive"]?.Value, out var active) ? active : true
            };
        }

        private static AchievementConditionType ParseConditionType(string value)
        {
            return value switch
            {
                "GoldEarned" => AchievementConditionType.GoldEarned,
                "EnemiesDefeated" => AchievementConditionType.EnemiesDefeated,
                "DistanceTraveled" => AchievementConditionType.DistanceTraveled,
                "ItemsCollected" => AchievementConditionType.ItemsCollected,
                "TeamMembers" => AchievementConditionType.TeamMembers,
                "LevelsGained" => AchievementConditionType.LevelsGained,
                "EventsCompleted" => AchievementConditionType.EventsCompleted,
                "PrestigesPerformed" => AchievementConditionType.PrestigesPerformed,
                "CombatWon" => AchievementConditionType.CombatWon,
                "BossesDefeated" => AchievementConditionType.BossesDefeated,
                "LocationsDiscovered" => AchievementConditionType.LocationsDiscovered,
                "PetsMaxHappiness" => AchievementConditionType.PetsMaxHappiness,
                _ => AchievementConditionType.GoldEarned
            };
        }

        private static RewardType ParseRewardType(string value)
        {
            return value switch
            {
                "Item" => RewardType.Item,
                "Experience" => RewardType.Experience,
                "Title" => RewardType.Title,
                _ => RewardType.Gold
            };
        }
        #endregion

        #region 刷新逻辑
        /// <summary>
        /// 检查是否需要刷新任务（每天/每周）
        /// </summary>
        public void CheckRefresh()
        {
            var now = GetBeijingTime();
            var todayStart = GetTodayBeijingStart();
            var thisWeekStart = GetThisWeekBeijingStart();

            // 检查每日刷新
            var lastDailyRefresh = GetLastDailyRefreshTime();
            if (lastDailyRefresh < todayStart)
            {
                RefreshDailyTasks();
            }

            // 检查每周刷新
            var lastWeeklyRefresh = GetLastWeeklyRefreshTime();
            if (lastWeeklyRefresh < thisWeekStart)
            {
                RefreshWeeklyTasks();
            }
        }

        private long GetLastDailyRefreshTime()
        {
            // 从存档数据获取，或使用0（表示从未刷新）
            return _dailyTasks.Count > 0 ? _dailyTasks[0].refreshTimestamp : 0;
        }

        private long GetLastWeeklyRefreshTime()
        {
            return _weeklyTasks.Count > 0 ? _weeklyTasks[0].refreshTimestamp : 0;
        }

        /// <summary>
        /// 获取北京时间
        /// </summary>
        private DateTimeOffset GetBeijingTime()
        {
            return DateTimeOffset.UtcNow.ToOffset(BeijingOffset);
        }

        /// <summary>
        /// 获取今天北京时间0点
        /// </summary>
        private long GetTodayBeijingStart()
        {
            var now = GetBeijingTime();
            return new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, BeijingOffset).ToUnixTimeSeconds();
        }

        /// <summary>
        /// 获取本周周一北京时间0点
        /// </summary>
        private long GetThisWeekBeijingStart()
        {
            var now = GetBeijingTime();
            var dayOfWeek = (int)now.DayOfWeek;
            var monday = now.AddDays(-dayOfWeek);
            return new DateTimeOffset(monday.Year, monday.Month, monday.Day, 0, 0, 0, BeijingOffset).ToUnixTimeSeconds();
        }

        /// <summary>
        /// 刷新每日任务
        /// </summary>
        public void RefreshDailyTasks()
        {
            var now = GetBeijingTime();
            var refreshTime = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, BeijingOffset).AddDays(1).ToUnixTimeSeconds();

            // 清除旧数据
            _dailyTasks.Clear();

            // 随机选择3个每日任务
            var availableTemplates = _dailyTemplates.FindAll(t => t.isActive);
            ShuffleList(availableTemplates);

            var count = Math.Min(DAILY_TASK_COUNT, availableTemplates.Count);
            for (int i = 0; i < count; i++)
            {
                var template = availableTemplates[i];
                _dailyTasks.Add(new TaskInstance
                {
                    templateId = template.id,
                    category = TaskCategory.Daily,
                    currentValue = 0,
                    targetValue = template.targetValue,
                    isCompleted = false,
                    isClaimed = false,
                    refreshTimestamp = refreshTime
                });
            }

            Debug.Log($"[TaskDesign] Refreshed {count} daily tasks");
            onTasksRefreshed?.Invoke();
        }

        /// <summary>
        /// 刷新每周任务
        /// </summary>
        public void RefreshWeeklyTasks()
        {
            var now = GetBeijingTime();
            var dayOfWeek = (int)now.DayOfWeek;
            var nextMonday = now.AddDays(7 - dayOfWeek);
            var refreshTime = new DateTimeOffset(nextMonday.Year, nextMonday.Month, nextMonday.Day, 0, 0, 0, BeijingOffset).ToUnixTimeSeconds();

            // 清除旧数据
            _weeklyTasks.Clear();

            // 随机选择2个周常任务
            var availableTemplates = _weeklyTemplates.FindAll(t => t.isActive);
            ShuffleList(availableTemplates);

            var count = Math.Min(WEEKLY_TASK_COUNT, availableTemplates.Count);
            for (int i = 0; i < count; i++)
            {
                var template = availableTemplates[i];
                _weeklyTasks.Add(new TaskInstance
                {
                    templateId = template.id,
                    category = TaskCategory.Weekly,
                    currentValue = 0,
                    targetValue = template.targetValue,
                    isCompleted = false,
                    isClaimed = false,
                    refreshTimestamp = refreshTime
                });
            }

            Debug.Log($"[TaskDesign] Refreshed {count} weekly tasks");
            onTasksRefreshed?.Invoke();
        }

        private static void ShuffleList<T>(List<T> list)
        {
            var random = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void BuildConditionIndex()
        {
            _conditionIndex.Clear();

            // 索引每日任务
            foreach (var task in _dailyTasks)
            {
                var template = GetTemplate(task.templateId, TaskCategory.Daily);
                if (template == null) continue;

                if (!_conditionIndex.ContainsKey(template.conditionType))
                    _conditionIndex[template.conditionType] = new List<TaskInstance>();

                _conditionIndex[template.conditionType].Add(task);
            }

            // 索引每周任务
            foreach (var task in _weeklyTasks)
            {
                var template = GetTemplate(task.templateId, TaskCategory.Weekly);
                if (template == null) continue;

                if (!_conditionIndex.ContainsKey(template.conditionType))
                    _conditionIndex[template.conditionType] = new List<TaskInstance>();

                _conditionIndex[template.conditionType].Add(task);
            }
        }

        private TaskTemplate? GetTemplate(string templateId, TaskCategory category)
        {
            var templates = category == TaskCategory.Daily ? _dailyTemplates : _weeklyTemplates;
            return templates.Find(t => t.id == templateId);
        }
        #endregion

        #region 进度上报
        /// <summary>
        /// 上报任务进度
        /// </summary>
        public void ReportProgress(AchievementConditionType type, float delta)
        {
            if (!_conditionIndex.TryGetValue(type, out var tasks))
                return;

            foreach (var task in tasks)
            {
                if (task.isCompleted || task.isClaimed)
                    continue;

                task.currentValue += delta;

                // 检查是否完成
                if (task.currentValue >= task.targetValue)
                {
                    task.isCompleted = true;
                    onTaskCompleted?.Invoke(task);
                }
            }
        }
        #endregion

        #region 奖励领取
        /// <summary>
        /// 领取任务奖励
        /// </summary>
        public bool ClaimReward(string taskId)
        {
            TaskInstance? task = null;
            TaskCategory category = TaskCategory.Daily;

            // 查找任务
            task = _dailyTasks.Find(t => t.templateId == taskId);
            if (task != null)
            {
                category = TaskCategory.Daily;
            }
            else
            {
                task = _weeklyTasks.Find(t => t.templateId == taskId);
                if (task != null)
                    category = TaskCategory.Weekly;
            }

            if (task == null)
            {
                Debug.LogWarning($"[TaskDesign] Task not found: {taskId}");
                return false;
            }

            if (!task.isCompleted)
            {
                Debug.LogWarning($"[TaskDesign] Task not completed: {taskId}");
                return false;
            }

            if (task.isClaimed)
            {
                Debug.LogWarning($"[TaskDesign] Reward already claimed: {taskId}");
                return false;
            }

            // 发放奖励
            var template = GetTemplate(taskId, category);
            if (template != null)
            {
                GrantReward(template.reward);
            }

            task.isClaimed = true;
            onRewardClaimed?.Invoke(task);

            Debug.Log($"[TaskDesign] Reward claimed for task: {taskId}");
            return true;
        }

        private void GrantReward(AchievementRewardData reward)
        {
            switch (reward.type)
            {
                case RewardType.Gold:
                    // PlayerActor.instance.AddGold(reward.amount);
                    Debug.Log($"[TaskDesign] Granted gold reward: {reward.amount}");
                    break;
                case RewardType.Item:
                    ItemManager.AddItem(reward.configId, reward.amount);
                    Debug.Log($"[TaskDesign] Granted item reward: {reward.configId} x{reward.amount}");
                    break;
                case RewardType.Experience:
                    // PlayerActor.instance.AddExp(reward.amount);
                    Debug.Log($"[TaskDesign] Granted exp reward: {reward.amount}");
                    break;
                case RewardType.Title:
                    // 处理称号奖励
                    Debug.Log($"[TaskDesign] Granted title reward: {reward.configId}");
                    break;
            }
        }
        #endregion

        #region 存档
        public TaskSaveData Export()
        {
            var data = new TaskSaveData
            {
                version = 1,
                dailyTasks = new List<TaskInstance>(_dailyTasks),
                weeklyTasks = new List<TaskInstance>(_weeklyTasks),
                lastDailyRefreshTime = _dailyTasks.Count > 0 ? _dailyTasks[0].refreshTimestamp : 0,
                lastWeeklyRefreshTime = _weeklyTasks.Count > 0 ? _weeklyTasks[0].refreshTimestamp : 0
            };
            return data;
        }

        public void Import(TaskSaveData data)
        {
            if (data == null) return;

            _dailyTasks = data.dailyTasks ?? new List<TaskInstance>();
            _weeklyTasks = data.weeklyTasks ?? new List<TaskInstance>();

            BuildConditionIndex();
            Debug.Log($"[TaskDesign] Imported {_dailyTasks.Count} daily tasks, {_weeklyTasks.Count} weekly tasks");
        }
        #endregion

        #region 查询API
        public List<TaskInstance> GetDailyTasks() => new List<TaskInstance>(_dailyTasks);
        public List<TaskInstance> GetWeeklyTasks() => new List<TaskInstance>(_weeklyTasks);

        public TaskTemplate? GetTemplate(string templateId)
        {
            var template = _dailyTemplates.Find(t => t.id == templateId);
            if (template != null) return template;
            return _weeklyTemplates.Find(t => t.id == templateId);
        }

        public TaskInstance? GetTaskInstance(string templateId)
        {
            var task = _dailyTasks.Find(t => t.templateId == templateId);
            if (task != null) return task;
            return _weeklyTasks.Find(t => t.templateId == templateId);
        }
        #endregion
    }

    [System.Serializable]
    public class TaskSaveData
    {
        public int version;
        public List<TaskInstance> dailyTasks = new();
        public List<TaskInstance> weeklyTasks = new();
        public long lastDailyRefreshTime;
        public long lastWeeklyRefreshTime;
    }
}