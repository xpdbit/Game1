#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace Game1.Editor.UI.Map
{
    /// <summary>
    /// UIMapPathV2 预制体设置辅助工具
    /// 用于在Editor中快速设置UIMapPathV2所需的Sprites和配置
    /// </summary>
    public static class UIMapPathV2Setup
    {
        [MenuItem("Game1/UI/Setup UIMapPathV2")]
        public static void SetupUIMapPathV2()
        {
            // 创建必要的资源文件夹
            CreateFolders();

            // 创建示例Sprites（使用内置Unity图标）
            CreateSampleSprites();

            Debug.Log("[UIMapPathV2Setup] Setup completed!");
        }

        private static void CreateFolders()
        {
            string[] folders = new string[]
            {
                "Assets/Art/UI/Map/Icons",
                "Assets/Prefabs/UI/Map"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.CreateFolder("Assets/Art/UI/Map", "Icons");
                    AssetDatabase.CreateFolder("Assets/Prefabs", "UI/Map");
                }
            }
        }

        private static void CreateSampleSprites()
        {
            // 说明：这些图标资源需要美术提供
            // 这里创建占位符用于演示

            string iconPath = "Assets/Art/UI/Map/Icons/";
            string[] iconNames = new string[]
            {
                "icon_start",
                "icon_city",
                "icon_wilderness",
                "icon_market",
                "icon_dungeon",
                "icon_boss",
                "icon_goal",
                "icon_current",
                "icon_unexplored"
            };

            foreach (var iconName in iconNames)
            {
                string fullPath = iconPath + iconName + ".png";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
                if (existing == null)
                {
                    Debug.Log($"[UIMapPathV2Setup] 请在 {iconPath} 添加图标: {iconName}.png");
                }
            }
        }

        [MenuItem("Game1/UI/Create UIMapPathV2 Prefab")]
        public static void CreateUIMapPathV2Prefab()
        {
            // 创建Canvas参照
            var canvasObj = new GameObject("UIMapPathV2");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建Panel
            var panelObj = new GameObject("PathPanel");
            panelObj.transform.SetParent(canvasObj.transform);
            var panelRT = panelObj.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;

            var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // 创建路径容器
            var pathContainer = new GameObject("PathContainer");
            pathContainer.transform.SetParent(panelObj.transform);
            var pathRT = pathContainer.AddComponent<RectTransform>();
            pathRT.anchorMin = new Vector2(0, 0.4f);
            pathRT.anchorMax = new Vector2(1, 0.9f);
            pathRT.sizeDelta = new Vector2(0, 100);

            // 创建路径线条模板
            var lineTemplate = new GameObject("PathLineTemplate");
            lineTemplate.transform.SetParent(pathContainer.transform);
            var lineRT = lineTemplate.AddComponent<RectTransform>();
            lineRT.sizeDelta = new Vector2(100, 4);
            var lineImage = lineTemplate.AddComponent<UnityEngine.UI.Image>();
            lineImage.color = Color.green;
            lineTemplate.SetActive(false);

            // 创建标题
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panelObj.transform);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.9f);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.sizeDelta = new Vector2(0, 40);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "旅途 1/10";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;

            // 创建当前位置文本
            var currentObj = new GameObject("CurrentLocationText");
            currentObj.transform.SetParent(panelObj.transform);
            var currentRT = currentObj.AddComponent<RectTransform>();
            currentRT.anchorMin = new Vector2(0, 0.25f);
            currentRT.anchorMax = new Vector2(1, 0.35f);
            currentRT.sizeDelta = new Vector2(0, 30);
            var currentText = currentObj.AddComponent<TextMeshProUGUI>();
            currentText.text = "🏠 起点";
            currentText.fontSize = 18;
            currentText.alignment = TextAlignmentOptions.Center;

            // 创建选项列表容器
            var choicesObj = new GameObject("ChoicesList");
            choicesObj.transform.SetParent(panelObj.transform);
            var choicesRT = choicesObj.AddComponent<RectTransform>();
            choicesRT.anchorMin = new Vector2(0, 0);
            choicesRT.anchorMax = new Vector2(1, 0.2f);
            choicesRT.sizeDelta = new Vector2(0, 0);

            // 选择预设保存路径
            string prefabPath = "Assets/Prefabs/UI/Map/UIMapPathV2.prefab";

            // 检查是否已存在
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.LogWarning($"[UIMapPathV2Setup] Prefab already exists at {prefabPath}");
                return;
            }

            // 创建预制体
            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath, out bool success);
            if (success)
            {
                Debug.Log($"[UIMapPathV2Setup] Created prefab at {prefabPath}");
            }

            // 清理临时对象
            Object.DestroyImmediate(canvasObj);
        }
    }
}
#endif