using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace Game1
{
/// <summary>
    /// 通用资源加载器
    /// 提供 JSON 配置的统一加载接口
    /// </summary>
    public static class ResourceManager
    {
        /// <summary>
        /// 资源加载器注册表
        /// </summary>
        private static readonly Dictionary<Type, ResourceLoaderDelegate> _loaders = new();

        /// <summary>
        /// 资源缓存
        /// </summary>
        private static readonly Dictionary<string, object> _cache = new();

        /// <summary>
        /// 加载器委托
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>加载的资源对象</returns>
        public delegate object ResourceLoaderDelegate(string path);

        /// <summary>
        /// JSON数组包装器
        /// </summary>
        [Serializable]
        private class JsonArrayWrapper<T>
        {
            public T[] items;
        }

        /// <summary>
        /// 注册指定类型的加载器
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="loader">加载函数</param>
        public static void RegisterLoader<T>(Func<string, T> loader) where T : class
        {
            _loaders[typeof(T)] = path => loader(path);
        }

        /// <summary>
        /// 通用加载接口
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（相对于 Resources）</param>
        /// <returns>加载的资源对象</returns>
        public static T Load<T>(string path) where T : class
        {
            // 检查缓存
            var cacheKey = $"{typeof(T).Name}_{path}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached as T;
            }

            // 查找已注册的加载器
            if (_loaders.TryGetValue(typeof(T), out var loader))
            {
                var result = loader(path) as T;
                if (result != null)
                {
                    _cache[cacheKey] = result;
                }
                return result;
            }

            // 默认 JSON 加载
            var json = LoadJson(path);
            if (json == null) return null;

            var obj = JsonUtility.FromJson<T>(json);
            if (obj != null)
            {
                _cache[cacheKey] = obj;
            }
            return obj;
        }

        /// <summary>
        /// 加载JSON数组格式的资源
        /// JsonUtility无法直接解析JSON数组，需要包装器
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（相对于 Resources，不含扩展名）</param>
        /// <returns>加载的资源数组</returns>
        public static T[] LoadArray<T>(string path) where T : class
        {
            var cacheKey = $"Array_{typeof(T).Name}_{path}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached as T[];
            }

            var json = LoadJson(path);
            if (json == null)
            {
                Debug.LogWarning($"[ResourceManager] LoadArray failed: JSON is null for path: {path}");
                return null;
            }

            Debug.Log($"[ResourceManager] LoadArray raw JSON (first 100 chars): {json.Substring(0, Mathf.Min(100, json.Length))}");

            // 处理纯JSON数组格式：[...] -> {...}
            // JsonUtility只能解析对象，不能解析数组
            string wrappedJson = json.Trim();
            if (wrappedJson.StartsWith("["))
            {
                // 将纯数组转换为 {"items": [...]} 格式
                wrappedJson = "{\"items\":" + wrappedJson + "}";
                Debug.Log($"[ResourceManager] Wrapped JSON: {wrappedJson.Substring(0, Mathf.Min(100, wrappedJson.Length))}...");
            }

            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
            if (wrapper?.items == null)
            {
                Debug.LogWarning($"[ResourceManager] LoadArray failed: wrapper or items is null. JSON may be malformed.");
                return null;
            }

            _cache[cacheKey] = wrapper.items;
            return wrapper.items;
        }

        /// <summary>
        /// 加载XML数组格式的资源（手动解析）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（相对于 Resources，不含扩展名）</param>
        /// <returns>加载的资源数组</returns>
        public static T[] LoadXmlArray<T>(string path) where T : class
        {
            var cacheKey = $"XmlArray_{typeof(T).Name}_{path}";
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached as T[];
            }

            var xml = LoadXml(path);
            if (string.IsNullOrEmpty(xml))
            {
                Debug.LogWarning($"[ResourceManager] LoadXmlArray failed: XML is null or empty for path: {path}");
                return null;
            }

            try
            {
                // 对ItemTemplate使用专用手动解析
                if (typeof(T) == typeof(ItemTemplate))
                {
                    var items = ParseItemTemplates(xml);
                    if (items == null || items.Length == 0)
                    {
                        Debug.LogWarning($"[ResourceManager] LoadXmlArray failed: no items parsed");
                        return null;
                    }
                    _cache[cacheKey] = items;
                    return items as T[];
                }

                Debug.LogWarning($"[ResourceManager] LoadXmlArray: generic XML parsing not supported for {typeof(T).Name}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] LoadXmlArray failed: {ex.Message}");
                Debug.LogError($"[ResourceManager] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 手动解析ItemTemplate数组（使用XmlDocument）
        /// </summary>
        private static ItemTemplate[] ParseItemTemplates(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var itemList = new List<ItemTemplate>();
            var itemNodes = doc.SelectNodes("/Items/Item");

            if (itemNodes == null || itemNodes.Count == 0)
            {
                Debug.LogWarning("[ResourceManager] ParseItemTemplates: No Item nodes found");
                return null;
            }

            foreach (XmlNode itemNode in itemNodes)
            {
                try
                {
                    var template = ItemTemplate.ParseFromXml(itemNode as XmlElement);
                    itemList.Add(template);
                    Debug.Log($"[ResourceManager] Parsed ItemTemplate: {template.id}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ResourceManager] Failed to parse Item node: {ex.Message}");
                    continue;
                }
            }

            return itemList.ToArray();
        }

        /// <summary>
        /// 加载 XML 文件内容
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>XML 字符串</returns>
        public static string LoadXml(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            return asset?.text;
        }

        /// <summary>
        /// 批量加载同类型资源（单文件多记录）
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="folderPath">文件夹路径（相对于 Resources）</param>
        /// <returns>加载的资源列表</returns>
        public static T[] LoadAll<T>(string folderPath) where T : class
        {
            var assets = Resources.LoadAll<TextAsset>(folderPath);
            var results = new List<T>();

            foreach (var asset in assets)
            {
                var items = LoadArray<T>(asset.name);
                if (items != null)
                {
                    results.AddRange(items);
                }
            }

            return results.ToArray();
        }

    /// <summary>
    /// 加载 JSON 文件内容
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>JSON 字符串</returns>
    public static string LoadJson(string path)
    {
      var asset = Resources.Load<TextAsset>(path);
      return asset?.text;
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public static void ClearCache()
    {
      _cache.Clear();
    }

    /// <summary>
    /// 移除指定缓存
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <typeparam name="T">资源类型</typeparam>
    public static void RemoveCache<T>(string path) where T : class
    {
      var cacheKey = $"{typeof(T).Name}_{path}";
      _cache.Remove(cacheKey);
    }
  }
}