using System.Collections.Generic;
using NUnit.Framework;
using Game1.Modules.PendingEvent;

namespace Game1.Tests.EditMode.PendingEvent
{
    /// <summary>
    /// PendingEventDesign 单元测试
    /// </summary>
    public class PendingEventDesignTests
    {
        [SetUp]
        public void SetUp()
        {
            PendingEventDesign.instance.Clear();
            PendingEventDesign.instance.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            PendingEventDesign.instance.Clear();
        }

        #region Initialization Tests

        [Test]
        public void Initialize_SetsInitialized()
        {
            // Arrange
            PendingEventDesign.instance.Clear();

            // Act
            PendingEventDesign.instance.Initialize();

            // Assert
            Assert.IsFalse(PendingEventDesign.instance.hasPendingEvents);
            Assert.AreEqual(0, PendingEventDesign.instance.pendingCount);
        }

        #endregion

        #region Generation Tests

        [Test]
        public void GeneratePendingEvents_WithNoOfflineTime_CreatesNoEvents()
        {
            // Act
            PendingEventDesign.instance.GeneratePendingEvents(0f);

            // Assert
            Assert.AreEqual(0, PendingEventDesign.instance.pendingCount);
        }

        [Test]
        public void GeneratePendingEvents_With1HourOffline_CreatesEvents()
        {
            // Act
            PendingEventDesign.instance.GeneratePendingEvents(3600f);

            // Assert
            Assert.Greater(PendingEventDesign.instance.pendingCount, 0,
                "1小时离线应生成积压事件");
        }

        [Test]
        public void GeneratePendingEvents_With24HourOffline_IsCappedAt50()
        {
            // Act
            PendingEventDesign.instance.GeneratePendingEvents(86400f);

            // Assert
            Assert.LessOrEqual(PendingEventDesign.instance.totalCount, 50,
                "单次最多生成50个事件");
        }

        [Test]
        public void GeneratePendingEvents_EventsHaveTimestamps()
        {
            // Act
            PendingEventDesign.instance.GeneratePendingEvents(7200f);

            // Assert
            var events = PendingEventDesign.instance.GetPendingEvents();
            foreach (var e in events)
            {
                Assert.Greater(e.timestamp, 0, "每个事件应有时间戳");
                Assert.IsFalse(string.IsNullOrEmpty(e.templateId), "每个事件应有模板ID");
            }
        }

        #endregion

        #region Rarity Distribution Tests

        [Test]
        public void RarityDistribution_ReturnsCorrectCounts()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(36000f);

            // Act
            var dist = PendingEventDesign.instance.GetRarityDistribution();

            // Assert
            Assert.AreEqual(PendingEventDesign.instance.pendingCount,
                dist.normalCount + dist.rareCount + dist.legendaryCount,
                "稀有度分布合计应等于总事件数");
        }

        [Test]
        public void RarityWeight_RollsAreWithinRange()
        {
            // Arrange
            var config = new RarityWeightConfig();

            // Act - 测试100次
            for (int i = 0; i < 100; i++)
            {
                var rarity = config.RollRarity();
                Assert.IsTrue(rarity == PendingEventRarity.Normal
                    || rarity == PendingEventRarity.Rare
                    || rarity == PendingEventRarity.Legendary,
                    $"稀有度应在枚举范围内: {rarity}");
            }
        }

        #endregion

        #region Processing Tests

