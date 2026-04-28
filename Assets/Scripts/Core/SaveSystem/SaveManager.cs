using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 职能存档管理器
    /// 负责协调多个 ISaveFile 的注册、加载与保存
    /// 不再使用增量/槽位模式，每个职能文件独立读写
    /// </summary>
    public class SaveManager
    {
        private const string SAVE_DIRECTORY = "Save";

        private readonly Dictionary<Type, ISaveFile> _saveFiles = new();
        private ISaveBackend _backend;
        private readonly MigrationManager _migrationManager;

        private float _autoSaveTimer = 0f;
        private float _autoSaveInterval = 1f;

        // ====== 属性 ======

        public ISaveBackend backend => _backend;
        public MigrationManager migrationManager => _migrationManager;
        public string saveDirectory { get; private set; }

        /// <summary>
        /// 存档名称（默认为"Default"，对应 Save/{SaveName}/ 目录）
        /// </summary>
        public string SaveName { get; private set; } = "Default";

        // ====== 构造 ======

        /// <summary>
        /// 构造存档管理器，默认存档名称"Default"，对应 Save/Default/ 目录
        /// </summary>
        public SaveManager()
        {
            _migrationManager = new MigrationManager();
            var dir = GetSaveDirectory();
            _backend = new LocalSaveBackend(dir);
            saveDirectory = dir;
        }

        /// <summary>
        /// 设置存档后端（默认使用本地存储）
        /// </summary>
        public void SetBackend(ISaveBackend backend)
        {
            _backend = backend ?? new LocalSaveBackend(saveDirectory);
        }

        /// <summary>
        /// 设置存档名称，会更新存档目录路径
        /// </summary>
        public void SetSaveName(string saveName)
        {
            SaveName = string.IsNullOrEmpty(saveName) ? "Default" : saveName;
            var dir = GetSaveDirectory();
            saveDirectory = dir;
            if (_backend is LocalSaveBackend local)
            {
                _backend = new LocalSaveBackend(dir);
            }
        }

        // ====== 文件注册 ======

        /// <summary>
        /// 注册一个职能存档文件
        /// </summary>
        public void RegisterFile<T>(T file) where T : class, ISaveFile
        {
            if (file == null)
            {
                Debug.LogError("[SaveManager] RegisterFile: file is null");
                return;
            }

            var type = typeof(T);
            if (_saveFiles.ContainsKey(type))
            {
                Debug.LogWarning($"[SaveManager] File of type {type.Name} already registered, overwriting.");
            }

            _saveFiles[type] = file;
        }

        /// <summary>
        /// 批量注册多个职能存档文件
        /// </summary>
        public void RegisterFiles(params ISaveFile[] files)
        {
            foreach (var file in files)
            {
                if (file == null) continue;
                _saveFiles[file.GetType()] = file;
            }
        }

        /// <summary>
        /// 获取已注册的职能存档文件
        /// </summary>
        public T GetFile<T>() where T : class, ISaveFile
        {
            if (_saveFiles.TryGetValue(typeof(T), out var file))
            {
                return file as T;
            }

            Debug.LogError($"[SaveManager] File of type {typeof(T).Name} not registered.");
            return null;
        }

        // ====== 保存 ======

        /// <summary>
        /// 保存单个职能文件
        /// </summary>
        public void SaveFile<T>() where T : class, ISaveFile
        {
            var file = GetFile<T>();
            if (file == null) return;

            try
            {
                EnsureSaveDirectory();
                var xmlContent = file.ToXml();
                var bytes = Encoding.UTF8.GetBytes(xmlContent);
                _backend.Save(file.FileName, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save {typeof(T).Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存所有已注册的职能文件
        /// </summary>
        public void SaveAll()
        {
            foreach (var kvp in _saveFiles)
            {
                try
                {
                    EnsureSaveDirectory();
                    var file = kvp.Value;
                    var xmlContent = file.ToXml();
                    var bytes = Encoding.UTF8.GetBytes(xmlContent);
                    _backend.Save(file.FileName, bytes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Failed to save {kvp.Key.Name}: {ex.Message}");
                }
            }
        }

        // ====== 加载 ======

        /// <summary>
        /// 加载单个职能文件
        /// </summary>
        public bool LoadFile<T>() where T : class, ISaveFile
        {
            var file = GetFile<T>();
            if (file == null) return false;

            try
            {
                var data = _backend.Load(file.FileName);
                if (data == null || data.Length == 0)
                {
                    Debug.Log($"[SaveManager] No save file found for {typeof(T).Name}, using defaults");
                    return false;
                }

                var xmlString = Encoding.UTF8.GetString(data);
                var doc = new XmlDocument();
                doc.LoadXml(xmlString);

                if (doc.DocumentElement == null)
                {
                    Debug.LogWarning($"[SaveManager] Invalid XML in {file.FileName}");
                    return false;
                }

                // 执行版本迁移（如果需要）
                var migrated = _migrationManager.MigrateFileToLatest(file);

                // 解析到文件实例
                migrated.ParseFromXml(doc.DocumentElement);

                // 如果迁移创建了新实例，替换注册的文件
                if (migrated != file)
                {
                    _saveFiles[typeof(T)] = migrated;
                }

                Debug.Log($"[SaveManager] Loaded {file.FileName} successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Failed to load {typeof(T).Name}: {ex.Message}, using defaults");
                return false;
            }
        }

        /// <summary>
        /// 加载所有已注册的职能文件
        /// </summary>
        public void LoadAll()
        {
            foreach (var kvp in _saveFiles)
            {
                try
                {
                    var fileType = kvp.Key;
                    var file = kvp.Value;

                    var data = _backend.Load(file.FileName);
                    if (data == null || data.Length == 0)
                    {
                        Debug.Log($"[SaveManager] No save file found for {file.FileName}, using defaults");
                        continue;
                    }

                    var xmlString = Encoding.UTF8.GetString(data);
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlString);

                    if (doc.DocumentElement == null)
                    {
                        Debug.LogWarning($"[SaveManager] Invalid XML in {file.FileName}");
                        continue;
                    }

                    // 执行版本迁移
                    var migrated = _migrationManager.MigrateFileToLatest(file);

                    // 解析
                    migrated.ParseFromXml(doc.DocumentElement);

                    // 替换
                    if (migrated != file)
                    {
                        _saveFiles[fileType] = migrated;
                    }

                    Debug.Log($"[SaveManager] Loaded {file.FileName} successfully");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Failed to load {kvp.Key.Name}: {ex.Message}");
                }
            }
        }

        // ====== 云存档 ======

        /// <summary>
        /// 异步保存所有文件到云端
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SaveToCloudAsync()
        {
            try
            {
                // 将所有职能文件打包为一个完整的存档数据
                var combined = new StringBuilder();
                combined.Append("<CloudSaveData>");
                foreach (var kvp in _saveFiles)
                {
                    combined.Append(kvp.Value.ToXml());
                }
                combined.Append("</CloudSaveData>");

                var xmlBytes = Encoding.UTF8.GetBytes(combined.ToString());
                return await _backend.SaveAsync("cloud_save.xml", xmlBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Cloud save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步从云端加载所有文件
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadFromCloudAsync()
        {
            try
            {
                var data = await _backend.LoadAsync("cloud_save.xml");
                if (data == null || data.Length == 0)
                {
                    Debug.Log("[SaveManager] No cloud save found");
                    return false;
                }

                var xmlString = Encoding.UTF8.GetString(data);
                var doc = new XmlDocument();
                doc.LoadXml(xmlString);

                if (doc.DocumentElement == null)
                {
                    Debug.LogWarning("[SaveManager] Invalid cloud save XML");
                    return false;
                }

                // 尝试按职能文件名称解析各文件
                foreach (var kvp in _saveFiles)
                {
                    var file = kvp.Value;
                    var fileNode = doc.DocumentElement.SelectSingleNode(file.FileName.Replace(".xml", ""));
                    if (fileNode != null && fileNode is XmlElement fileElement)
                    {
                        file.ParseFromXml(fileElement);
                    }
                }

                Debug.Log("[SaveManager] Cloud save loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Cloud load failed: {ex.Message}");
                return false;
            }
        }

        // ====== 自动保存 ======

        /// <summary>
        /// Tick（自动存档检测）
        /// </summary>
        public void Tick(float deltaTime)
        {
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                SaveAll();
                _autoSaveTimer = 0f;
            }
        }

        // ====== 工具方法 ======

        private string GetSaveDirectory()
        {
            string exeDir = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(exeDir, SAVE_DIRECTORY, SaveName);
        }

        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
        }
    }
}
