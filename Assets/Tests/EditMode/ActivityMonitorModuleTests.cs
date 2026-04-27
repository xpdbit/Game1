using System.Reflection;
using Game1.Modules.Activity;
using NUnit.Framework;

namespace Game1.Tests.EditMode
{
    /// <summary>
    /// ActivityMonitorModule 单元测试
    /// 测试活跃度监控模块逻辑
    /// </summary>
    public class ActivityMonitorModuleTests
    {
        private ActivityMonitorModule _activityMonitor;

        [SetUp]
        public void SetUp()
        {
            // 每个测试创建独立的ActivityMonitorModule实例
            _activityMonitor = new ActivityMonitorModule();

            // 通过反射设置内部状态
            SetPrivateField("_accumulatedActivity", 0f);
            SetPrivateField("_displayedActivity", 0);
            SetPrivateField("_validOperationCount", 0);
            SetPrivateField("_minuteTimer", 0f);
            SetPrivateField("_lastOperationKey", -1);
            SetPrivateField("_isActive", true);

            // 清空键位连续计数
            var keyCountField = typeof(ActivityMonitorModule).GetField("_keyConsecutiveCount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (keyCountField != null)
            {
                keyCountField.SetValue(_activityMonitor, new System.Collections.Generic.Dictionary<int, int>());
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.OnDeactivate();
            }
        }

        /// <summary>
        /// 通过反射设置私有字段
        /// </summary>
        private void SetPrivateField(string fieldName, object value)
        {
            var field = typeof(ActivityMonitorModule).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(_activityMonitor, value);
            }
        }

        /// <summary>
        /// 通过反射获取私有字段值
        /// </summary>
        private T GetPrivateField<T>(string fieldName)
        {
            var field = typeof(ActivityMonitorModule).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                return (T)field.GetValue(_activityMonitor);
            }
            return default;
        }

        #region GetCurrentTier Tests

