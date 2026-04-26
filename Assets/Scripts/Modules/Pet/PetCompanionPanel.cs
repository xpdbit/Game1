using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// 随队宠物面板 - 作为游戏封面常年显示在角落
    /// 继承BaseUIPanel以支持UIManager状态管理
    /// </summary>
    public class PetCompanionPanel : BaseUIPanel
    {
        public override string panelId => "PetCompanionPanel";

        [Header("宠物动画控制器")]
        public PetAnimationController animationController;

        [Header("宠物状态指示器")]
        public Image moodIndicator;          // 心情指示器
        public Image stateIcon;             // 状态图标
        public UIText moodText;             // 心情文字

        [Header("玩家状态同步")]
        public bool syncWithPlayerHP = true;
        public bool syncWithTravelProgress = true;

        [Header("显示配置")]
        public bool showMoodIndicator = true;
        public bool showStateIcon = true;
        public float updateInterval = 0.5f; // UI更新频率

        // 私有变量
        private PetCompanionModule _petModule;
        private PlayerActor _player;
        private float _updateTimer = 0f;
        private PetState _lastState = PetState.Idle;

        #region Unity Lifecycle

        private void Awake()
        {
            // 尝试获取或创建动画控制器
            if (animationController == null)
            {
                animationController = GetComponent<PetAnimationController>();
                if (animationController == null)
                {
                    animationController = gameObject.AddComponent<PetAnimationController>();
                }
            }
        }

        private void Start()
        {
            // 初始化
            Initialize();
        }

        private void Update()
        {
            if (!isOpen) return;

            // 更新计时器
            _updateTimer += Time.deltaTime;
            if (_updateTimer < updateInterval) return;
            _updateTimer = 0f;

            // 更新UI
            UpdateUI();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            // 订阅宠物状态变化事件
            if (_petModule != null)
            {
                _petModule.onStateChanged += OnPetStateChanged;
                _petModule.onMoodChanged += OnMoodChanged;
            }
        }

        public override void OnClose()
        {
            base.OnClose();

            // 取消订阅
            if (_petModule != null)
            {
                _petModule.onStateChanged -= OnPetStateChanged;
                _petModule.onMoodChanged -= OnMoodChanged;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 初始化宠物面板
        /// </summary>
        public void Initialize()
        {
            // 获取玩家数据
            _player = GameMain.instance?.GetPlayerActor();

            // 获取宠物模块
            if (_player != null)
            {
                _petModule = _player.modules.GetModule<PetCompanionModule>();
                if (_petModule == null)
                {
                    // 如果模块不存在，创建一个
                    _petModule = new PetCompanionModule();
                    _petModule.Initialize(_player);
                    _player.AddModule(_petModule);
                }
            }

            // 初始化动画控制器
            if (animationController != null)
            {
                animationController.Initialize(_petModule);
            }

            Debug.Log("[PetCompanionPanel] 宠物面板初始化完成");
        }

        /// <summary>
        /// 刷新UI显示
        /// </summary>
        public void UpdateUI()
        {
            if (_petModule == null) return;

            // 更新状态图标
            UpdateStateIcon();

            // 更新心情指示器
            UpdateMoodIndicator();

            // 更新心情文字
            UpdateMoodText();
        }

        /// <summary>
        /// 手动触发宠物状态
        /// </summary>
        public void TriggerPetState(PetState state)
        {
            if (_petModule == null) return;

            switch (state)
            {
                case PetState.Idle:
                    _petModule.TriggerIdle();
                    break;
                case PetState.Happy:
                    _petModule.TriggerHappy();
                    break;
                case PetState.Sad:
                    _petModule.TriggerSad();
                    break;
                case PetState.Excited:
                    _petModule.TriggerExcited();
                    break;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 宠物状态变化回调
        /// </summary>
        private void OnPetStateChanged(PetState newState)
        {
            if (newState == _lastState) return;
            _lastState = newState;

            // 通知动画控制器切换动画
            if (animationController != null)
            {
                animationController.PlayState(newState);
            }

            // 播放状态切换音效（预留）
            // AudioManager.PlayPetStateSound(newState);

            Debug.Log($"[PetCompanionPanel] 宠物状态变为: {newState}");
        }

        /// <summary>
        /// 心情变化回调
        /// </summary>
        private void OnMoodChanged(PetMood mood)
        {
            // 节流更新UI
            UpdateMoodIndicator();
            UpdateMoodText();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 更新状态图标
        /// </summary>
        private void UpdateStateIcon()
        {
            if (stateIcon == null || !showStateIcon) return;

            if (_petModule == null) return;

            // 根据状态切换图标
            // 注意：实际项目中应该使用实际的Sprite资源
            // 这里使用颜色来区分状态作为占位
            Color iconColor = GetStateColor(_petModule.GetCurrentState());
            stateIcon.color = iconColor;
        }

        /// <summary>
        /// 更新心情指示器
        /// </summary>
        private void UpdateMoodIndicator()
        {
            if (moodIndicator == null || !showMoodIndicator) return;

            if (_petModule == null) return;

            var mood = _petModule.GetMood();

            // 使用心情值来设置指示器的显示
            // 这里使用单一颜色表示整体心情倾向
            float avgMood = (mood.happiness + (1f - mood.sadness) + mood.excitement) / 3f;
            moodIndicator.fillAmount = avgMood;
        }

        /// <summary>
        /// 更新心情文字
        /// </summary>
        private void UpdateMoodText()
        {
            if (moodText == null) return;

            if (_petModule == null)
            {
                moodText.text = "??";
                return;
            }

            var mood = _petModule.GetMood();
            PetState state = mood.GetState();

            // 根据状态显示对应文字
            string stateText = state switch
            {
                PetState.Idle => "悠闲",
                PetState.Happy => "开心",
                PetState.Sad => "难过",
                PetState.Excited => "兴奋",
                _ => "未知"
            };

            moodText.text = stateText;
        }

        /// <summary>
        /// 获取状态对应的颜色
        /// </summary>
        private Color GetStateColor(PetState state)
        {
            return state switch
            {
                PetState.Idle => new Color(0.7f, 0.7f, 0.7f),      // 灰色
                PetState.Happy => new Color(0.3f, 0.9f, 0.3f),     // 绿色
                PetState.Sad => new Color(0.3f, 0.5f, 0.9f),       // 蓝色
                PetState.Excited => new Color(0.9f, 0.6f, 0.2f),   // 橙色
                _ => Color.white
            };
        }

        #endregion
    }

    /// <summary>
    /// 宠物动画控制器 - 处理2D骨骼动画状态切换
    /// 支持四种状态: Idle, Happy, Sad, Excited
    /// </summary>
    public class PetAnimationController : MonoBehaviour
    {
        [Header("动画配置")]
        public PetState currentState = PetState.Idle;

        [Header("骨骼节点")]
        public Transform body;          // 身体
        public Transform head;         // 头部
        public Transform tail;         // 尾巴
        public Transform leftEar;      // 左耳
        public Transform rightEar;     // 右耳
        public Transform eyeLeft;      // 左眼
        public Transform eyeRight;     // 右眼
        public Transform mouth;        // 嘴巴

        [Header("动画参数")]
        public float idleBobSpeed = 1f;        // Idle状态上下浮动速度
        public float idleBobHeight = 5f;        // Idle状态浮动高度
        public float tailWagSpeed = 3f;        // 尾巴摇摆速度
        public float earFlopSpeed = 2f;        // 耳朵 flop 速度
        public float stateTransitionSpeed = 5f; // 状态切换速度

        // 私有变量
        private PetCompanionModule _petModule;
        private PetState _targetState = PetState.Idle;
        private float _stateBlend = 0f;
        private Vector3 _originalBodyPos;
        private Vector3 _originalHeadPos;
        private float _animTime = 0f;

        // 表情状态
        private float _eyeOpenness = 1f;   // 眼睛睁开程度
        private float _mouthOpenness = 0f; // 嘴巴张开程度
        private float _tailWag = 0f;
        private float _earDroop = 0f;

        #region Unity Lifecycle

        private void Awake()
        {
            // 记录原始位置
            if (body != null) _originalBodyPos = body.localPosition;
            if (head != null) _originalHeadPos = head.localPosition;
        }

        private void Update()
        {
            _animTime += Time.deltaTime;

            // 平滑过渡到目标状态
            _stateBlend = Mathf.Lerp(_stateBlend, 1f, Time.deltaTime * stateTransitionSpeed);

            // 根据当前状态更新动画
            UpdateAnimation();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 初始化动画控制器
        /// </summary>
        public void Initialize(PetCompanionModule petModule)
        {
            _petModule = petModule;

            // 订阅状态变化事件
            if (_petModule != null)
            {
                _petModule.onStateChanged += OnStateChanged;
                PlayState(_petModule.GetCurrentState());
            }
        }

        /// <summary>
        /// 播放指定状态动画
        /// </summary>
        public void PlayState(PetState state)
        {
            if (currentState == state) return;

            currentState = state;
            _targetState = state;
            _stateBlend = 0f;

            // 根据状态设置表情参数
            ApplyExpression(state);
        }

        /// <summary>
        /// 立即切换到指定状态（不带过渡）
        /// </summary>
        public void SetStateImmediate(PetState state)
        {
            currentState = state;
            _targetState = state;
            _stateBlend = 1f;
            ApplyExpression(state);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 状态变化回调
        /// </summary>
        private void OnStateChanged(PetState newState)
        {
            PlayState(newState);
        }

        /// <summary>
        /// 根据状态应用表情
        /// </summary>
        private void ApplyExpression(PetState state)
        {
            switch (state)
            {
                case PetState.Idle:
                    _eyeOpenness = 1f;
                    _mouthOpenness = 0f;
                    _tailWag = 0.3f;
                    _earDroop = 0f;
                    break;

                case PetState.Happy:
                    _eyeOpenness = 0.8f;
                    _mouthOpenness = 0.6f;
                    _tailWag = 1f;
                    _earDroop = 0f;
                    break;

                case PetState.Sad:
                    _eyeOpenness = 0.6f;
                    _mouthOpenness = 0.2f;
                    _tailWag = 0f;
                    _earDroop = 0.5f;
                    break;

                case PetState.Excited:
                    _eyeOpenness = 1f;
                    _mouthOpenness = 1f;
                    _tailWag = 1f;
                    _earDroop = 0f;
                    break;
            }
        }

        /// <summary>
        /// 更新动画
        /// </summary>
        private void UpdateAnimation()
        {
            switch (currentState)
            {
                case PetState.Idle:
                    UpdateIdleAnimation();
                    break;
                case PetState.Happy:
                    UpdateHappyAnimation();
                    break;
                case PetState.Sad:
                    UpdateSadAnimation();
                    break;
                case PetState.Excited:
                    UpdateExcitedAnimation();
                    break;
            }

            // 更新骨骼变换
            UpdateBones();
        }

        /// <summary>
        /// Idle状态动画 - 轻微上下浮动 + 尾巴慢摇
        /// </summary>
        private void UpdateIdleAnimation()
        {
            // 身体轻微上下浮动
            float bob = Mathf.Sin(_animTime * idleBobSpeed) * idleBobHeight;
            if (body != null)
            {
                body.localPosition = _originalBodyPos + new Vector3(0, bob, 0);
            }

            // 尾巴轻微摇摆
            _tailWag = 0.3f;
            float tailAngle = Mathf.Sin(_animTime * tailWagSpeed) * 15f;
            if (tail != null)
            {
                tail.localRotation = Quaternion.Euler(0, 0, tailAngle);
            }

            // 耳朵正常
            if (leftEar != null)
                leftEar.localRotation = Quaternion.Euler(0, 0, 0);
            if (rightEar != null)
                rightEar.localRotation = Quaternion.Euler(0, 0, 0);
        }

        /// <summary>
        /// Happy状态动画 - 尾巴快摇 + 耳朵上扬
        /// </summary>
        private void UpdateHappyAnimation()
        {
            // 身体轻微弹跳
            float bounce = Mathf.Abs(Mathf.Sin(_animTime * 6f)) * 8f;
            if (body != null)
            {
                body.localPosition = _originalBodyPos + new Vector3(0, bounce, 0);
            }

            // 尾巴快速摇摆
            float tailAngle = Mathf.Sin(_animTime * tailWagSpeed * 2f) * 30f;
            if (tail != null)
            {
                tail.localRotation = Quaternion.Euler(0, 0, tailAngle);
            }

            // 耳朵上扬
            if (leftEar != null)
                leftEar.localRotation = Quaternion.Euler(0, 0, -10f);
            if (rightEar != null)
                rightEar.localRotation = Quaternion.Euler(0, 0, 10f);
        }

        /// <summary>
        /// Sad状态动画 - 耳朵下垂 + 身体下沉
        /// </summary>
        private void UpdateSadAnimation()
        {
            // 身体下沉
            if (body != null)
            {
                body.localPosition = _originalBodyPos + new Vector3(0, -3f, 0);
            }

            // 尾巴下垂
            if (tail != null)
            {
                tail.localRotation = Quaternion.Euler(0, 0, -20f);
            }

            // 耳朵下垂
            if (leftEar != null)
                leftEar.localRotation = Quaternion.Euler(0, 0, 20f);
            if (rightEar != null)
                rightEar.localRotation = Quaternion.Euler(0, 0, -20f);
        }

        /// <summary>
        /// Excited状态动画 - 全身快速运动 + 跳跃
        /// </summary>
        private void UpdateExcitedAnimation()
        {
            // 身体快速上下跳动
            float bounce = Mathf.Abs(Mathf.Sin(_animTime * 8f)) * 15f;
            if (body != null)
            {
                body.localPosition = _originalBodyPos + new Vector3(0, bounce, 0);
            }

            // 尾巴疯狂摇摆
            float tailAngle = Mathf.Sin(_animTime * tailWagSpeed * 3f) * 45f;
            if (tail != null)
            {
                tail.localRotation = Quaternion.Euler(0, 0, tailAngle);
            }

            // 耳朵快速抖动
            float earShake = Mathf.Sin(_animTime * earFlopSpeed * 2f) * 15f;
            if (leftEar != null)
                leftEar.localRotation = Quaternion.Euler(0, earShake, -5f);
            if (rightEar != null)
                rightEar.localRotation = Quaternion.Euler(0, -earShake, 5f);
        }

        /// <summary>
        /// 更新骨骼变换（表情）
        /// </summary>
        private void UpdateBones()
        {
            // 眼睛 - 根据眼睛睁开程度
            if (eyeLeft != null)
            {
                float scaleY = Mathf.Lerp(0.3f, 1f, _eyeOpenness);
                eyeLeft.localScale = new Vector3(1, scaleY, 1);
            }
            if (eyeRight != null)
            {
                float scaleY = Mathf.Lerp(0.3f, 1f, _eyeOpenness);
                eyeRight.localScale = new Vector3(1, scaleY, 1);
            }

            // 嘴巴 - 根据嘴巴张开程度
            if (mouth != null)
            {
                float scaleY = Mathf.Lerp(0.2f, 1f, _mouthOpenness);
                mouth.localScale = new Vector3(1, scaleY, 1);
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// 在Scene视图中绘制骨骼连接（调试用）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.yellow;

            // 绘制骨骼连接线
            if (body != null && head != null)
            {
                Gizmos.DrawLine(body.position, head.position);
            }
            if (tail != null && body != null)
            {
                Gizmos.DrawLine(body.position, tail.position);
            }
            if (leftEar != null && head != null)
            {
                Gizmos.DrawLine(head.position, leftEar.position);
            }
            if (rightEar != null && head != null)
            {
                Gizmos.DrawLine(head.position, rightEar.position);
            }
        }

        #endregion
    }
}