using System;
using System.Collections.Generic;
using System.Xml;

namespace Game1.Core.SaveSystem
{
    public sealed class TaskSaveFile : ISaveFile
    {
        public string FileName => "task.xml";
        public int Version => 1;

        public long lastDailyRefresh;
        public long lastWeeklyRefresh;
        public List<TaskEntry> dailyTasks = new List<TaskEntry>();
        public List<TaskEntry> weeklyTasks = new List<TaskEntry>();

        public string ToXml()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<TaskSaveFile>");
            sb.Append($"<lastDailyRefresh>{lastDailyRefresh}</lastDailyRefresh>");
            sb.Append($"<lastWeeklyRefresh>{lastWeeklyRefresh}</lastWeeklyRefresh>");

            sb.Append("<dailyTasks>");
            foreach (var task in dailyTasks)
            {
                sb.Append($"<TaskEntry templateId=\"{XmlEscape.EscapeXml(task.templateId)}\" currentValue=\"{task.currentValue}\" targetValue=\"{task.targetValue}\" isCompleted=\"{task.isCompleted}\" isClaimed=\"{task.isClaimed}\"/>");
            }
            sb.Append("</dailyTasks>");

            sb.Append("<weeklyTasks>");
            foreach (var task in weeklyTasks)
            {
                sb.Append($"<TaskEntry templateId=\"{XmlEscape.EscapeXml(task.templateId)}\" currentValue=\"{task.currentValue}\" targetValue=\"{task.targetValue}\" isCompleted=\"{task.isCompleted}\" isClaimed=\"{task.isClaimed}\"/>");
            }
            sb.Append("</weeklyTasks>");

            sb.Append("</TaskSaveFile>");
            return sb.ToString();
        }

        public void ParseFromXml(XmlElement element)
        {
            dailyTasks.Clear();
            weeklyTasks.Clear();

            var lastDailyRefreshNode = element.SelectSingleNode("lastDailyRefresh");
            if (lastDailyRefreshNode != null)
                lastDailyRefresh = long.Parse(lastDailyRefreshNode.InnerText);

            var lastWeeklyRefreshNode = element.SelectSingleNode("lastWeeklyRefresh");
            if (lastWeeklyRefreshNode != null)
                lastWeeklyRefresh = long.Parse(lastWeeklyRefreshNode.InnerText);

            var dailyTasksNode = element.SelectSingleNode("dailyTasks");
            if (dailyTasksNode != null)
            {
                foreach (XmlElement taskElement in dailyTasksNode.ChildNodes)
                {
                    dailyTasks.Add(new TaskEntry
                    {
                        templateId = taskElement.GetAttribute("templateId"),
                        currentValue = float.Parse(taskElement.GetAttribute("currentValue")),
                        targetValue = float.Parse(taskElement.GetAttribute("targetValue")),
                        isCompleted = bool.Parse(taskElement.GetAttribute("isCompleted")),
                        isClaimed = bool.Parse(taskElement.GetAttribute("isClaimed"))
                    });
                }
            }

            var weeklyTasksNode = element.SelectSingleNode("weeklyTasks");
            if (weeklyTasksNode != null)
            {
                foreach (XmlElement taskElement in weeklyTasksNode.ChildNodes)
                {
                    weeklyTasks.Add(new TaskEntry
                    {
                        templateId = taskElement.GetAttribute("templateId"),
                        currentValue = float.Parse(taskElement.GetAttribute("currentValue")),
                        targetValue = float.Parse(taskElement.GetAttribute("targetValue")),
                        isCompleted = bool.Parse(taskElement.GetAttribute("isCompleted")),
                        isClaimed = bool.Parse(taskElement.GetAttribute("isClaimed"))
                    });
                }
            }
        }

        public class TaskEntry
        {
            public string templateId;
            public float currentValue;
            public float targetValue;
            public bool isCompleted;
            public bool isClaimed;
        }
    }
}