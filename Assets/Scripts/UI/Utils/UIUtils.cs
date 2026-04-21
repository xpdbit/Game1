using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game1.UI.Utils
{
    /// <summary>
    /// 任务循环进度 - 完整实现
    /// </summary>
    public class XUniTaskLoopProgress
    {
        public Func<float, bool> loopCondition { get; private set; }
        public Func<float, float> getDeltaTime { get; private set; }
        public Action<float> loopAction { get; private set; }

        private bool _isRunning = false;
        private float _accumulator = 0f;

        public static XUniTaskLoopProgress Create(
            Func<float, bool> loopCondition,
            Func<float, float> getDeltaTime,
            Action<float> loopAction)
        {
            return new XUniTaskLoopProgress
            {
                loopCondition = loopCondition,
                getDeltaTime = getDeltaTime,
                loopAction = loopAction,
                _isRunning = false,
                _accumulator = 0f
            };
        }

        /// <summary>
        /// 执行一帧的循环逻辑
        /// </summary>
        /// <returns>如果循环完成返回true</returns>
        public bool Tick()
        {
            if (!_isRunning)
            {
                _isRunning = true;
            }

            float deltaTime = getDeltaTime?.Invoke(_accumulator) ?? Time.deltaTime;
            _accumulator += deltaTime;

            // 检查循环条件
            if (loopCondition != null && loopCondition(_accumulator))
            {
                return true; // 循环完成
            }

            // 执行循环动作
            loopAction?.Invoke(_accumulator);

            return false;
        }

        /// <summary>
        /// 重置循环状态
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _accumulator = 0f;
        }
    }

    /// <summary>
    /// 任务进度追踪类
    /// </summary>
    public class XUniTaskProgress
    {
        public XUniTaskProgress parent { get; set; }
        public XUniTaskLoopProgress loopProgress { get; set; }

        private List<Action<float>> _endActions = new();
        private bool _isStarted = false;
        private bool _isCompleted = false;

        public XUniTaskProgress() { }

        public XUniTaskProgress GetLast()
        {
            XUniTaskProgress current = this;
            while (current.parent != null && current.parent != current)
            {
                current = current.parent;
            }
            return current;
        }

        public void AddToEnd(float delay, Action<float> action)
        {
            if (delay <= 0f)
            {
                action?.Invoke(0f);
            }
            else
            {
                _endActions.Add(action);
            }
        }

        public void StartAsync()
        {
            if (_isStarted) return;
            _isStarted = true;

            if (loopProgress != null)
            {
                // 启动异步循环 - 使用协程
                XUniTaskRunner.Instance.StartCoroutine(RunLoopCoroutine());
            }
        }

        public void ExecuteImmediatelyAsync()
        {
            if (_isStarted) return;
            _isStarted = true;

            // 立即执行循环（同步模式）
            if (loopProgress != null)
            {
                loopProgress.Reset();
                while (!loopProgress.Tick())
                {
                    // 继续直到完成
                }
            }

            // 执行结束动作
            ExecuteEndActions();
            _isCompleted = true;
        }

        public bool IsCompleted()
        {
            return _isCompleted;
        }

        private System.Collections.IEnumerator RunLoopCoroutine()
        {
            if (loopProgress != null)
            {
                loopProgress.Reset();
                while (!loopProgress.Tick())
                {
                    yield return null;
                }
            }

            ExecuteEndActions();
            _isCompleted = true;
        }

        private void ExecuteEndActions()
        {
            foreach (var action in _endActions)
            {
                action?.Invoke(0f);
            }
            _endActions.Clear();
        }
    }
}

namespace Game1.UI.Utils
{
    /// <summary>
    /// 全局任务运行器（用于协程）
    /// </summary>
    public class XUniTaskRunner : MonoBehaviour
    {
        private static XUniTaskRunner _instance;
        public static XUniTaskRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("XUniTaskRunner");
                    _instance = go.AddComponent<XUniTaskRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}

namespace Game1.UI.Utils
{
    /// <summary>
    /// 对象池
    /// </summary>
    public static class XObjectPool
    {
        private static readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

        public static GameObject Get(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
            {
                var obj = queue.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            return UnityEngine.Object.Instantiate(prefab);
        }

        public static void Release(GameObject obj, GameObject prefab)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }
            _pools[prefab].Enqueue(obj);
        }
    }
}