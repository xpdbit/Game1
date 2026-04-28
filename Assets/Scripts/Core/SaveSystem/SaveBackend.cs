using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 存档后端接口（支持同步和异步操作）
    /// </summary>
    public interface ISaveBackend
    {
        /// <summary>异步保存存档</summary>
        Task<bool> SaveAsync(string slotId, byte[] data);
        /// <summary>同步保存存档</summary>
        bool Save(string slotId, byte[] data);

        /// <summary>异步加载存档</summary>
        Task<byte[]> LoadAsync(string slotId);
        /// <summary>同步加载存档</summary>
        byte[] Load(string slotId);

        /// <summary>异步检查存档是否存在</summary>
        Task<bool> ExistsAsync(string slotId);
        /// <summary>同步检查存档是否存在</summary>
        bool Exists(string slotId);

        /// <summary>异步删除存档</summary>
        Task<bool> DeleteAsync(string slotId);
        /// <summary>同步删除存档</summary>
        bool Delete(string slotId);
    }

    /// <summary>
    /// 本地存档后端（默认实现）
    /// </summary>
    public class LocalSaveBackend : ISaveBackend
    {
        private readonly string _basePath;

        public LocalSaveBackend(string basePath)
        {
            _basePath = basePath;
        }

        // ====== 同步方法 ======

        public bool Save(string slotId, byte[] data)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Save failed: {ex.Message}");
                return false;
            }
        }

        public byte[] Load(string slotId)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                if (!File.Exists(path))
                    return null;

                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Load failed: {ex.Message}");
                return null;
            }
        }

        public bool Exists(string slotId)
        {
            var path = Path.Combine(_basePath, slotId);
            return File.Exists(path);
        }

        public bool Delete(string slotId)
        {
            try
            {
                var path = Path.Combine(_basePath, slotId);
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalSaveBackend] Delete failed: {ex.Message}");
                return false;
            }
        }

        // ====== 异步方法（委托给同步实现） ======

        public async Task<bool> SaveAsync(string slotId, byte[] data)
        {
            return await Task.Run(() => Save(slotId, data));
        }

        public async Task<byte[]> LoadAsync(string slotId)
        {
            return await Task.Run(() => Load(slotId));
        }

        public async Task<bool> ExistsAsync(string slotId)
        {
            return await Task.Run(() => Exists(slotId));
        }

        public async Task<bool> DeleteAsync(string slotId)
        {
            return await Task.Run(() => Delete(slotId));
        }
    }
}
