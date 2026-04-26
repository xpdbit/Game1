using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// InventoryDesign 单元测试
    /// 测试背包核心逻辑
    /// </summary>
    public class InventoryDesignTests
    {
        private InventoryDesign _inventory;
        private ItemTemplate _testSwordTemplate;
        private ItemTemplate _testPotionTemplate;

        [SetUp]
        public void SetUp()
        {
            // 每个测试创建独立的InventoryDesign实例
            _inventory = new InventoryDesign();

            // 创建测试用物品模板
            _testSwordTemplate = new ItemTemplate
            {
                id = "Test.Item.Sword",
                nameTextId = "Test Sword",
                type = ItemType.Weapon,
                weight = 2.0f,
                damage = 10,
                maxStack = 1
            };

            _testPotionTemplate = new ItemTemplate
            {
                id = "Test.Item.Potion",
                nameTextId = "Test Potion",
                type = ItemType.Consumable,
                weight = 0.5f,
                maxStack = 99
            };

            // 注册测试模板到ItemManager
            RegisterTestTemplate(_testSwordTemplate);
            RegisterTestTemplate(_testPotionTemplate);
        }

        [TearDown]
        public void TearDown()
        {
            // 清理
            if (_inventory != null)
            {
                _inventory.Clear();
            }
            RemoveTestTemplate("Test.Item.Sword");
            RemoveTestTemplate("Test.Item.Potion");
        }

        /// <summary>
        /// 注册测试模板到ItemManager
        /// </summary>
        private void RegisterTestTemplate(ItemTemplate template)
        {
            var field = typeof(ItemManager).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                var templates = field.GetValue(null) as Dictionary<string, ItemTemplate>;
                if (templates != null)
                {
                    templates[template.id] = template;
                }
            }
        }

        /// <summary>
        /// 移除测试模板
        /// </summary>
        private void RemoveTestTemplate(string templateId)
        {
            var field = typeof(ItemManager).GetField("_templates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                var templates = field.GetValue(null) as Dictionary<string, ItemTemplate>;
                templates?.Remove(templateId);
            }
        }

        #region AddItem Tests

        [Test]
        public void AddItem_Success_SingleItem()
        {
            // Arrange
            var templateId = "Test.Item.Sword";

            // Act
            var result = _inventory.AddItem(templateId, 1);

            // Assert
            Assert.IsTrue(result.success, $"AddItem failed: {result.message}");
            Assert.AreEqual(1, _inventory.slotCount);
            Assert.AreEqual(2.0f, _inventory.totalWeight);
        }

        [Test]
        public void AddItem_Success_StackableItem()
        {
            // Arrange
            var templateId = "Test.Item.Potion";

            // Act
            var result = _inventory.AddItem(templateId, 50);

            // Assert
            Assert.IsTrue(result.success, $"AddItem failed: {result.message}");
            Assert.AreEqual(1, _inventory.slotCount); // 应该堆叠到同一个slot
            Assert.AreEqual(25.0f, _inventory.totalWeight); // 50 * 0.5
        }

        [Test]
        public void AddItem_Fail_InvalidTemplateId()
        {
            // Act
            var result = _inventory.AddItem("NonExistent.Item", 1);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Template not found: NonExistent.Item", result.message);
        }

        [Test]
        public void AddItem_Fail_EmptyTemplateId()
        {
            // Act
            var result = _inventory.AddItem("", 1);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("TemplateId is null or empty", result.message);
        }

        [Test]
        public void AddItem_Fail_ZeroAmount()
        {
            // Act
            var result = _inventory.AddItem("Test.Item.Sword", 0);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Amount must be positive", result.message);
        }

        [Test]
        public void AddItem_Fail_NegativeAmount()
        {
            // Act
            var result = _inventory.AddItem("Test.Item.Sword", -5);

            // Assert
            Assert.IsFalse(result.success);
            Assert.AreEqual("Amount must be positive", result.message);
        }

        #endregion

        #region RemoveItem Tests

        [Test]
        public void RemoveItem_Success_PartialAmount()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Potion", 50);
            var item = _inventory.GetItemsByTemplateId("Test.Item.Potion")[0];
            var originalInstanceId = item.instanceId;

            // Act
            var result = _inventory.RemoveItem(originalInstanceId, 30);

            // Assert
            Assert.IsTrue(result.success, $"RemoveItem failed: {result.message}");
            Assert.AreEqual(1, _inventory.slotCount);
            Assert.AreEqual(20, _inventory.GetItem(originalInstanceId)?.amount); // 剩余20个
            Assert.AreEqual(10.0f, _inventory.totalWeight); // 20 * 0.5
        }

        [Test]
        public void RemoveItem_Success_FullAmount()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Potion", 50);
            var item = _inventory.GetItemsByTemplateId("Test.Item.Potion")[0];
            var originalInstanceId = item.instanceId;

            // Act
            var result = _inventory.RemoveItem(originalInstanceId, 50); // 全部移除

            // Assert
            Assert.IsTrue(result.success, $"RemoveItem failed: {result.message}");
            Assert.AreEqual(0, _inventory.slotCount);
            Assert.IsNull(_inventory.GetItem(originalInstanceId));
        }

        [Test]
        public void RemoveItem_Success_ZeroAmount_RemovesAll()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Potion", 50);
            var item = _inventory.GetItemsByTemplateId("Test.Item.Potion")[0];
            var originalInstanceId = item.instanceId;

            // Act
            var result = _inventory.RemoveItem(originalInstanceId, 0); // 0表示全部

            // Assert
            Assert.IsTrue(result.success, $"RemoveItem failed: {result.message}");
            Assert.AreEqual(0, _inventory.slotCount);
        }

        [Test]
        public void RemoveItem_Fail_NotFound()
        {
            // Act
            var result = _inventory.RemoveItem(99999, 1);

            // Assert
            Assert.IsFalse(result.success);
            Assert.IsTrue(result.message.Contains("not found"));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_Success_RemovesAllItems()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Sword", 1);
            _inventory.AddItem("Test.Item.Potion", 20);

            // Act
            _inventory.Clear();

            // Assert
            Assert.AreEqual(0, _inventory.slotCount);
            Assert.AreEqual(0, _inventory.totalWeight);
        }

        #endregion

        #region Query Tests

        [Test]
        public void GetItem_Success_ExistingItem()
        {
            // Arrange
            var result = _inventory.AddItem("Test.Item.Sword", 1);
            var instanceId = result.instanceId;

            // Act
            var item = _inventory.GetItem(instanceId);

            // Assert
            Assert.IsNotNull(item);
            Assert.AreEqual("Test.Item.Sword", item.itemTemplate.id);
        }

        [Test]
        public void GetItem_Fail_NonExistent()
        {
            // Act
            var item = _inventory.GetItem(99999);

            // Assert
            Assert.IsNull(item);
        }

        [Test]
        public void GetItemsByTemplateId_Success_MultipleInstances()
        {
            // Arrange - 不可堆叠物品会创建多个实例
            _inventory.AddItem("Test.Item.Sword", 1);
            _inventory.AddItem("Test.Item.Sword", 1);

            // Act
            var items = _inventory.GetItemsByTemplateId("Test.Item.Sword");

            // Assert
            Assert.AreEqual(2, items.Count);
        }

        [Test]
        public void GetTotalAmountByTemplateId_Success()
        {
            // Arrange - 先添加一个可堆叠的
            _inventory.AddItem("Test.Item.Potion", 30);
            var item = _inventory.GetItemsByTemplateId("Test.Item.Potion")[0];

            // Act
            var total = _inventory.GetTotalAmountByTemplateId("Test.Item.Potion");

            // Assert
            Assert.AreEqual(30, total);
        }

        #endregion

        #region Capacity Tests

        [Test]
        public void CanAddItem_Success_EnoughSlots()
        {
            // Act
            var canAdd = _inventory.CanAddItem("Test.Item.Sword", 1);

            // Assert
            Assert.IsTrue(canAdd);
        }

        [Test]
        public void CanAddItem_Fail_NoSlots()
        {
            // Arrange - 填满背包
            _inventory.capacity = new InventoryCapacity(2, 1000f);
            _inventory.AddItem("Test.Item.Sword", 1);
            _inventory.AddItem("Test.Item.Potion", 1);

            // Act
            var canAdd = _inventory.CanAddItem("Test.Item.Sword", 1);

            // Assert
            Assert.IsFalse(canAdd);
        }

        [Test]
        public void CanAddItem_Fail_ExceedsWeight()
        {
            // Arrange
            _inventory.capacity = new InventoryCapacity(50, 5f); // 只够放10个potion

            // Act
            var canAdd = _inventory.CanAddItem("Test.Item.Potion", 20); // 需要10重量

            // Assert
            Assert.IsFalse(canAdd);
        }

        [Test]
        public void CanAddSlot_Success_HasEmptySlot()
        {
            // Assert
            Assert.IsTrue(_inventory.CanAddSlot());
        }

        [Test]
        public void CanAddSlot_Fail_Full()
        {
            // Arrange
            _inventory.capacity = new InventoryCapacity(1, 1000f);
            _inventory.AddItem("Test.Item.Sword", 1);

            // Assert
            Assert.IsFalse(_inventory.CanAddSlot());
        }

        [Test]
        public void CanAddWeight_Success()
        {
            // Assert
            Assert.IsTrue(_inventory.CanAddWeight(50f));
        }

        [Test]
        public void CanAddWeight_Fail_Exceeds()
        {
            // Arrange
            _inventory.capacity = new InventoryCapacity(50, 10f);

            // Assert
            Assert.IsFalse(_inventory.CanAddWeight(15f));
        }

        [Test]
        public void RemainingSlotCount_ReturnsCorrectValue()
        {
            // Arrange
            _inventory.capacity = new InventoryCapacity(10, 100f);
            _inventory.AddItem("Test.Item.Sword", 1);
            _inventory.AddItem("Test.Item.Potion", 1);

            // Assert
            Assert.AreEqual(8, _inventory.RemainingSlotCount());
        }

        [Test]
        public void RemainingWeight_ReturnsCorrectValue()
        {
            // Arrange
            _inventory.capacity = new InventoryCapacity(50, 10f);
            _inventory.AddItem("Test.Item.Potion", 10); // 5 weight used

            // Assert
            Assert.AreEqual(5f, _inventory.RemainingWeight());
        }

        #endregion

        #region Batch Operation Tests

        [Test]
        public void AddItems_Batch_Success()
        {
            // Arrange
            var items = new List<(string, int)>
            {
                ("Test.Item.Sword", 1),
                ("Test.Item.Potion", 10)
            };

            // Act
            var results = _inventory.AddItems(items);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].success);
            Assert.IsTrue(results[1].success);
            Assert.AreEqual(2, _inventory.slotCount);
        }

        [Test]
        public void RemoveItems_Batch_Success()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Sword", 1);
            var swordItem = _inventory.GetItemsByTemplateId("Test.Item.Sword")[0];
            _inventory.AddItem("Test.Item.Potion", 10);
            var potionItem = _inventory.GetItemsByTemplateId("Test.Item.Potion")[0];

            var items = new List<(int, int)>
            {
                (swordItem.instanceId, 1),
                (potionItem.instanceId, 5)
            };

            // Act
            var results = _inventory.RemoveItems(items);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].success);
            Assert.IsTrue(results[1].success);
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Export_ReturnsCorrectData()
        {
            // Arrange
            _inventory.AddItem("Test.Item.Sword", 1);
            _inventory.AddItem("Test.Item.Potion", 20);
            var swordItem = _inventory.GetItemsByTemplateId("Test.Item.Sword")[0];

            // Act
            var saveData = _inventory.Export();

            // Assert
            Assert.AreEqual(2, saveData.Count);
        }

        [Test]
        public void Import_RestoresCorrectState()
        {
            // Arrange
            var swordItem = _inventory.AddItem("Test.Item.Sword", 1);
            var potionItem = _inventory.AddItem("Test.Item.Potion", 20);
            var exportData = _inventory.Export();

            // Act - 创建新实例并导入
            var newInventory = new InventoryDesign();
            newInventory.Import(exportData);

            // Assert
            Assert.AreEqual(2, newInventory.slotCount);
            Assert.AreEqual(20, newInventory.GetTotalAmountByTemplateId("Test.Item.Potion"));
        }

        #endregion
    }
}
