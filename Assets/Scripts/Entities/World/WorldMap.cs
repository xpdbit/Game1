using System;
using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 地点类型
    /// </summary>
    public enum LocationType
    {
        Start,      // 起点
        City,       // 古城
        Wilderness, // 荒野
        Market,     // 集市
        Dungeon,    // 副本
        Boss,       // BOSS
        Goal,       // 终点
    }

    /// <summary>
    /// 地点节点
    /// </summary>
    [Serializable]
    public class Location
    {
        public string id;
        public string locationName;
        public LocationType type;
        public List<string> connections = new(); // 连接的节点ID
        public int nodeIndex; // 在路径上的顺序

        // 事件配置
        public bool hasEvent;
        public string eventId;
        public float eventChance; // 触发概率 0~1

        // 收益配置
        public int baseReward;
        public float travelTime; // 基础旅行时间(秒)
    }

    /// <summary>
    /// 世界地图
    /// </summary>
    public class WorldMap
    {
        private Dictionary<string, Location> _locations = new();
        private List<Location> _path = new(); // 主路径顺序
        private string _seed;
        private int _currentNodeIndex;

        public string seed => _seed;
        public int currentNodeIndex => _currentNodeIndex;
        public Location currentLocation => GetLocationByIndex(_currentNodeIndex);
        public Location nextLocation => GetLocationByIndex(_currentNodeIndex + 1);
        public int totalNodes => _path.Count;

        public WorldMap()
        {
            _seed = "";
            _currentNodeIndex = 0;
        }

        /// <summary>
        /// 生成新地图
        /// </summary>
        public void Generate(string seed)
        {
            _seed = seed;
            _locations.Clear();
            _path.Clear();
            // TODO: 实现地图生成算法
        }

        /// <summary>
        /// 获取地点
        /// </summary>
        public Location GetLocation(string id)
        {
            return _locations.TryGetValue(id, out var loc) ? loc : null;
        }

        /// <summary>
        /// 按索引获取地点
        /// </summary>
        public Location GetLocationByIndex(int index)
        {
            if (index >= 0 && index < _path.Count)
                return _path[index];
            return null;
        }

        /// <summary>
        /// 前进到下一个节点
        /// </summary>
        public bool MoveToNext()
        {
            if (_currentNodeIndex < _path.Count - 1)
            {
                _currentNodeIndex++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取当前节点的连接节点
        /// </summary>
        public List<Location> GetCurrentConnections()
        {
            var current = currentLocation;
            if (current == null) return new List<Location>();

            var result = new List<Location>();
            foreach (var connId in current.connections)
            {
                var loc = GetLocation(connId);
                if (loc != null)
                    result.Add(loc);
            }
            return result;
        }

        /// <summary>
        /// 重置地图
        /// </summary>
        public void Reset()
        {
            _currentNodeIndex = 0;
        }
    }
}
