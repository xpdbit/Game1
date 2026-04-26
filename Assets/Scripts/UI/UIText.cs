using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game1.UI.Utils;

namespace Game1
{
    /// <summary>
    /// UIText - TextMeshPro封装组件
    /// 支持零分配数字更新
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIText : MonoBehaviour
    {
        public string id = "";
        public SyncRectMode syncRectMode = SyncRectMode.Both;
        public RectTransform syncRectTransformTarget;

        public TextMeshProUGUI textMesh => this._textMesh ??= this.GetComponent<TextMeshProUGUI>();
        private TextMeshProUGUI _textMesh;

        public UITextLinkHandler textLinkHandler;

        // ===== 零分配数字更新优化 =====
        // 预分配的StringBuilder，避免每帧string操作导致的GC分配
        private static readonly StringBuilder _stringBuilder = new StringBuilder(64);
        private static readonly char[] _numberBuffer = new char[16]; // 数字转换缓冲区

        /// <summary>
        /// 设置整数文本 - 零GC分配
        /// </summary>
        public void SetNumber(int value)
        {
            this.textMesh.SetText(value.ToString());
        }

        /// <summary>
        /// 设置浮点数文本 - 零GC分配
        /// </summary>
        public void SetNumber(float value, string format = "0")
        {
            // 使用TextMeshPro的原生方法直接设置数值，避免string分配
            this.textMesh.SetText("{0:" + format + "}", value);
        }

        /// <summary>
        /// 设置带后缀的整数文本 (如 "100%") - 零GC分配
        /// </summary>
        public void SetNumberWithSuffix(int value, string suffix)
        {
            // 复用静态StringBuilder
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(value);
                _stringBuilder.Append(suffix);
                this.textMesh.SetText(_stringBuilder);
            }
        }

