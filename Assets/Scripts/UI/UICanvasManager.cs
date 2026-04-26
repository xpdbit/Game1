using UnityEngine;
using UnityEngine.UI;

namespace Game1
{
    /// <summary>
    /// UI渲染层级配置
    /// 用于Canvas动静分离后的排序控制
    /// </summary>
    public static class UIRenderLayer
    {
        public const int StaticCanvasOrder = 0;
        public const int DynamicCanvasOrder = 1;
        public const int HUDCanvasOrder = 2;
        public const int ModalCanvasOrder = 3;

        /// <summary>
        /// 为主Canvas设置排序层级
        /// </summary>
        public static void SetCanvasSortOrder(Canvas canvas, int layer)
        {
            if (canvas != null)
            {
                canvas.sortingOrder = layer;
            }
        }
    }

    /// <summary>
    /// Canvas动静分离管理器
    /// 提供静态获取各Canvas引用的接口
    /// </summary>
    public class UICanvasManager : MonoBehaviour
    {
        #region Singleton
        private static UICanvasManager _instance;
        public static UICanvasManager instance => _instance;
        #endregion

        #region Canvas References
        [Header("静态Canvas - 背景/装饰/不变按钮")]
        public Canvas staticCanvas;
        public Canvas staticCanvas_prefab;
        private const string STATIC_CANVAS_PATH = "Prefabs/UI/StaticCanvas";

        [Header("动态Canvas - 进度条/动态文本")]
        public Canvas dynamicCanvas;
        public Canvas dynamicCanvas_prefab;
        private const string DYNAMIC_CANVAS_PATH = "Prefabs/UI/DynamicCanvas";

        [Header("HUD Canvas - 调试信息/提示")]
        public Canvas hudCanvas;
        public Canvas hudCanvas_prefab;
        private const string HUD_CANVAS_PATH = "Prefabs/UI/HUDCanvas";
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeCanvases();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化所有Canvas
        /// 如果场景中没有则从预制件加载
        /// </summary>
        private void InitializeCanvases()
        {
            // 确保所有Canvas存在
            EnsureCanvasExists(ref staticCanvas, staticCanvas_prefab, "StaticCanvas", UIRenderLayer.StaticCanvasOrder);
            EnsureCanvasExists(ref dynamicCanvas, dynamicCanvas_prefab, "DynamicCanvas", UIRenderLayer.DynamicCanvasOrder);
            EnsureCanvasExists(ref hudCanvas, hudCanvas_prefab, "HUDCanvas", UIRenderLayer.HUDCanvasOrder);
        }

        private void EnsureCanvasExists(ref Canvas canvas, Canvas prefab, string name, int order)
        {
            if (canvas == null)
            {
                // 尝试查找
                var existing = GameObject.Find(name);
                if (existing != null)
                {
                    canvas = existing.GetComponent<Canvas>();
                }
                else if (prefab != null)
                {
                    // 从预制件实例化
                    var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    go.name = name;
                    canvas = go.GetComponent<Canvas>();
                }
                else
                {
                    // 运行时创建
                    var go = new GameObject(name);
                    canvas = go.AddComponent<Canvas>();
                    go.AddComponent<CanvasScaler>();
                    go.AddComponent<GraphicRaycaster>();
                }

                canvas.sortingOrder = order;
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// 获取静态Canvas（用于背景、不变元素）
        /// </summary>
        public Canvas GetStaticCanvas() => staticCanvas;

        /// <summary>
        /// 获取动态Canvas（用于进度条、动态文本）
        /// </summary>
        public Canvas GetDynamicCanvas() => dynamicCanvas;

        /// <summary>
        /// 获取HUD Canvas（用于调试信息）
        /// </summary>
        public Canvas GetHUDCanvas() => hudCanvas;

        /// <summary>
        /// 将UI元素移到指定Canvas
        /// </summary>
        public void MoveToCanvas(RectTransform target, int layer)
        {
            Canvas canvas = layer switch
            {
                UIRenderLayer.StaticCanvasOrder => staticCanvas,
                UIRenderLayer.DynamicCanvasOrder => dynamicCanvas,
                UIRenderLayer.HUDCanvasOrder => hudCanvas,
                _ => dynamicCanvas
            };

            if (canvas != null && target != null)
            {
                target.SetParent(canvas.transform, false);
            }
        }
        #endregion
    }
}
