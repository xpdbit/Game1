using System.Collections.Generic;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 存档版本迁移处理器接口（支持按职能文件独立迁移）
    /// </summary>
    public interface IMigrationHandler
    {
        /// <summary>
        /// 处理迁移的目标版本
        /// </summary>
        int TargetVersion { get; }

        /// <summary>
        /// 从哪个版本迁移过来
        /// </summary>
        int SourceVersion { get; }

        /// <summary>
        /// 目标文件名（用于按职能文件迁移）
        /// </summary>
        string TargetFileName { get; }

        /// <summary>
        /// 执行迁移
        /// </summary>
        /// <param name="saveFile">要迁移的存档文件数据</param>
        /// <returns>迁移后的存档文件数据</returns>
        ISaveFile Migrate(ISaveFile saveFile);
    }

    /// <summary>
    /// 存档版本迁移管理器（支持按职能文件独立迁移）
    /// </summary>
    public class MigrationManager
    {
        private readonly List<IMigrationHandler> _handlers = new();

        /// <summary>
        /// 当前存档版本（最新版本号）
        /// </summary>
        public int CurrentVersion { get; private set; } = 1;

        /// <summary>
        /// 注册迁移处理器
        /// </summary>
        public void RegisterHandler(IMigrationHandler handler)
        {
            if (handler == null) return;

            var insertIndex = _handlers.FindIndex(h => h.SourceVersion >= handler.SourceVersion);
            if (insertIndex < 0)
                _handlers.Add(handler);
            else
                _handlers.Insert(insertIndex, handler);

            if (handler.TargetVersion > CurrentVersion)
                CurrentVersion = handler.TargetVersion;
        }

        /// <summary>
        /// 迁移单个存档文件到最新版本
        /// </summary>
        /// <param name="saveFile">存档文件</param>
        /// <returns>迁移后的存档文件，如果无需迁移则返回原对象</returns>
        public ISaveFile MigrateFileToLatest(ISaveFile saveFile)
        {
            if (saveFile == null) return null;

            var currentVersion = saveFile.Version;
            if (currentVersion >= CurrentVersion)
                return saveFile;

            var result = saveFile;
            var fileName = saveFile.FileName;

            foreach (var handler in _handlers)
            {
                if (handler.TargetFileName == fileName && handler.SourceVersion == currentVersion)
                {
                    result = handler.Migrate(result);
                    currentVersion = handler.TargetVersion;
                    Debug.Log($"[MigrationManager] Migrated {fileName} from v{handler.SourceVersion} to v{handler.TargetVersion}");

                    if (currentVersion >= CurrentVersion)
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 检测存档文件版本
        /// </summary>
        public int DetectVersion(ISaveFile saveFile)
        {
            return saveFile?.Version ?? 0;
        }
    }
}
