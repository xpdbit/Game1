using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 暴击特效数据
    /// </summary>
    [Serializable]
    public class CritEffectData
    {
        public Vector3 worldPosition;
        public bool isPlayerCrit;
        public float scale;
        public float duration;

        public static CritEffectData Default => new CritEffectData
        {
            scale = 1f,
            duration = 0.5f
        };
    }

    /// <summary>
    /// 暴击特效项
    /// </summary>
    public class CritEffectItem
    {
        public GameObject go;
        public ParticleSystem particles;
        public float lifetime;
        public float maxLifetime;

        public void Update(float deltaTime)
        {
            if (go == null || !go.activeSelf) return;

            lifetime -= deltaTime;
            if (lifetime <= 0f)
            {
                go.SetActive(false);
                return;
            }

            // 粒子系统自行管理，这里只处理生命周期
        }

        public bool IsAlive => go != null && go.activeSelf && lifetime > 0f;
    }

    /// <summary>
    /// 暴击特效管理器
    /// 提供暴击时的粒子爆发效果
    /// </summary>
    public class CritEffectManager
    {
        #region Singleton
        private static CritEffectManager _instance;
        public static CritEffectManager instance => _instance ??= new CritEffectManager();
        #endregion

        private const int POOL_SIZE = 10;
        private const string CRIT_PREFAB_PATH = "Effects/Prefabs/CritEffect";

        private Queue<CritEffectItem> _effectPool = new();
        private List<CritEffectItem> _activeEffects = new();
        private GameObject _poolContainer;
        private GameObject _critPrefab;
        private Camera _worldCamera;

        public void Initialize(Transform parent, Camera worldCamera)
        {
            _worldCamera = worldCamera ?? Camera.main;

            var go = new GameObject("[CritEffectPool]");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            _poolContainer = go;

            CreatePool();
            Debug.Log("[CritEffectManager] Initialized with pool size: " + POOL_SIZE);
        }

        private void CreatePool()
        {
            _critPrefab = Resources.Load<GameObject>(CRIT_PREFAB_PATH);

            if (_critPrefab == null)
            {
                Debug.LogWarning("[CritEffectManager] Crit prefab not found, creating default");
                _critPrefab = CreateDefaultCritPrefab();
            }

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var item = CreateEffectItem();
                item.go.SetActive(false);
                _effectPool.Enqueue(item);
            }
        }

        private CritEffectItem CreateEffectItem()
        {
            var go = UnityEngine.Object.Instantiate(_critPrefab, _poolContainer.transform);
            go.name = "CritEffect_" + _effectPool.Count;

            var particles = go.GetComponent<ParticleSystem>();
            if (particles == null)
            {
                particles = go.AddComponent<ParticleSystem>();
                ConfigureDefaultParticleSystem(particles);
            }

            return new CritEffectItem
            {
                go = go,
                particles = particles,
                lifetime = 0f,
                maxLifetime = 0.5f
            };
        }

        private void ConfigureDefaultParticleSystem(ParticleSystem ps)
        {
            // 主模块
            var main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 100f;
            main.startSize = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.8f, 0.2f, 1f),
                new Color(1f, 0.2f, 0.2f, 1f)
            );

            // 发射模块
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst { time = 0f, count = 20, repeatInterval = 30, probability = 1f };
            emission.SetBursts(new ParticleSystem.Burst[] { burst });

            // 形状模块
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            shape.arc = 360f;

            // 速度模块
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = 50f;

            // 大小模块
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // 颜色模块
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 0.5f, 0f, 0f)
            );
        }

        private GameObject CreateDefaultCritPrefab()
        {
            var go = new GameObject("CritEffectPrefab");
            go.AddComponent<ParticleSystem>();
            return go;
        }

        /// <summary>
        /// 播放暴击特效
        /// </summary>
        public void PlayCritEffect(CritEffectData data)
        {
            CritEffectItem effect;
            if (_effectPool.Count > 0)
            {
                effect = _effectPool.Dequeue();
            }
            else
            {
                effect = CreateEffectItem();
            }

            effect.go.transform.position = data.worldPosition;
            effect.go.transform.localScale = Vector3.one * data.scale;
            effect.lifetime = data.duration;
            effect.maxLifetime = data.duration;

            var ps = effect.particles;
            if (ps != null)
            {
                // 根据是玩家暴击还是敌人暴击调整颜色
                var main = ps.main;
                if (data.isPlayerCrit)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(1f, 0.9f, 0.3f, 1f),
                        new Color(1f, 0.3f, 0.1f, 1f)
                    );
                }
                else
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(0.5f, 0.5f, 1f, 1f),
                        new Color(0.8f, 0.2f, 0.2f, 1f)
                    );
                }

                ps.Play();
            }

            effect.go.SetActive(true);
            _activeEffects.Add(effect);

            Debug.Log($"[CritEffectManager] PlayCritEffect at {data.worldPosition}, isPlayerCrit={data.isPlayerCrit}");
        }

        /// <summary>
        /// 从战斗事件播放暴击特效
        /// </summary>
        public void PlayFromCombatEvent(CombatAnimationEvent e)
        {
            var data = new CritEffectData
            {
                worldPosition = e.worldPosition != default ? e.worldPosition : Vector3.zero,
                isPlayerCrit = e.eventType == CombatAnimationEventType.PlayerCrit,
                scale = e.isCritical ? 1.5f : 1f,
                duration = 0.5f
            };

            PlayCritEffect(data);
        }

        public void Update(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Update(deltaTime);

                if (!effect.IsAlive)
                {
                    _activeEffects.RemoveAt(i);
                    effect.go.SetActive(false);
                    _effectPool.Enqueue(effect);
                }
            }
        }

        public int GetActiveCount() => _activeEffects.Count;

        public void ClearAll()
        {
            foreach (var effect in _activeEffects)
            {
                effect.go.SetActive(false);
                _effectPool.Enqueue(effect);
            }
            _activeEffects.Clear();
        }
    }

    /// <summary>
    /// 战斗特效组件
    /// 挂载到战斗场景的Canvas或特效层
    /// 管理暴击特效和屏幕震动
    /// </summary>
    public class CombatEffects : MonoBehaviour
    {
        [Header("Crit Effect Settings")]
        [SerializeField] private bool _enableCritEffect = true;
        [SerializeField] private float _critScale = 1.5f;
        [SerializeField] private float _critDuration = 0.5f;

        [Header("Screen Shake Settings")]
        [SerializeField] private bool _enableScreenShake = true;
        [SerializeField] private float _shakeDuration = 0.2f;
        [SerializeField] private float _shakeIntensity = 0.3f;

        [Header("References")]
        [SerializeField] private Camera _worldCamera;

        private Vector3 _originalCameraPos;
        private float _shakeTimer = 0f;
        private bool _isShaking = false;

        private void Awake()
        {
            if (_worldCamera == null)
                _worldCamera = Camera.main;

            _originalCameraPos = _worldCamera != null ? _worldCamera.transform.position : Vector3.zero;
        }

        private void OnEnable()
        {
            CombatAnimationDispatcher.instance.onCritEffect += OnCritEffect;
            CombatAnimationDispatcher.instance.onDeathAnimation += OnDeathAnimation;
        }

        private void OnDisable()
        {
            CombatAnimationDispatcher.instance.onCritEffect -= OnCritEffect;
            CombatAnimationDispatcher.instance.onDeathAnimation -= OnDeathAnimation;
        }

        private void Update()
        {
            UpdateScreenShake();
            CritEffectManager.instance.Update(Time.deltaTime);
        }

        private void OnCritEffect(CombatAnimationEvent e)
        {
            if (!_enableCritEffect) return;

            var data = new CritEffectData
            {
                worldPosition = e.worldPosition,
                isPlayerCrit = e.eventType == CombatAnimationEventType.PlayerCrit,
                scale = _critScale,
                duration = _critDuration
            };

            CritEffectManager.instance.PlayCritEffect(data);

            // 触发屏幕震动
            if (_enableScreenShake && e.isCritical)
            {
                TriggerScreenShake();
            }
        }

        private void OnDeathAnimation(CombatAnimationEvent e)
        {
            // 死亡时也可以触发特殊特效
            Debug.Log($"[CombatEffects] Death animation triggered for {e.defenderName}");
        }

        /// <summary>
        /// 触发屏幕震动
        /// </summary>
        public void TriggerScreenShake()
        {
            if (_worldCamera == null) return;

            _isShaking = true;
            _shakeTimer = _shakeDuration;
            Debug.Log("[CombatEffects] Screen shake triggered");
        }

        private void UpdateScreenShake()
        {
            if (!_isShaking || _worldCamera == null) return;

            _shakeTimer -= Time.deltaTime;
            if (_shakeTimer <= 0f)
            {
                _worldCamera.transform.position = _originalCameraPos;
                _isShaking = false;
                return;
            }

            float progress = _shakeTimer / _shakeDuration;
            float intensity = _shakeIntensity * progress;

            float x = UnityEngine.Random.Range(-intensity, intensity);
            float y = UnityEngine.Random.Range(-intensity, intensity);

            _worldCamera.transform.position = _originalCameraPos + new Vector3(x, y, 0);
        }

        /// <summary>
        /// 初始化特效系统
        /// </summary>
        public void Initialize()
        {
            CritEffectManager.instance.Initialize(this.transform, _worldCamera);
        }
    }
}