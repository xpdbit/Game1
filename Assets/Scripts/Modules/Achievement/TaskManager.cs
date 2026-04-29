using System.Collections.Generic;
using UnityEngine;

namespace Game1.Modules.Achievement
{
    // 静态API
    public static class TaskManager
    {
        private static bool _isLoaded = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_isLoaded) return;
            TaskDesign.instance.Initialize();
            _isLoaded = true;
        }

        public static void ReportProgress(AchievementConditionType type, float delta)
            => TaskDesign.instance.ReportProgress(type, delta);
        public static List<TaskInstance> GetDailyTasks()
            => TaskDesign.instance.GetDailyTasks();
        public static List<TaskInstance> GetWeeklyTasks()
            => TaskDesign.instance.GetWeeklyTasks();
        public static void CheckRefresh()
            => TaskDesign.instance.CheckRefresh();
        public static bool ClaimReward(string taskId)
            => TaskDesign.instance.ClaimReward(taskId);
    }
}