        [Test]
        public void GetCurrentTier_ActivityZero_ReturnsLow()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 0);
            SetPrivateField("_accumulatedActivity", 0f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.Low, tier);
        }

        [Test]
        public void GetCurrentTier_Activity29_ReturnsLow()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 29);
            SetPrivateField("_accumulatedActivity", 29f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.Low, tier);
        }

        [Test]
        public void GetCurrentTier_Activity30_ReturnsNormal()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 30);
            SetPrivateField("_accumulatedActivity", 30f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.Normal, tier);
        }

        [Test]
        public void GetCurrentTier_Activity69_ReturnsNormal()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 69);
            SetPrivateField("_accumulatedActivity", 69f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.Normal, tier);
        }

        [Test]
        public void GetCurrentTier_Activity70_ReturnsHigh()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 70);
            SetPrivateField("_accumulatedActivity", 70f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.High, tier);
        }

        [Test]
        public void GetCurrentTier_Activity100_ReturnsHigh()
        {
            // Arrange
            SetPrivateField("_displayedActivity", 100);
            SetPrivateField("_accumulatedActivity", 100f);

            // Act
            var tier = _activityMonitor.GetCurrentTier();

            // Assert
            Assert.AreEqual(ActivityMonitorModule.ActivityTier.High, tier);
        }

        #endregion

        #region NoOperation Equilibrium Tests

        [Test]
        public void NoOperation_Equilibrium_OscillatesAround13()
        {
            // 衰减因子0.85，基础值2，无操作时equilibrium = 2/(1-0.85) ≈ 13.33
            // 30分钟无操作后应该振荡在10-20范围内

            // Act - 模拟30分钟无操作
            for (int i = 0; i < 30; i++)
            {
                _activityMonitor.Tick(60f);
            }

            // Assert - 活跃度应该在10-20范围内
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.IsTrue(activity >= 10 && activity <= 20,
                $"Expected activity between 10-20 after 30 minutes with no operations, but got {activity}");
        }

        [Test]
        public void NoOperation_LongTerm_ConvergesToEquilibrium()
        {
            // 长时间无操作应该收敛到equilibrium ≈ 13

            // Act - 模拟100分钟无操作
            for (int i = 0; i < 100; i++)
            {
                _activityMonitor.Tick(60f);
            }

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.IsTrue(activity >= 12 && activity <= 15,
                $"Expected activity between 12-15 after 100 minutes, but got {activity}");
        }

        #endregion

        #region Minute Settlement Tests

        [Test]
        public void MinuteSettlement_With5ValidOps_Returns12()
        {
            // 衰减=0.85, 操作数=5, 系数=2, 基础值=2
            // decayed = 0 * 0.85 = 0
            // increment = 5 * 2 = 10
            // total = 0 + 10 + 2 = 12

            // Act - 模拟5次有效操作，然后Tick触发分钟结算
            for (int i = 0; i < 5; i++)
            {
                _activityMonitor.SimulateInputForTest(65); // 'A' key
            }
            _activityMonitor.Tick(60f); // 触发分钟结算

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(12, activity, $"Expected activity 12 after 5 valid ops, but got {activity}");
        }

        [Test]
        public void MinuteSettlement_With1ValidOp_Returns4()
        {
            // 1次有效操作:
            // decayed = 0 * 0.85 = 0
            // increment = 1 * 2 = 2
            // total = 0 + 2 + 2 = 4

            // Act
            _activityMonitor.SimulateInputForTest(65);
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(4, activity, $"Expected activity 4 after 1 valid op, but got {activity}");
        }

        [Test]
        public void MinuteSettlement_With0ValidOp_Returns2()
        {
            // 0次有效操作:
            // decayed = 0 * 0.85 = 0
            // increment = 0 * 2 = 0
            // total = 0 + 0 + 2 = 2

            // Act - 不调用SimulateInputForTest，直接Tick
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(2, activity, $"Expected activity 2 with no operations, but got {activity}");
        }

        #endregion

        #region Consecutive Same Key Limit Tests

        [Test]
        public void ConsecutiveSameKey_Limit4_Returns10()
        {
            // 连续相同键位限制为4次
            // 前4次有效，第5次无效
            // 有效操作数 = 4
            // decayed = 0, increment = 4*2 = 8, base = 2
            // total = 10

            // Act - 模拟5次相同的'A'键
            _activityMonitor.SimulateInputForTest(65); // 1st - valid
            _activityMonitor.SimulateInputForTest(65); // 2nd - valid
            _activityMonitor.SimulateInputForTest(65); // 3rd - valid
            _activityMonitor.SimulateInputForTest(65); // 4th - valid
            _activityMonitor.SimulateInputForTest(65); // 5th - INVALID (exceeds limit)
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(10, activity, $"Expected activity 10 (4 valid ops), but got {activity}");
        }

        [Test]
        public void ConsecutiveSameKey_Exactly4_Returns10()
        {
            // 正好4次相同键位，全部有效
            // decayed = 0, increment = 4*2 = 8, base = 2
            // total = 10

            // Act
            _activityMonitor.SimulateInputForTest(65); // 1st
            _activityMonitor.SimulateInputForTest(65); // 2nd
            _activityMonitor.SimulateInputForTest(65); // 3rd
            _activityMonitor.SimulateInputForTest(65); // 4th
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(10, activity, $"Expected activity 10, but got {activity}");
        }

        #endregion

        #region Key Interrupt Tests

        [Test]
        public void KeyInterrupt_ResetsStreak_Returns12()
        {
            // A, A, B, A, A = 5次有效操作（不同键位重置计数）
            // decayed = 0, increment = 5*2 = 10, base = 2
            // total = 12

            // Act
            _activityMonitor.SimulateInputForTest(65);  // 'A' key - 1st
            _activityMonitor.SimulateInputForTest(65);  // 'A' key - 2nd
            _activityMonitor.SimulateInputForTest(66);  // 'B' key - interrupts, resets A streak, counts as 1
            _activityMonitor.SimulateInputForTest(65);  // 'A' key - new streak, 1st
            _activityMonitor.SimulateInputForTest(65);  // 'A' key - 2nd
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(12, activity, $"Expected activity 12 after key interrupt, but got {activity}");
        }

        [Test]
        public void KeyInterrupt_ThenExceedLimit_StopsAt4()
        {
            // 第一分钟: A, A, B, A, A = 5次有效
            // 第二分钟: A, A, A, A, A (5th invalid)
            // 第一分钟结果: 12
            // 第二分钟: decayed = 12*0.85 = 10, valid = 4, increment = 8, base = 2
            // 第二分钟结果: 20

            // Act - 第一分钟
            _activityMonitor.SimulateInputForTest(65);  // A-1
            _activityMonitor.SimulateInputForTest(65);  // A-2
            _activityMonitor.SimulateInputForTest(66);  // B
            _activityMonitor.SimulateInputForTest(65);  // A-1
            _activityMonitor.SimulateInputForTest(65);  // A-2
            _activityMonitor.Tick(60f); // 第一分钟结算: 12

            // Act - 第二分钟（再输入5次A）
            _activityMonitor.SimulateInputForTest(65);  // A-1
            _activityMonitor.SimulateInputForTest(65);  // A-2
            _activityMonitor.SimulateInputForTest(65);  // A-3
            _activityMonitor.SimulateInputForTest(65);  // A-4
            _activityMonitor.SimulateInputForTest(65);  // A-5 (invalid)
            _activityMonitor.Tick(60f); // 第二分钟结算

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(20, activity, $"Expected activity 20, but got {activity}");
        }

        #endregion

        #region Activity Bounds Tests

        [Test]
        public void ActivityBounds_LowerNeverBelowZero()
        {
            // 多次Tick后活跃度应该始终 >= 0

            // Act - 多次Tick
            for (int i = 0; i < 50; i++)
            {
                _activityMonitor.Tick(60f);
            }

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.IsTrue(activity >= 0, $"Activity should never be below 0, but got {activity}");
        }

        [Test]
        public void ActivityBounds_UpperNeverExceeds100()
        {
            // 高活跃度操作应该被上限约束
            // 10次操作 * 3轮应该达到上限100

            // Act - 模拟大量操作来测试上限
            for (int round = 0; round < 3; round++)
            {
                // 每轮10次有效操作（不同键位避免连续限制）
                _activityMonitor.SimulateInputForTest(1);
                _activityMonitor.SimulateInputForTest(2);
                _activityMonitor.SimulateInputForTest(3);
                _activityMonitor.SimulateInputForTest(4);
                _activityMonitor.SimulateInputForTest(5);
                _activityMonitor.SimulateInputForTest(6);
                _activityMonitor.SimulateInputForTest(7);
                _activityMonitor.SimulateInputForTest(8);
                _activityMonitor.SimulateInputForTest(9);
                _activityMonitor.SimulateInputForTest(10);
                _activityMonitor.Tick(60f);
            }

            // Assert - 活跃度不应该超过100
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.IsTrue(activity <= 100, $"Activity should never exceed 100, but got {activity}");
        }

        [Test]
        public void ActivityBounds_HitCapStaysAt100()
        {
            // 直接设置高活跃度并验证不会超出上限

            // Arrange - 设置活跃度到接近上限
            SetPrivateField("_accumulatedActivity", 99f);
            SetPrivateField("_displayedActivity", 99);
            SetPrivateField("_validOperationCount", 10); // 大量操作

            // Act - Tick应该触发结算并应用上限
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(100, activity, $"Activity should be clamped to 100, but got {activity}");
        }

        #endregion

        #region Mouse And Scroll Operation Tests

        [Test]
        public void MouseAndScroll_OperateCorrectly_Returns10()
        {
            // 鼠标和滚轮操作应该被正确计数
            // OP_MOUSE_LEFT = -1
            // OP_MOUSE_RIGHT = -2
            // OP_MOUSE_MIDDLE = -3
            // OP_SCROLL = -4

            // Act - 4种不同操作
            _activityMonitor.SimulateInputForTest(-1); // MOUSE_LEFT
            _activityMonitor.SimulateInputForTest(-2); // MOUSE_RIGHT
            _activityMonitor.SimulateInputForTest(-3); // MOUSE_MIDDLE
            _activityMonitor.SimulateInputForTest(-4); // SCROLL
            _activityMonitor.Tick(60f);

            // Assert - 4次有效操作: decayed=0, increment=4*2=8, base=2, total=10
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(10, activity, $"Expected activity 10 after mouse/scroll ops, but got {activity}");
        }

        [Test]
        public void MouseLeft_Click_ValidOperation()
        {
            // Act
            _activityMonitor.SimulateInputForTest(-1); // MOUSE_LEFT
            _activityMonitor.Tick(60f);

            // Assert - 1次有效操作: 0 + 2 + 2 = 4
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(4, activity);
        }

        [Test]
        public void MouseRight_Click_ValidOperation()
        {
            // Act
            _activityMonitor.SimulateInputForTest(-2); // MOUSE_RIGHT
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(4, activity);
        }

        [Test]
        public void MouseMiddle_Click_ValidOperation()
        {
            // Act
            _activityMonitor.SimulateInputForTest(-3); // MOUSE_MIDDLE
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(4, activity);
        }

        [Test]
        public void Scroll_ValidOperation()
        {
            // Act
            _activityMonitor.SimulateInputForTest(-4); // SCROLL
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(4, activity);
        }

        #endregion

        #region Decay Applied Tests

        [Test]
        public void DecayApplied_PreviousValueDecays()
        {
            // 第一分钟: 12
            // 第二分钟: decayed = 12*0.85 = 10, no new ops, base = 2
            // 结果 = 10 + 0 + 2 = 12

            // Arrange - 先设置初始活跃度
            SetPrivateField("_accumulatedActivity", 12f);
            SetPrivateField("_displayedActivity", 12);

            // Act
            _activityMonitor.Tick(60f);

            // Assert - 12 * 0.85 = 10.2 -> 10, + 0 + 2 = 12
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(12, activity, $"Expected 12 after decay, but got {activity}");
        }

        [Test]
        public void DecayApplied_WithNewOperations_Accumulates()
        {
            // 初始值50，第一分钟: 3次有效操作
            // decayed = 50 * 0.85 = 42
            // increment = 3 * 2 = 6
            // total = 42 + 6 + 2 = 50

            // Arrange
            SetPrivateField("_accumulatedActivity", 50f);
            SetPrivateField("_displayedActivity", 50);

            // Act
            _activityMonitor.SimulateInputForTest(65);
            _activityMonitor.SimulateInputForTest(66);
            _activityMonitor.SimulateInputForTest(67);
            _activityMonitor.Tick(60f);

            // Assert
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(50, activity, $"Expected 50, but got {activity}");
        }

        #endregion

        #region Inactive Module Tests

        [Test]
        public void InactiveModule_IgnoresOperations()
        {
            // Arrange - 模块未激活
            SetPrivateField("_isActive", false);

            // Act
            _activityMonitor.SimulateInputForTest(65);
            _activityMonitor.SimulateInputForTest(65);
            _activityMonitor.SimulateInputForTest(65);
            _activityMonitor.Tick(60f);

            // Assert - 未激活时操作被忽略，只有基础值2
            int activity = _activityMonitor.GetCurrentActivity();
            Assert.AreEqual(2, activity, $"Expected 2 (operations ignored when inactive), but got {activity}");
        }

        #endregion

        #region Peak Activity Tests

        [Test]
        public void PeakActivity_TracksHighestValue()
        {
            // Act - 达到高活跃度
            SetPrivateField("_accumulatedActivity", 50f);
            SetPrivateField("_displayedActivity", 50);
            _activityMonitor.Tick(60f);

            // 再降低
            _activityMonitor.Tick(60f);
            _activityMonitor.Tick(60f);

            // Assert - 峰值应该仍然是50
            int peak = _activityMonitor.GetPeakActivity();
            Assert.AreEqual(50, peak, $"Expected peak 50, but got {peak}");
        }

        #endregion
    }
}