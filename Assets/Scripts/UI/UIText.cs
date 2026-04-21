using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game1.UI.Utils;

namespace Game1
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UIText : MonoBehaviour
    {
        public string id = "";
        public SyncRectMode syncRectMode = SyncRectMode.Both;
        public RectTransform syncRectTransformTarget;

        public TextMeshProUGUI textMesh => this._textMesh ??= this.GetComponent<TextMeshProUGUI>();
        private TextMeshProUGUI _textMesh;

        public UITextLinkHandler textLinkHandler;

        public string text
        {
            get => this.textMesh.text;
            set { this.textMesh.text = value; }
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
            if (textLinkHandler == null)
                return;
            textLinkHandler.AddLinkAction(linkID, action);
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