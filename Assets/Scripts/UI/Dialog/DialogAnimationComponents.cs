using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game1.UI.Dialog
{
    /// <summary>
    /// 对话框动画类型
    /// </summary>
    public enum DialogAnimationType
    {
        None,
        Typewriter,          // 打字机效果
        FadeIn,             // 淡入
        FadeOut,            // 淡出
        SlideIn,            // 滑入
        SlideOut,           // 滑出
        ScaleIn,            // 缩放淡入
    }

    /// <summary>
    /// 对话框动画数据
    /// </summary>
    [Serializable]
    public class DialogAnimationData
    {
        public DialogAnimationType type = DialogAnimationType.FadeIn;
        public float duration = 0.3f;
        public float delay = 0f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    /// <summary>
    /// 打字机效果组件
    /// 支持 TMP_Text 打字机效果
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("目标文本")]
        [SerializeField] private TMP_Text _targetText;

        [Header("打字机设置")]
        public float charsPerSecond = 50f;        // 每秒字符数
        public float punctuationDelay = 0.5f;     // 标点符号延迟
        public bool skipOnClick = true;          // 点击跳过

        [Header("完成回调")]
        public Action onTypingCompleted;
        public Action onTypingInterrupted;

        private string _fullText = string.Empty;
        private string _displayedText = string.Empty;
        private int _currentCharIndex = 0;
        private float _timer = 0f;
        private bool _isTyping = false;
        private bool _isPaused = false;

        // 标点符号
        private readonly char[] _punctuation = { '。', '！', '？', '，', '、', ';', ':', '"', '\n' };

        private void Update()
        {
            if (!_isTyping || _isPaused) return;

            _timer += Time.deltaTime;
            float charTime = 1f / charsPerSecond;

            // 检查标点符号延迟
            if (_currentCharIndex < _fullText.Length)
            {
                char targetChar = _fullText[_currentCharIndex];
                bool isPunctuation = Array.IndexOf(_punctuation, targetChar) >= 0;

                if (isPunctuation && _timer < punctuationDelay)
                {
                    return; // 等待标点延迟
                }

                if (_timer >= (isPunctuation ? punctuationDelay : charTime))
                {
                    _timer = 0f;
                    _currentCharIndex++;
                    _displayedText = _fullText.Substring(0, _currentCharIndex);
                    _targetText.text = _displayedText;
                }
            }
            else
            {
                // 打字完成
                _isTyping = false;
                onTypingCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 开始打字机效果
        /// </summary>
        public void StartTyping(string text, Action onComplete = null, Action onInterrupt = null)
        {
            _fullText = text;
            _displayedText = string.Empty;
            _currentCharIndex = 0;
            _timer = 0f;
            _isTyping = true;
            _isPaused = false;

            _targetText.text = string.Empty;
            onTypingCompleted = onComplete;
            onTypingInterrupted = onInterrupt;
        }

        /// <summary>
        /// 跳过打字直接显示完整文本
        /// </summary>
        public void Skip()
        {
            if (!_isTyping) return;

            _isTyping = false;
            _displayedText = _fullText;
            _targetText.text = _fullText;
            onTypingInterrupted?.Invoke();
        }

        /// <summary>
        /// 暂停打字机效果
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// 恢复打字机效果
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// 是否正在打字
        /// </summary>
        public bool isTyping => _isTyping;

        /// <summary>
        /// 获取当前显示的文本
        /// </summary>
        public string displayedText => _displayedText;

        /// <summary>
        /// 获取完整文本
        /// </summary>
        public string fullText => _fullText;

        /// <summary>
        /// 获取打字进度 (0-1)
        /// </summary>
        public float progress => string.IsNullOrEmpty(_fullText) ? 1f : (float)_currentCharIndex / _fullText.Length;

        private void OnMouseDown()
        {
            if (skipOnClick && _isTyping)
            {
                Skip();
            }
        }
    }

    /// <summary>
    /// 渐变效果组件
    /// 支持 CanvasGroup 或 Material Color 渐变
    /// </summary>
    public class FadeEffect : MonoBehaviour
    {
        [Header("渐变设置")]
        public float fadeDuration = 0.3f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CanvasGroup _canvasGroup;
        private Graphic[] _graphics;
        private Color _originalColor = Color.white;
        private bool _isFading = false;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _graphics = GetComponentsInChildren<Graphic>();
        }

        /// <summary>
        /// 淡入
        /// </summary>
        public Coroutine FadeIn(Action onComplete = null)
        {
            return StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
        }

        /// <summary>
        /// 淡出
        /// </summary>
        public Coroutine FadeOut(Action onComplete = null)
        {
            return StartCoroutine(FadeCoroutine(1f, 0f, onComplete));
        }

        /// <summary>
        /// 淡入淡出循环
        /// </summary>
        public Coroutine FadeInOut(float holdDuration = 0.5f, Action onComplete = null)
        {
            return StartCoroutine(FadeInOutCoroutine(holdDuration, onComplete));
        }

        private IEnumerator FadeInOutCoroutine(float holdDuration, Action onComplete)
        {
            yield return FadeCoroutine(0f, 1f, null);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeCoroutine(1f, 0f, null);
            onComplete?.Invoke();
        }

        private IEnumerator FadeCoroutine(float from, float to, Action onComplete)
        {
            _isFading = true;
            float elapsed = 0f;
            float t = 0f;

            // CanvasGroup 渐变
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = from;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    t = curve.Evaluate(elapsed / fadeDuration);
                    _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                    yield return null;
                }
                _canvasGroup.alpha = to;
            }
            else
            {
                // Graphic 渐变
                SetGraphicsAlpha(from);
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    t = curve.Evaluate(elapsed / fadeDuration);
                    SetGraphicsAlpha(Mathf.Lerp(from, to, t));
                    yield return null;
                }
                SetGraphicsAlpha(to);
            }

            _isFading = false;
            onComplete?.Invoke();
        }

        private void SetGraphicsAlpha(float alpha)
        {
            foreach (var graphic in _graphics)
            {
                if (graphic != null)
                {
                    var color = graphic.color;
                    color.a = alpha;
                    graphic.color = color;
                }
            }
        }

        /// <summary>
        /// 设置立即显示/隐藏
        /// </summary>
        public void SetImmediate(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
            else
            {
                SetGraphicsAlpha(alpha);
            }
        }

        public bool isFading => _isFading;
    }

    /// <summary>
    /// 选项按钮动画组件
    /// 悬停、选中、禁用状态动画
    /// </summary>
    public class ChoiceButtonAnimator : MonoBehaviour
    {
        [Header("动画设置")]
        public float hoverScale = 1.05f;
        public float hoverDuration = 0.15f;
        public float selectDuration = 0.1f;

        [Header("颜色设置")]
        public Color hoverColor = new Color(1.2f, 1.2f, 1.2f);
        public Color selectedColor = new Color(1.3f, 1.3f, 1.3f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("引用")]
        public Button button;
        public TMP_Text buttonText;
        public Image backgroundImage;

        private Vector3 _originalScale;
        private Color _originalTextColor;
        private Color _originalBgColor;
        private bool _isHovered = false;
        private bool _isSelected = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (buttonText != null)
                _originalTextColor = buttonText.color;
            if (backgroundImage != null)
                _originalBgColor = backgroundImage.color;
        }

        public void OnHoverEnter()
        {
            if (button != null && !button.interactable) return;
            _isHovered = true;
            StopAllCoroutines();
            StartCoroutine(ScaleCoroutine(_originalScale * hoverScale, hoverDuration));
            SetColors(hoverColor);
        }

        public void OnHoverExit()
        {
            _isHovered = false;
            StopAllCoroutines();
            StartCoroutine(ScaleCoroutine(_originalScale, hoverDuration));
            ResetColors();
        }

        public void OnSelect()
        {
            if (button != null && !button.interactable) return;
            _isSelected = true;
            StopAllCoroutines();
            StartCoroutine(ScaleCoroutine(_originalScale * 0.95f, selectDuration));
            SetColors(selectedColor);
        }

        public void OnDeselect()
        {
            _isSelected = false;
            StopAllCoroutines();
            StartCoroutine(ScaleCoroutine(_originalScale, hoverDuration));
            if (_isHovered)
                SetColors(hoverColor);
            else
                ResetColors();
        }

        public void SetEnabled(bool enabled)
        {
            if (button != null)
                button.interactable = enabled;

            if (!enabled)
            {
                StopAllCoroutines();
                transform.localScale = _originalScale;
                if (backgroundImage != null)
                    backgroundImage.color = disabledColor;
                if (buttonText != null)
                    buttonText.color = disabledColor;
            }
            else
            {
                ResetColors();
            }
        }

        private IEnumerator ScaleCoroutine(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void SetColors(Color tint)
        {
            if (buttonText != null)
                buttonText.color = _originalTextColor * tint;
            if (backgroundImage != null)
                backgroundImage.color = _originalBgColor * tint;
        }

        private void ResetColors()
        {
            if (buttonText != null)
                buttonText.color = _originalTextColor;
            if (backgroundImage != null)
                backgroundImage.color = _originalBgColor;
        }
    }

    /// <summary>
    /// 角色头像数据
    /// </summary>
    [Serializable]
    public class CharacterPortraitData
    {
        public string characterId;
        public string displayName;
        public Sprite portrait;
        public CharacterExpression expression = CharacterExpression.Neutral;
        public CharacterAlignment alignment = CharacterAlignment.Left;
    }

    /// <summary>
    /// 角色表情枚举
    /// </summary>
    public enum CharacterExpression
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Thoughtful
    }

    /// <summary>
    /// 头像对齐位置
    /// </summary>
    public enum CharacterAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// 角色头像组件
    /// 显示对话角色头像和名称
    /// </summary>
    public class CharacterPortrait : MonoBehaviour
    {
        [Header("头像设置")]
        public Image portraitImage;
        public TMP_Text nameText;
        public GameObject portraitContainer;

        [Header("动画设置")]
        public float slideInDuration = 0.3f;
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private CharacterPortraitData _currentData;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.localPosition;
        }

        /// <summary>
        /// 显示头像
        /// </summary>
        public void Show(CharacterPortraitData data)
        {
            _currentData = data;

            if (portraitContainer != null)
                portraitContainer.SetActive(true);

            if (portraitImage != null && data.portrait != null)
                portraitImage.sprite = data.portrait;

            if (nameText != null)
                nameText.text = data.displayName;

            // 滑入动画
            StartCoroutine(SlideInCoroutine(data.alignment));
        }

        /// <summary>
        /// 隐藏头像
        /// </summary>
        public Coroutine Hide(Action onComplete = null)
        {
            return StartCoroutine(SlideOutCoroutine(() =>
            {
                if (portraitContainer != null)
                    portraitContainer.SetActive(false);
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// 切换表情
        /// </summary>
        public void SetExpression(CharacterExpression expression)
        {
            if (_currentData != null)
            {
                _currentData.expression = expression;
                // 表情切换逻辑 - 可以实现为精灵切换或动画
            }
        }

        private IEnumerator SlideInCoroutine(CharacterAlignment alignment)
        {
            // 根据对齐设置目标位置
            Vector3 targetPos = _originalPosition;
            Vector3 startPos = targetPos + new Vector3(alignment == CharacterAlignment.Left ? -100f : 100f, 0, 0);

            _rectTransform.localPosition = startPos;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideCurve.Evaluate(elapsed / slideInDuration);
                _rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            _rectTransform.localPosition = targetPos;
        }

        private IEnumerator SlideOutCoroutine(Action onComplete)
        {
            Vector3 startPos = _rectTransform.localPosition;
            Vector3 endPos = startPos + new Vector3(-100f, 0, 0);

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = slideCurve.Evaluate(elapsed / slideInDuration);
                _rectTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            _rectTransform.localPosition = _originalPosition;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 对话框动画协调器
    /// 管理对话框的入场、出场、打字机效果的协调
    /// </summary>
    public class DialogAnimator : MonoBehaviour
    {
        [Header("组件引用")]
        public TypewriterEffect typewriter;
        public FadeEffect fadeEffect;
        public CharacterPortrait characterPortrait;

        [Header("对话设置")]
        public bool autoPlay = true;
        public float autoPlayDelay = 0.5f;

        private DialogAnimationData _currentAnimation;
        private bool _isPlaying = false;

        /// <summary>
        /// 播放对话动画
        /// </summary>
        public Coroutine PlayDialog(string text, CharacterPortraitData portrait = null, DialogAnimationData animation = null)
        {
            _currentAnimation = animation ?? new DialogAnimationData();
            _isPlaying = true;

            // 显示头像
            if (portrait != null && characterPortrait != null)
            {
                characterPortrait.Show(portrait);
            }

            // 开始打字机效果
            if (typewriter != null && !string.IsNullOrEmpty(text))
            {
                return StartCoroutine(PlayTypingSequence(text));
            }

            return null;
        }

        /// <summary>
        /// 播放打字机序列
        /// </summary>
        private IEnumerator PlayTypingSequence(string text)
        {
            if (_currentAnimation.delay > 0)
                yield return new WaitForSeconds(_currentAnimation.delay);

            typewriter.StartTyping(text, () =>
            {
                _isPlaying = false;
            });

            while (typewriter.isTyping)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 跳过当前打字
        /// </summary>
        public void SkipTyping()
        {
            if (typewriter != null && typewriter.isTyping)
            {
                typewriter.Skip();
            }
        }

        /// <summary>
        /// 淡出对话框
        /// </summary>
        public Coroutine FadeOutDialog(Action onComplete = null)
        {
            if (fadeEffect != null)
            {
                return StartCoroutine(FadeOutSequence(onComplete));
            }
            onComplete?.Invoke();
            return null;
        }

        private IEnumerator FadeOutSequence(Action onComplete)
        {
            if (characterPortrait != null)
                yield return characterPortrait.Hide();

            if (fadeEffect != null)
                yield return fadeEffect.FadeOut(onComplete);
            else
                onComplete?.Invoke();
        }

        public bool isPlaying => _isPlaying;
    }
}