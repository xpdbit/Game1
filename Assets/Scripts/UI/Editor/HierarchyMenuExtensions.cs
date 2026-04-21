#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game1.Editor
{
    /// <summary>
    /// Unity Hierarchy右键菜单扩展
    /// </summary>
    public static class HierarchyMenuExtensions
    {
        /// <summary>
        /// 创建Progress Bar预制件
        /// </summary>
        [MenuItem("GameObject/UI/Game1/Progress Bar", false, 100)]
        public static void CreateProgressBar(MenuCommand menuCommand)
        {
            // 创建Canvas（如果不存在）
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // 创建Progress Bar根对象
            GameObject progressBarRoot = CreateProgressBarHierarchy(canvas.gameObject);

            // 注册撤销
            Undo.RegisterCreatedObjectUndo(progressBarRoot, "Create Progress Bar");
            Selection.activeGameObject = progressBarRoot;
        }

        private static GameObject CreateProgressBarHierarchy(GameObject parent)
        {
            // 创建Progress Bar父对象
            GameObject progressBarRoot = new GameObject("ProgressBar_Root");
            RectTransform rootRect = progressBarRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(400, 30);
            rootRect.anchoredPosition = Vector2.zero;

            GameObjectUtility.SetParentAndAlign(progressBarRoot, parent);

            // 创建背景条
            GameObject background = new GameObject("Background");
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(1, 1);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            GameObjectUtility.SetParentAndAlign(background, progressBarRoot);

            // 创建进度条
            GameObject progressFill = new GameObject("ProgressFill");
            RectTransform fillRect = progressFill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0.5f, 1); // 50%填充
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = progressFill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 0.3f, 1f); // 绿色
            GameObjectUtility.SetParentAndAlign(progressFill, progressBarRoot);

            // 创建指示器
            GameObject indicator = new GameObject("Indicator");
            RectTransform indRect = indicator.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0.5f, 0.5f);
            indRect.anchorMax = new Vector2(0.5f, 0.5f);
            indRect.sizeDelta = new Vector2(4, 40);
            indRect.offsetMin = Vector2.zero;
            indRect.offsetMax = Vector2.zero;
            Image indImage = indicator.AddComponent<Image>();
            indImage.color = Color.white;
            GameObjectUtility.SetParentAndAlign(indicator, progressBarRoot);

            // 创建百分比文本 - 先TextMeshPro后UIText
            GameObject textObj = new GameObject("PercentText");
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // 先添加TextMeshProUGUI (UIText的RequireComponent会要求它)
            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "50%";
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = 18;

            // 再添加UIText (它需要TMPro已经存在)
            Game1.UIText uiText = textObj.AddComponent<Game1.UIText>();

            GameObjectUtility.SetParentAndAlign(textObj, progressBarRoot);

            // 添加UIProgressBar组件到根对象
            Game1.UIProgressBar progressBar = progressBarRoot.AddComponent<Game1.UIProgressBar>();
            progressBar.progressBar = fillRect;
            progressBar.indicator = indRect;
            progressBar.indicatorPercentText = uiText;
            progressBar.direction = Game1.UIProgressBar.Direction.LeftToRight;
            progressBar.UpdateBarImmediate(0.5f);

            return progressBarRoot;
        }

        /// <summary>
        /// 创建UIText
        /// </summary>
        [MenuItem("GameObject/UI/Game1/Text (TMP)", false, 101)]
        public static void CreateUIText(MenuCommand menuCommand)
        {
            Canvas canvas = GetOrCreateCanvas();

            GameObject textObj = new GameObject("UIText");
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 50);
            rect.anchoredPosition = Vector2.zero;

            // 先添加TextMeshProUGUI
            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "New Text";
            tmp.fontSize = 24;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // 再添加UIText
            Game1.UIText uiText = textObj.AddComponent<Game1.UIText>();

            GameObjectUtility.SetParentAndAlign(textObj, canvas.gameObject);
            Undo.RegisterCreatedObjectUndo(textObj, "Create UIText");

            Selection.activeGameObject = textObj;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            return canvas;
        }
    }
}
#endif