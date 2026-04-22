using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 游戏功能测试类
    /// 用于测试库存、事件、挂机等核心系统
    /// </summary>
    public class GameTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _runInventoryTest = true;
        [SerializeField] private bool _runEventTest = true;
        [SerializeField] private bool _runIdleTest = true;
        [SerializeField] private bool _runEventBusTest = true;

        private PlayerActor _testPlayer;
        private IdleRewardModule _idleModule;

        private void Start()
        {
            ItemManager.Initialize();

            _testPlayer = new PlayerActor();
            _idleModule = new IdleRewardModule
            {
                baseRewardPerSecond = 10f,
                offlineRewardRate = 0.5f
            };
            _idleModule.Initialize(_testPlayer);
            _testPlayer.AddModule(_idleModule);

            if (_runInventoryTest) RunInventoryTests();
            if (_runEventBusTest) RunEventBusTests();
            if (_runEventTest) RunEventTests();
            if (_runIdleTest) RunIdleTests();

            Debug.Log("[GameTest] All tests completed!");
        }

        #region Inventory Tests

        private void RunInventoryTests()
        {
            TestInventoryAdd();
            // TestInventoryRemove();
            TestInventoryCapacity();
            TestInventoryBatchOperations();
        }

        private void TestInventoryAdd()
        {
            ItemManager.AddItem("Core.Item.GoldCoin", 100);
            ItemManager.AddItem("Core.Item.Bacon", 5);
            ItemManager.AddItem("Core.Item.Bacon", 2);
            ItemManager.AddItem("Non.Existent.Item", 1);
            ItemManager.AddItem("Core.Item.ShortBlade", 0);

            RefreshInventoryUI();
        }

        private void TestInventoryRemove()
        {
            var items = ItemManager.GetInventory();
            if (items.Count > 0)
            {
                var firstItem = items[0];
                ItemManager.RemoveItem(firstItem.instanceId, 1);
                ItemManager.RemoveItem(firstItem.instanceId, 0);
            }
            ItemManager.RemoveItem(99999, 1);

            RefreshInventoryUI();
        }

        private void TestInventoryCapacity()
        {
            ItemManager.SetInventoryCapacity(20, 50f);
            ItemManager.SetInventoryCapacity(100, 500f);
        }

        private void TestInventoryBatchOperations()
        {
            ItemManager.ClearInventory();
            ItemManager.AddItem("Core.Item.GoldCoin", 50);
            ItemManager.AddItem("Core.Item.Bacon", 10);
        }

        private void RefreshInventoryUI()
        {
            if (UIManager.instance?.inventory != null)
            {
                UIManager.instance.inventory.Refresh();
            }
        }

        #endregion

        #region EventBus Tests

        private void RunEventBusTests()
        {
            var subscriber = new TestInventorySubscriber();
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemAdded, subscriber);
            InventoryEventBus.instance.Subscribe(InventoryEventType.ItemRemoved, subscriber);

            ItemManager.AddItem("Core.Item.Money", 25);
            ItemManager.AddItem("Core.Item.Bacon", 1);

            InventoryEventBus.instance.UnsubscribeAll(subscriber);
            InventoryEventBus.instance.Clear();
        }

        private class TestInventorySubscriber : IInventoryEventSubscriber
        {
            public void OnInventoryEvent(InventoryEventData data)
            {
                Debug.Log($"[Event] {data.eventType}: {data.templateId} x{data.amount}");
            }
        }

        #endregion

        #region Event Tests

        private void RunEventTests()
        {
            var queue = new EventQueue();
            var chainMgr = EventChainManager.instance;
        }

        #endregion

        #region Idle Tests

        private void RunIdleTests()
        {
            _idleModule.OnActivate();
            _idleModule.Tick(1f);
            _idleModule.OnDeactivate();

            float offlineReward = _idleModule.CalculateOfflineReward(3600f);
        }

        #endregion
    }
}
