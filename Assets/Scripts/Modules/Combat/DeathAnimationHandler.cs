using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 死亡动画数据
    /// </summary>
    [Serializable]
    public class DeathAnimationData
    {
        public string entityName;
        public Vector3 worldPosition;
        public bool isPlayer;
        public float delayBeforeDeath;
        public float deathDuration;
        public DeathType deathType;

        public enum DeathType
        {
            Default,
            FadeOut,
            ScaleDown,
            Collapse,
            Dissolve
        }

        public static DeathAnimationData Default => new DeathAnimationData
        {
            delayBeforeDeath = 0.5f,
            deathDuration = 1f,
            deathType = DeathType.Default
        };
    }

    /// <summary>
    /// 死亡动画项
    /// </summary>
    public class DeathAnimationItem
    {
        public GameObject targetGameObject;
        public DeathAnimationData data;
        public float timer;
        public bool started;
        public Vector3 originalPosition;
        public Vector3 originalScale;
        public Renderer[] renderers;
        public Color[] originalColors;

        public void Update(float deltaTime)
        {
            if (targetGameObject == null || !started) return;

            timer += deltaTime;

            if (timer < data.delayBeforeDeath) return;

            float progress = (timer - data.delayBeforeDeath) / data.deathDuration;
            progress = Mathf.Clamp01(progress);

            ApplyDeathAnimation(progress);

            if (progress >= 1f)
            {
                CompleteDeath();
            }
        }

        private void ApplyDeathAnimation(float progress)
        {
            switch (data.deathType)
            {
                case DeathAnimationData.DeathType.FadeOut:
                    FadeOut(progress);
                    break;

                case DeathAnimationData.DeathType.ScaleDown:
                    ScaleDown(progress);
                    break;

                case DeathAnimationData.DeathType.Collapse:
                    Collapse(progress);
                    break;

                case DeathAnimationData.DeathType.Dissolve:
                    Dissolve(progress);
                    break;

                default:
                    ScaleDown(progress);
                    break;
            }
        }

        private void FadeOut(float progress)
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is Renderer r)
                {
                    var color = originalColors[i];
                    color.a = 1f - progress;
                    r.material.color = color;
                }
            }
        }

        private void ScaleDown(float progress)
        {
            if (targetGameObject == null) return;

            float scale = Mathf.Lerp(1f, 0f, progress);
            targetGameObject.transform.localScale = originalScale * scale;
        }

        private void Collapse(float progress)
        {
            if (targetGameObject == null) return;

            // 向地面塌陷
            float collapse = Mathf.Lerp(0f, -2f, progress);
            targetGameObject.transform.position = originalPosition + new Vector3(0, collapse, 0);

            // 同时缩小
            float scale = Mathf.Lerp(1f, 0.3f, progress);
            targetGameObject.transform.localScale = originalScale * scale;
        }

        private void Dissolve(float progress)
        {
            if (renderers == null) return;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i] as Renderer;
                if (r != null && r.material.HasProperty("_Dissolve"))
                {
                    r.material.SetFloat("_Dissolve", progress);
                }
                else
                {
                    // Fallback to alpha fade
                    var color = originalColors[i];
                    color.a = 1f - progress;
                    if (r != null)
                        r.material.color = color;
                }
            }
        }

        private void CompleteDeath()
        {
            if (targetGameObject != null)
            {
                // 隐藏或销毁
                targetGameObject.SetActive(false);
                Debug.Log($"[DeathAnimationHandler] Death animation completed for {data.entityName}");
            }
        }

        public bool IsAlive => targetGameObject != null && started && timer < data.delayBeforeDeath + data.deathDuration;
    }

    /// <summary>
    /// 死亡动画请求
    /// </summary>
    public class DeathAnimationRequest
    {
        public string entityName;
        public GameObject targetGameObject;
        public DeathAnimationData animationData;
        public Action<DeathAnimationRequest> onComplete;

        public DeathAnimationRequest(string name, GameObject target, DeathAnimationData data, Action<DeathAnimationRequest> callback = null)
        {
            entityName = name;
            targetGameObject = target;
            animationData = data;
            onComplete = callback;
        }
    }

    /// <summary>
    /// 死亡动画管理器
    /// 处理战斗中的死亡动画播放
    /// </summary>
    public class DeathAnimationManager
    {
        #region Singleton
        private static DeathAnimationManager _instance;
        public static DeathAnimationManager instance => _instance ??= new DeathAnimationManager();
        #endregion

        private List<DeathAnimationItem> _activeAnimations = new();
        private Queue<DeathAnimationRequest> _pendingRequests = new();
        private bool _isProcessing = false;

        /// <summary>
        /// 请求播放死亡动画
        /// </summary>
        public void RequestDeathAnimation(DeathAnimationRequest request)
        {
            if (request == null || request.targetGameObject == null)
            {
                Debug.LogWarning("[DeathAnimationManager] Invalid death animation request");
                return;
            }

            _pendingRequests.Enqueue(request);
            ProcessNextRequest();
        }

        /// <summary>
        /// 简化请求接口
        /// </summary>
        public void PlayDeathAnimation(string entityName, GameObject target, DeathAnimationData.DeathType deathType = DeathAnimationData.DeathType.Default)
        {
            var data = DeathAnimationData.Default;
            data.entityName = entityName;
            data.worldPosition = target.transform.position;
            data.deathType = deathType;

            RequestDeathAnimation(new DeathAnimationRequest(entityName, target, data));
        }

        /// <summary>
        /// 从战斗事件触发死亡动画
        /// </summary>
        public void PlayFromCombatEvent(CombatAnimationEvent e, GameObject targetObject)
        {
            if (targetObject == null) return;

            var data = DeathAnimationData.Default;
            data.entityName = e.defenderName;
            data.worldPosition = e.worldPosition != default ? e.worldPosition : targetObject.transform.position;
            data.isPlayer = e.eventType == CombatAnimationEventType.PlayerDefeat;

            RequestDeathAnimation(new DeathAnimationRequest(e.defenderName, targetObject, data));
        }

        private void ProcessNextRequest()
        {
            if (_isProcessing || _pendingRequests.Count == 0) return;

            _isProcessing = true;
            var request = _pendingRequests.Dequeue();

            var item = new DeathAnimationItem
            {
                targetGameObject = request.targetGameObject,
                data = request.animationData,
                timer = 0f,
                started = false,
                originalPosition = request.targetGameObject.transform.position,
                originalScale = request.targetGameObject.transform.localScale,
                renderers = request.targetGameObject.GetComponentsInChildren<Renderer>(),
                originalColors = new Color[0]
            };

            // 缓存原始颜色
            if (item.renderers != null && item.renderers.Length > 0)
            {
                item.originalColors = new Color[item.renderers.Length];
                for (int i = 0; i < item.renderers.Length; i++)
                {
                    item.originalColors[i] = item.renderers[i].material.color;
                }
            }

            item.started = true;
            _activeAnimations.Add(item);

            Debug.Log($"[DeathAnimationManager] Started death animation for {request.entityName}");
        }

        public void Update(float deltaTime)
        {
            for (int i = _activeAnimations.Count - 1; i >= 0; i--)
            {
                var item = _activeAnimations[i];
                item.Update(deltaTime);

                if (!item.IsAlive)
                {
                    _activeAnimations.RemoveAt(i);
                    _isProcessing = false;

                    // 处理下一个请求
                    ProcessNextRequest();
                }
            }
        }

        public int GetActiveCount() => _activeAnimations.Count;

        public void ClearAll()
        {
            _activeAnimations.Clear();
            _pendingRequests.Clear();
            _isProcessing = false;
        }

        /// <summary>
        /// 跳过所有正在播放的死亡动画
        /// </summary>
        public void SkipAll()
        {
            foreach (var item in _activeAnimations)
            {
                if (item.targetGameObject != null)
                {
                    item.targetGameObject.SetActive(false);
                }
            }
            _activeAnimations.Clear();
            _isProcessing = false;

            while (_pendingRequests.Count > 0)
            {
                var request = _pendingRequests.Dequeue();
                if (request.targetGameObject != null)
                {
                    request.targetGameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 死亡动画处理器组件
    /// 挂载到战斗场景的管理器GameObject上
    /// </summary>
    public class DeathAnimationHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _autoListenToDispatcher = true;
        [SerializeField] private DeathAnimationData.DeathType _defaultDeathType = DeathAnimationData.DeathType.ScaleDown;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        [SerializeField] private string _testEntityName = "TestEnemy";

        private void OnEnable()
        {
            if (_autoListenToDispatcher)
            {
                CombatAnimationDispatcher.instance.onDeathAnimation += OnDeathAnimationEvent;
            }
        }

        private void OnDisable()
        {
            CombatAnimationDispatcher.instance.onDeathAnimation -= OnDeathAnimationEvent;
        }

        private void Update()
        {
            DeathAnimationManager.instance.Update(Time.deltaTime);
        }

        private void OnDeathAnimationEvent(CombatAnimationEvent e)
        {
            // 尝试查找对应的GameObject
            // 这里通过名字查找，实际项目中可能需要通过其他方式关联
            var targetObject = FindEntityGameObject(e.defenderName);

            if (targetObject != null)
            {
                var data = DeathAnimationData.Default;
                data.entityName = e.defenderName;
                data.worldPosition = e.worldPosition != default ? e.worldPosition : targetObject.transform.position;
                data.deathType = _defaultDeathType;

                DeathAnimationManager.instance.PlayDeathAnimation(e.defenderName, targetObject, _defaultDeathType);
            }
            else
            {
                Debug.LogWarning($"[DeathAnimationHandler] Could not find GameObject for {e.defenderName}");
            }
        }

        /// <summary>
        /// 根据名字查找实体GameObject
        /// 子类可以重写此方法来实现自定义查找逻辑
        /// </summary>
        protected virtual GameObject FindEntityGameObject(string entityName)
        {
            // 默认实现：按名字查找
            var go = GameObject.Find(entityName);
            if (go != null) return go;

            // 尝试在场景中查找包含该名字的对象
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains(entityName))
                {
                    return obj;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Death Animation")]
        private void TestDeathAnimation()
        {
            var testObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObj.name = _testEntityName;
            testObj.transform.position = new Vector3(0, 1, 0);

            DeathAnimationManager.instance.PlayDeathAnimation(_testEntityName, testObj, _defaultDeathType);
        }
#endif
    }
}