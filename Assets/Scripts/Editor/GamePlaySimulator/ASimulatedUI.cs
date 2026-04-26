using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game1.GamePlay
{
    /// <summary>
    /// Runtime UI generator for simulation (no prefabs)
    /// </summary>
    public class ASimulatedUI
    {
        #region Canvas
        /// <summary>
        /// Create a Canvas at runtime
        /// </summary>
        public static Canvas CreateCanvas(string name = "SimCanvas")
        {
            var go = new GameObject(name);
            go.transform.position = Vector3.zero;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(
                AConfig.Active.uiReferenceWidth,
                AConfig.Active.uiReferenceHeight
            );
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return canvas;
        }
        #endregion

        #region Progress Bar
        /// <summary>
        /// Create a progress bar at runtime
        /// </summary>
        public static GameObject CreateProgressBar(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            var config = AConfig.Active;

            // Root
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = config.progressBarBackground;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = config.progressBarFill;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;

            return go;
        }

        /// <summary>
        /// Update progress bar fill amount
        /// </summary>
        public static void UpdateProgressBar(GameObject bar, float value, float maxValue)
        {
            if (bar == null) return;

            var fill = bar.transform.Find("Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.fillAmount = maxValue > 0 ? Mathf.Clamp01(value / maxValue) : 0f;
            }
        }

        /// <summary>
        /// Update progress bar color based on percentage
        /// </summary>
        public static void UpdateProgressBarColor(GameObject bar, float ratio)
        {
            if (bar == null) return;

            var fill = bar.transform.Find("Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                // Green > 50%, Yellow 20-50%, Red < 20%
                if (ratio > 0.5f)
                    fill.color = new Color(0f, 0.8f, 0.2f); // Green
                else if (ratio > 0.2f)
                    fill.color = new Color(0.8f, 0.8f, 0.2f); // Yellow
                else
                    fill.color = new Color(0.8f, 0.2f, 0.2f); // Red
            }
        }
        #endregion

        #region Text
        /// <summary>
        /// Create a Text element at runtime
        /// </summary>
        public static Text CreateText(
            Transform parent,
            string content,
            string name,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize = 24,
            Color? textColor = null,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = textColor ?? Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;

            return text;
        }

        /// <summary>
        /// Update text content
        /// </summary>
        public static void UpdateText(GameObject textObj, string content)
        {
            var text = textObj?.GetComponent<Text>();
            if (text != null)
                text.text = content;
        }
        #endregion

        #region Button
        /// <summary>
        /// Create a Button at runtime
        /// </summary>
        public static Button CreateButton(
            Transform parent,
            string label,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick)
        {
            var config = AConfig.Active;

            // Root
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Image (background)
            var image = go.AddComponent<Image>();
            image.color = config.buttonBackground;

            // Button
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 20;
            text.color = config.buttonText;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            return button;
        }

        /// <summary>
        /// Create button with hover effect
        /// </summary>
        public static Button CreateButtonWithHover(
            Transform parent,
            string label,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction onClick,
            Color? hoverColor = null)
        {
            var btn = CreateButton(parent, label, name, anchorMin, anchorMax, sizeDelta, anchoredPosition, null);

            var hover = hoverColor ?? new Color(0.5f, 0.5f, 0.5f);
            var normalColor = AConfig.Active.buttonBackground;

            btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() =>
            {
                // Restore normal color on click
                btn.targetGraphic.color = normalColor;
            });

            // Add hover events
            var eventTrigger = btn.gameObject.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) =>
            {
                btn.targetGraphic.color = hover;
            });
            eventTrigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) =>
            {
                btn.targetGraphic.color = normalColor;
            });
            eventTrigger.triggers.Add(exitEntry);

            return btn;
        }
        #endregion

        #region Panel
        /// <summary>
        /// Create a background panel
        /// </summary>
        public static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            Color? backgroundColor = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = go.AddComponent<Image>();
            image.color = backgroundColor ?? new Color(0, 0, 0, 0.8f);

            return go;
        }
        #endregion

        #region Vertical Layout
        /// <summary>
        /// Create a vertical layout group
        /// </summary>
        public static VerticalLayoutGroup CreateVerticalLayout(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition,
            float spacing = 10f,
            float padding = 10f)
        {
            var go = CreatePanel(parent, name, anchorMin, anchorMax, sizeDelta, anchoredPosition, new Color(0, 0, 0, 0));
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return layout;
        }
        #endregion
    }

    /// <summary>
    /// UI element references for runtime manipulation
    /// </summary>
    [Serializable]
    public class AUIElements
    {
        public GameObject rootCanvas;
        public GameObject healthBar;
        public GameObject goldText;
        public GameObject levelText;
        public GameObject progressBar;
        public GameObject travelProgressBar;
        public GameObject logText;
        public List<GameObject> buttons = new();

        public void Destroy()
        {
            if (rootCanvas != null)
                UnityEngine.Object.Destroy(rootCanvas);
        }
    }
}