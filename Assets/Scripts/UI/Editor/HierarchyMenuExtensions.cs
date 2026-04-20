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

        /// <summary>
        /// 创建Inventory背包预制件
        /// </summary>
        [MenuItem("GameObject/UI/Game1/Inventory", false, 102)]
        public static void CreateInventory(MenuCommand menuCommand)
        {
            Canvas canvas = GetOrCreateCanvas();

            // 创建Inventory根对象
            GameObject inventoryRoot = new GameObject("Inventory");
            RectTransform rootRect = inventoryRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0, 0);
            rootRect.anchorMax = new Vector2(1, 1);
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(inventoryRoot, canvas.gameObject);

            // 添加UIInventory组件
            Game1.UIInventory inventory = inventoryRoot.AddComponent<Game1.UIInventory>();

            // 创建物品列表容器
            GameObject itemsContainer = new GameObject("ItemsContainer");
            RectTransform containerRect = itemsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);
            GameObjectUtility.SetParentAndAlign(itemsContainer, inventoryRoot);

            // 创建模板物品行（隐藏）
            GameObject templateItem = CreateInventoryItemTemplate(inventoryRoot);

            // 添加UILayout组件到容器
            UILayout layout = itemsContainer.AddComponent<UILayout>();
            layout.parentRT = containerRect;
            layout.layoutRow = UILayout.LayoutRow.TileVertical;
            layout.validLineSize = 300;
            layout.spacing = new Vector2(5, 5);
            inventory.layout = layout;

            // 添加UIListItems组件
            UIListItems listItems = inventoryRoot.AddComponent<UIListItems>();
            listItems.templateRT = templateItem.GetComponent<RectTransform>();
            listItems.layout = layout;
            inventory.listItems = listItems;

            // 创建顶部操作栏（简化版）
            CreateInventoryTopBarSimple(inventoryRoot, inventory);

            // 创建底部操作栏（简化版）
            CreateInventoryBottomBarSimple(inventoryRoot, inventory);

            // 隐藏模板
            templateItem.SetActive(false);

            Undo.RegisterCreatedObjectUndo(inventoryRoot, "Create Inventory");
            Selection.activeGameObject = inventoryRoot;
        }

        private static GameObject CreateInventoryItemTemplate(GameObject parent)
        {
            // 创建模板物品行
            GameObject template = new GameObject("ItemTemplate");
            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.sizeDelta = new Vector2(300, 60);
            GameObjectUtility.SetParentAndAlign(template, parent);

            // 背景
            GameObject bg = new GameObject("Background");
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(1, 1, 1, 0.5f);
            GameObjectUtility.SetParentAndAlign(bg, template);

            // 左侧：Image + Name
            GameObject leftSide = new GameObject("LeftSide");
            RectTransform leftRect = leftSide.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0.5f, 1);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(leftSide, template);

            // Image
            GameObject imageObj = new GameObject("Image");
            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0, 0.2f);
            imageRect.anchorMax = new Vector2(0, 0.8f);
            imageRect.sizeDelta = new Vector2(50, 50);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            Image itemImage = imageObj.AddComponent<Image>();
            itemImage.color = Color.gray;
            GameObjectUtility.SetParentAndAlign(imageObj, leftSide);

            // Name Text - 只用TextMeshPro不用UIText避免空引用
            GameObject nameObj = new GameObject("NameText");
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(55, 0);
            nameRect.offsetMax = Vector2.zero;
            TMPro.TextMeshProUGUI nameTmp = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            nameTmp.text = "Item Name";
            nameTmp.fontSize = 16;
            nameTmp.alignment = TMPro.TextAlignmentOptions.Left;
            nameTmp.color = Color.white;
            GameObjectUtility.SetParentAndAlign(nameObj, leftSide);

            // 右侧：Amount
            GameObject amountObj = new GameObject("AmountText");
            RectTransform amountRect = amountObj.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0.7f, 0);
            amountRect.anchorMax = new Vector2(1, 1);
            amountRect.offsetMin = Vector2.zero;
            amountRect.offsetMax = Vector2.zero;
            TMPro.TextMeshProUGUI amountTmp = amountObj.AddComponent<TMPro.TextMeshProUGUI>();
            amountTmp.text = "x1";
            amountTmp.fontSize = 18;
            amountTmp.alignment = TMPro.TextAlignmentOptions.Right;
            amountTmp.color = Color.yellow;
            GameObjectUtility.SetParentAndAlign(amountObj, template);

            // Checkbox (可选)
            GameObject checkbox = new GameObject("Checkbox");
            RectTransform checkRect = checkbox.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.55f, 0.3f);
            checkRect.anchorMax = new Vector2(0.55f, 0.7f);
            checkRect.sizeDelta = new Vector2(20, 20);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            Image checkImage = checkbox.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            GameObjectUtility.SetParentAndAlign(checkbox, template);

            // Checkmark (默认隐藏)
            GameObject checkmark = new GameObject("Checkmark");
            RectTransform checkmarkRect = checkmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = Vector2.zero;
            checkmarkRect.offsetMax = Vector2.zero;
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = Color.green;
            checkmark.SetActive(false);
            GameObjectUtility.SetParentAndAlign(checkmark, checkbox);

            // 添加UIInventoryItem组件
            Game1.UIInventoryItem itemComponent = template.AddComponent<Game1.UIInventoryItem>();
            itemComponent.backgroundImage = bgImage;
            itemComponent.itemImage = itemImage;
            // 延迟获取UIText组件避免空引用
            itemComponent.itemNameText = null; // 暂时设为null，后面通过代码引用
            itemComponent.amountText = null;
            itemComponent.checkboxBackground = checkImage;
            itemComponent.checkmarkImage = checkmarkImage;
            itemComponent.disabledOverlay = null;
            itemComponent.normalColor = new Color(1, 1, 1, 0.5f);
            itemComponent.highlightedColor = new Color(1, 0.9f, 0.5f, 0.8f);
            itemComponent.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            return template;
        }

        private static void CreateInventoryTopBarSimple(GameObject parent, Game1.UIInventory inventory)
        {
            // 创建顶部操作栏
            GameObject topBar = new GameObject("TopBar");
            RectTransform topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.85f);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.offsetMin = Vector2.zero;
            topRect.offsetMax = new Vector2(0, -30);
            GameObjectUtility.SetParentAndAlign(topBar, parent);

            // 全选按钮
            GameObject selectAllBtn = new GameObject("SelectAllButton");
            RectTransform selectAllRect = selectAllBtn.AddComponent<RectTransform>();
            selectAllRect.anchorMin = new Vector2(0, 0);
            selectAllRect.anchorMax = new Vector2(0, 1);
            selectAllRect.sizeDelta = new Vector2(80, 30);
            selectAllRect.offsetMin = Vector2.zero;
            selectAllRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(selectAllBtn, topBar);
            selectAllBtn.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 1f);
            Button selectAllButton = selectAllBtn.AddComponent<Button>();
            selectAllButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 取消全选按钮
            GameObject deselectBtn = new GameObject("DeselectButton");
            RectTransform deselectRect = deselectBtn.AddComponent<RectTransform>();
            deselectRect.anchorMin = new Vector2(0.15f, 0);
            deselectRect.anchorMax = new Vector2(0.15f, 1);
            deselectRect.sizeDelta = new Vector2(80, 30);
            deselectRect.offsetMin = Vector2.zero;
            deselectRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(deselectBtn, topBar);
            deselectBtn.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f, 1f);
            Button deselectButton = deselectBtn.AddComponent<Button>();
            deselectButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 已选择数量文本
            GameObject selectedCountObj = new GameObject("SelectedCountText");
            RectTransform selectedCountRect = selectedCountObj.AddComponent<RectTransform>();
            selectedCountRect.anchorMin = new Vector2(0.5f, 0);
            selectedCountRect.anchorMax = new Vector2(1, 1);
            selectedCountRect.offsetMin = Vector2.zero;
            selectedCountRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(selectedCountObj, topBar);

            // 使用UnityEngine.UI.Text代替TextMeshPro
            UnityEngine.UI.Text selectedCountText = selectedCountObj.AddComponent<UnityEngine.UI.Text>();
            selectedCountText.text = "已选择: 0";
            selectedCountText.fontSize = 16;
            selectedCountText.color = Color.white;
            selectedCountText.alignment = TextAnchor.MiddleCenter;

            // 延迟设置UIText引用
            inventory.selectedCountText = null;
        }

        private static void CreateInventoryBottomBarSimple(GameObject parent, Game1.UIInventory inventory)
        {
            // 创建底部操作栏
            GameObject bottomBar = new GameObject("BottomBar");
            RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = Vector2.zero;
            bottomRect.anchorMax = new Vector2(1, 0.15f);
            bottomRect.offsetMin = new Vector2(0, 0);
            bottomRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(bottomBar, parent);

            // 使用按钮
            GameObject useBtn = new GameObject("UseButton");
            RectTransform useRect = useBtn.AddComponent<RectTransform>();
            useRect.anchorMin = new Vector2(0, 0);
            useRect.anchorMax = new Vector2(0, 1);
            useRect.sizeDelta = new Vector2(80, 30);
            useRect.offsetMin = Vector2.zero;
            useRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(useBtn, bottomBar);
            useBtn.AddComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);
            Button useButton = useBtn.AddComponent<Button>();
            useButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 丢弃按钮
            GameObject discardBtn = new GameObject("DiscardButton");
            RectTransform discardRect = discardBtn.AddComponent<RectTransform>();
            discardRect.anchorMin = new Vector2(0.15f, 0);
            discardRect.anchorMax = new Vector2(0.15f, 1);
            discardRect.sizeDelta = new Vector2(80, 30);
            discardRect.offsetMin = Vector2.zero;
            discardRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(discardBtn, bottomBar);
            discardBtn.AddComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);
            Button discardButton = discardBtn.AddComponent<Button>();
            discardButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 总物品数量文本
            GameObject totalObj = new GameObject("TotalItemsText");
            RectTransform totalRect = totalObj.AddComponent<RectTransform>();
            totalRect.anchorMin = new Vector2(0.5f, 0);
            totalRect.anchorMax = new Vector2(1, 1);
            totalRect.offsetMin = Vector2.zero;
            totalRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(totalObj, bottomBar);

            // 使用UnityEngine.UI.Text代替TextMeshPro
            UnityEngine.UI.Text totalText = totalObj.AddComponent<UnityEngine.UI.Text>();
            totalText.text = "共 0 个物品";
            totalText.fontSize = 16;
            totalText.color = Color.white;
            totalText.alignment = TextAnchor.MiddleCenter;

            // 延迟设置UIText引用
            inventory.totalItemsText = null;
        }

        private static void CreateInventoryBottomBarSimple(GameObject parent, Game1.UIInventory inventory)
        {
            // 创建底部操作栏
            GameObject bottomBar = new GameObject("BottomBar");
            RectTransform bottomRect = bottomBar.AddComponent<RectTransform>();
            bottomRect.anchorMin = Vector2.zero;
            bottomRect.anchorMax = new Vector2(1, 0.15f);
            bottomRect.offsetMin = new Vector2(0, 0);
            bottomRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(bottomBar, parent);

            // 使用按钮
            GameObject useBtn = new GameObject("UseButton");
            RectTransform useRect = useBtn.AddComponent<RectTransform>();
            useRect.anchorMin = new Vector2(0, 0);
            useRect.anchorMax = new Vector2(0, 1);
            useRect.sizeDelta = new Vector2(80, 30);
            useRect.offsetMin = Vector2.zero;
            useRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(useBtn, bottomBar);
            Image useImage = useBtn.AddComponent<Image>();
            useImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            TMPro.TextMeshProUGUI useTmp = useBtn.AddComponent<TMPro.TextMeshProUGUI>();
            useTmp.text = "使用";
            useTmp.fontSize = 14;
            useTmp.alignment = TMPro.TextAlignmentOptions.Center;
            useTmp.color = Color.white;
            Button useButton = useBtn.AddComponent<Button>();
            useButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 丢弃按钮
            GameObject discardBtn = new GameObject("DiscardButton");
            RectTransform discardRect = discardBtn.AddComponent<RectTransform>();
            discardRect.anchorMin = new Vector2(0.15f, 0);
            discardRect.anchorMax = new Vector2(0.15f, 1);
            discardRect.sizeDelta = new Vector2(80, 30);
            discardRect.offsetMin = Vector2.zero;
            discardRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(discardBtn, bottomBar);
            Image discardImage = discardBtn.AddComponent<Image>();
            discardImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
            TMPro.TextMeshProUGUI discardTmp = discardBtn.AddComponent<TMPro.TextMeshProUGUI>();
            discardTmp.text = "丢弃";
            discardTmp.fontSize = 14;
            discardTmp.alignment = TMPro.TextAlignmentOptions.Center;
            discardTmp.color = Color.white;
            Button discardButton = discardBtn.AddComponent<Button>();
            discardButton.colors = new ColorBlock { normalColor = Color.gray, highlightedColor = Color.white };

            // 总物品数量文本
            GameObject totalObj = new GameObject("TotalItemsText");
            RectTransform totalRect = totalObj.AddComponent<RectTransform>();
            totalRect.anchorMin = new Vector2(0.5f, 0);
            totalRect.anchorMax = new Vector2(1, 1);
            totalRect.offsetMin = Vector2.zero;
            totalRect.offsetMax = Vector2.zero;
            GameObjectUtility.SetParentAndAlign(totalObj, bottomBar);
            TMPro.TextMeshProUGUI totalTmp = totalObj.AddComponent<TMPro.TextMeshProUGUI>();
            totalTmp.text = "共 0 个物品";
            totalTmp.fontSize = 16;
            totalTmp.alignment = TMPro.TextAlignmentOptions.Center;
            totalTmp.color = Color.white;

            // 延迟设置UIText引用
            inventory.totalItemsText = null;
        }
    }
}
#endif