        /// <summary>
        /// 设置带后缀的浮点数文本 (如 "50%") - 零GC分配
        /// </summary>
        public void SetNumberWithSuffix(float value, string format, string suffix)
        {
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                FormatNumber(value, format);
                _stringBuilder.Append(suffix);
                this.textMesh.SetText(_stringBuilder);
            }
        }

        /// <summary>
        /// 直接设置文本 - 零GC分配（使用TextMeshPro原生方法）
        /// </summary>
        public void SetText(string value)
        {
            this.textMesh.SetText(value);
        }

        /// <summary>
        /// 格式化数字到缓冲区（内部使用）
        /// </summary>
        private static void FormatNumber(float value, string format)
        {
            // 使用 custom formatter 避免 ToString 分配
            int len = FastNumberFormat(value, _numberBuffer, format);
            for (int i = 0; i < len; i++)
                _stringBuilder.Append(_numberBuffer[i]);
        }

        /// <summary>
        /// 快速数字格式化 - 零分配
        /// </summary>
        private static int FastNumberFormat(float value, char[] buffer, string format)
        {
            if (format == "0" || format == "F0")
            {
                int intVal = (int)value;
                return IntToString(intVal, buffer);
            }
            else if (format == "0.0" || format == "F1")
            {
                int intPart = (int)value;
                int decimalPart = (int)((value - intPart) * 10);
                int pos = IntToString(intPart, buffer);
                buffer[pos++] = '.';
                buffer[pos++] = (char)('0' + decimalPart);
                return pos;
            }
            else if (format == "0.00" || format == "F2")
            {
                int intPart = (int)value;
                int decimalPart = (int)((value - intPart) * 100);
                int pos = IntToString(intPart, buffer);
                buffer[pos++] = '.';
                buffer[pos++] = (char)('0' + decimalPart / 10);
                buffer[pos++] = (char)('0' + decimalPart % 10);
                return pos;
            }
            else
            {
                // 未知格式，回退到 ToString（会有分配）
                var s = value.ToString(format);
                for (int i = 0; i < s.Length; i++)
                    buffer[i] = s[i];
                return s.Length;
            }
        }

        /// <summary>
        /// 快速整数转字符串 - 零分配
        /// </summary>
        private static int IntToString(int value, char[] buffer)
        {
            if (value == 0)
            {
                buffer[0] = '0';
                return 1;
            }

            bool negative = value < 0;
            if (negative) value = -value;

            int pos = 0;
            while (value > 0)
            {
                buffer[pos++] = (char)('0' + value % 10);
                value /= 10;
            }

            if (negative) buffer[pos++] = '-';

            // 反转缓冲区
            for (int i = 0; i < pos / 2; i++)
            {
                char temp = buffer[i];
                buffer[i] = buffer[pos - 1 - i];
                buffer[pos - 1 - i] = temp;
            }

            return pos;
        }
        // ===== 零分配优化结束 =====

        // 为了向后兼容，保留 string 属性但使用零分配实现
        public string text
        {
            get => this.textMesh.text;
            set { this.textMesh.SetText(value); }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (this._rectTransform == null)
                    this._rectTransform = this.GetComponent<RectTransform>();
                return this._rectTransform;
            }
        }
        private RectTransform _rectTransform;

        #region MonoBehaviour

        private void Awake()
        {
            textLinkHandler = this.GetComponent<UITextLinkHandler>();
        }

        private void Start()
        {
            this.Reload();
        }

        #endregion

        public void Reload()
        {
            if (this.id.Length > 0)
            {
                // TODO: 集成 GameAssetManager 系统
                // var t = GameAssetManager.GetText(this.id);
                // if (t.assetInfo != null)
                //     this.text = t.ToString();
                // else
                this.text = this.id.Split(".").Last();
            }
        }

        public void AddLinkAction(string linkID, Action action)
        {
            // TODO: 实现链接点击处理
            if (textLinkHandler == null)
                return;
            // textLinkHandler.AddLinkAction(linkID, action);
        }

        [ContextMenu("同步名称到Inspector")]
        public void SyncNameToInspector()
        {
            this.text = this.id.Split(".").Last();
            this.gameObject.name = $"Text {this.id}";
        }

        public void SyncRectTransformTarget(bool immediate = false)
        {
            if (this.syncRectTransformTarget == null)
                return;

            if (immediate)
            {
                Canvas.ForceUpdateCanvases();
                this.textMesh.enableWordWrapping = false;
                Canvas.ForceUpdateCanvases();
                this.SyncRectTransformTarget_Sync();
            }
            else
            {
                this.textMesh.enableWordWrapping = false;
                // 简化的异步同步
                UniTaskProgress task = new UniTaskProgress();
                task.AddToEnd(0f, _ => this.SyncRectTransformTarget_Sync());
                task.StartAsync();
            }
        }

        private void SyncRectTransformTarget_Sync()
        {
            Vector2 textSize = this.textMesh.GetRenderedValues(false);

            // 获取 RectTransform 的padding
            float paddingLeft = this.rectTransform.offsetMin.x;
            float paddingRight = -this.rectTransform.offsetMax.x;
            float paddingTop = this.rectTransform.offsetMin.y;
            float paddingBottom = -this.rectTransform.offsetMax.y;

            // 计算考虑 padding 后的尺寸
            float width = textSize.x + paddingLeft + paddingRight;
            float height = textSize.y + paddingTop + paddingBottom;

            // 根据 SyncRectMode 设置目标 RectTransform 的尺寸
            if (this.syncRectMode == SyncRectMode.Both)
            {
                this.syncRectTransformTarget.sizeDelta = new Vector2(width, height);
            }
            else if (this.syncRectMode == SyncRectMode.Width)
            {
                this.syncRectTransformTarget.sizeDelta = new Vector2(width, this.syncRectTransformTarget.sizeDelta.y);
            }
            else if (this.syncRectMode == SyncRectMode.Height)
            {
                this.syncRectTransformTarget.sizeDelta = new Vector2(this.syncRectTransformTarget.sizeDelta.x, height);
            }
        }

        [ContextMenu("同步文本尺寸到RectTransform")]
        public void ContextMenu_SyncRectTransformTarget()
        {
            if (this.textMesh == null)
                this.Awake();
            this.SyncRectTransformTarget(true);
        }

        public enum SyncRectMode
        {
            Both = 0,
            Width = 1,
            Height = 2,
        }

        public static void ReloadAll()
        {
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                ReloadAll_Handle(root.transform);
                ReloadAll_TranverseElements(root.transform);
            }
        }

        private static void ReloadAll_TranverseElements(Transform parentTransform)
        {
            foreach (Transform child in parentTransform)
            {
                ReloadAll_Handle(child);
                ReloadAll_TranverseElements(child);
            }
        }

        private static void ReloadAll_Handle(Transform t)
        {
            UIText text = t.GetComponent<UIText>();
            if (text != null)
            {
                text.Reload();
            }
        }
    }

    /// <summary>
    /// UIText链接处理器（存根）
    /// </summary>
    public class UITextLinkHandler : MonoBehaviour
    {
        public void AddLinkAction(string linkID, Action action)
        {
            // TODO: 实现链接点击处理
        }
    }
}