        [Test]
        public void ProcessEvent_WithValidId_ReturnsResult()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);
            var events = PendingEventDesign.instance.GetPendingEvents();
            Assert.Greater(events.Count, 0, "应有事件可处理");

            var eventId = events[0].eventId;

            // Act
            var result = PendingEventManager.ProcessEvent(eventId);

            // Assert
            Assert.IsNotNull(result, "处理有效事件应返回结果");
            Assert.IsTrue(result.success, "处理应成功");
        }

        [Test]
        public void ProcessEvent_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = PendingEventManager.ProcessEvent("nonexistent_id");

            // Assert
            Assert.IsNull(result, "无效ID应返回null");
        }

        [Test]
        public void ProcessEvent_MarksEventAsProcessed()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);
            var events = PendingEventDesign.instance.GetPendingEvents();
            var eventId = events[0].eventId;

            // Act
            PendingEventManager.ProcessEvent(eventId);

            // Assert
            Assert.AreEqual(events.Count - 1, PendingEventDesign.instance.pendingCount,
                "处理后待处理数应减1");
        }

        [Test]
        public void ProcessBatch_ProcessesMultipleEvents()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(7200f);
            var events = PendingEventDesign.instance.GetPendingEvents();
            Assert.GreaterOrEqual(events.Count, 2, "至少需要2个事件才能测试批量处理");

            var ids = new List<string> { events[0].eventId, events[1].eventId };

            // Act
            var results = PendingEventManager.ProcessBatch(ids);

            // Assert
            Assert.AreEqual(2, results.Count, "批量处理应处理2个事件");
            Assert.AreEqual(events.Count - 2, PendingEventDesign.instance.pendingCount,
                "处理后待处理数应减2");
        }

        [Test]
        public void ProcessAllPending_ProcessesAllUnprocessedEvents()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);
            int count = PendingEventDesign.instance.pendingCount;

            // Act
            var results = PendingEventManager.ProcessAllPending();

            // Assert
            Assert.AreEqual(count, results.Count, "应处理所有待处理事件");
            Assert.AreEqual(0, PendingEventDesign.instance.pendingCount, "处理后应无待处理事件");
        }

        #endregion

        #region Query Tests

        [Test]
        public void GetPendingByRarity_FiltersCorrectly()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(36000f);

            // Act
            var normalEvents = PendingEventDesign.instance.GetPendingByRarity(PendingEventRarity.Normal);
            var rareEvents = PendingEventDesign.instance.GetPendingByRarity(PendingEventRarity.Rare);
            var legendaryEvents = PendingEventDesign.instance.GetPendingByRarity(PendingEventRarity.Legendary);

            // Assert
            foreach (var e in normalEvents)
                Assert.AreEqual(PendingEventRarity.Normal, e.rarity);
            foreach (var e in rareEvents)
                Assert.AreEqual(PendingEventRarity.Rare, e.rarity);
            foreach (var e in legendaryEvents)
                Assert.AreEqual(PendingEventRarity.Legendary, e.rarity);
        }

        [Test]
        public void GetTimeline_ReturnsEventsInDescendingOrder()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(7200f);

            // Act
            var timeline = PendingEventDesign.instance.GetTimeline();

            // Assert
            for (int i = 1; i < timeline.Count; i++)
            {
                Assert.GreaterOrEqual(timeline[i - 1].timestamp, timeline[i].timestamp,
                    "时间线应按时间倒序排列");
            }
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Export_Import_RoundTrip_PreservesData()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);
            int originalCount = PendingEventDesign.instance.totalCount;

            // Act
            var saveData = PendingEventDesign.instance.Export();
            PendingEventDesign.instance.Clear();
            PendingEventDesign.instance.Import(saveData);

            // Assert
            Assert.AreEqual(originalCount, PendingEventDesign.instance.totalCount,
                "导出导入后事件总数应一致");
        }

        [Test]
        public void Import_NullData_DoesNotCrash()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                PendingEventDesign.instance.Import(null);
            });
        }

        [Test]
        public void Export_ToXml_ProducesValidXml()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);
            var saveData = PendingEventDesign.instance.Export();

            // Act
            string xml = saveData.ToXml();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(xml));
            Assert.IsTrue(xml.StartsWith("<PendingEventSaveData>"), "XML应以正确标签开始");
            Assert.IsTrue(xml.EndsWith("</PendingEventSaveData>"), "XML应以正确标签结束");
        }

        #endregion

        #region Brief Tests

        [Test]
        public void GetBrief_WithEvents_ReturnsSummary()
        {
            // Arrange
            PendingEventDesign.instance.GeneratePendingEvents(3600f);

            // Act
            var brief = PendingEventManager.GetBrief();

            // Assert
            Assert.Greater(brief.totalCount, 0, "应有事件汇总");
            Assert.IsFalse(string.IsNullOrEmpty(brief.summaryText), "应有汇总文本");
            Assert.Greater(brief.totalGoldPreview, 0, "应有金币预览");
        }

        [Test]
        public void GetBrief_WithNoEvents_ReturnsEmptySummary()
        {
            // Act
            var brief = PendingEventManager.GetBrief();

            // Assert
            Assert.AreEqual(0, brief.totalCount, "无事件时count应为0");
            Assert.IsFalse(string.IsNullOrEmpty(brief.summaryText), "应有空事件的描述文本");
        }

        #endregion
    }
}
