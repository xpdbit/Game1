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

        // 探索配置
        public float explorationTime;  // 探索时间（秒）
        public List<string> discoveredItems = new();  // 可发现物品
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
        private int _maxNodeIndex;  // 最大到达节点（用于限制回头路）

        public string seed => _seed;
        public int currentNodeIndex => _currentNodeIndex;
        public int maxNodeIndex => _maxNodeIndex;
        public Location currentLocation => GetLocationByIndex(_currentNodeIndex);
        public Location nextLocation => GetLocationByIndex(_currentNodeIndex + 1);
        public int totalNodes => _path.Count;

        public WorldMap()
        {
            _seed = "";
            _currentNodeIndex = 0;
            _maxNodeIndex = 0;
        }

        /// <summary>
        /// 生成新地图
        /// </summary>
        public void Generate(string seed)
        {
            _seed = seed;
            _locations.Clear();
            _path.Clear();
            _currentNodeIndex = 0;
            _maxNodeIndex = 0;

            // 使用随机数生成器
            var random = new System.Random(seed.GetHashCode());

            // 生成固定数量的节点
            int nodeCount = 10 + random.Next(5);  // 10-15个节点
            Location previousNode = null;

            for (int i = 0; i < nodeCount; i++)
            {
                var nodeType = DetermineNodeType(i, nodeCount, random);
                var location = GenerateLocation(i, nodeType, random);

                // 设置连接
                if (previousNode != null)
                {
                    previousNode.connections.Add(location.id);
                    location.connections.Add(previousNode.id);
                }

                _locations[location.id] = location;
                _path.Add(location);
                previousNode = location;

                // 设置最大奖励随距离增加
                location.baseReward = 10 + i * 5 + random.Next(10);
                location.travelTime = 5f + random.Next(10);
            }
        }

        /// <summary>
        /// 确定节点类型
        /// </summary>
        private LocationType DetermineNodeType(int index, int total, System.Random random)
        {
            if (index == 0) return LocationType.Start;
            if (index == total - 1) return LocationType.Goal;

            int roll = random.Next(100);
            if (roll < 15) return LocationType.City;
            if (roll < 30) return LocationType.Market;
            if (roll < 50) return LocationType.Wilderness;
            if (roll < 75) return LocationType.Dungeon;
            return LocationType.Boss;
        }

        /// <summary>
        /// 生成单个地点
        /// </summary>
        private Location GenerateLocation(int index, LocationType type, System.Random random)
        {
            string id = $"loc_{index}_{type}";
            string nameId = $"Map.Location.{type}_{index}";

            var location = new Location
            {
                id = id,
                locationName = nameId,
                type = type,
                nodeIndex = index,
                hasEvent = random.Next(100) < 40,  // 40%概率有事件
                eventId = DetermineEventId(type, random),
                eventChance = 0.5f + random.Next(50) / 100f,
                explorationTime = 2f + random.Next(5)
            };

            return location;
        }

        /// <summary>
        /// 根据地点类型确定事件ID
        /// </summary>
        private string DetermineEventId(LocationType type, System.Random random)
        {
            return type switch
            {
                LocationType.City => "npc_001",
                LocationType.Market => "trade_001",
                LocationType.Dungeon => "combat_001",
                LocationType.Boss => "combat_003",
                LocationType.Wilderness => random.Next(2) == 0 ? "combat_002" : "trade_001",
                _ => null
            };
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
                if (_currentNodeIndex > _maxNodeIndex)
                    _maxNodeIndex = _currentNodeIndex;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移动到指定位置（用于路径选择）
        /// </summary>
        public bool MoveToLocation(string locationId)
        {
            var location = GetLocation(locationId);
            if (location == null) return false;

            // 只能移动到已连接的位置
            var current = currentLocation;
            if (current == null || !current.connections.Contains(locationId))
                return false;

            // 更新索引
            _currentNodeIndex = location.nodeIndex;
            if (_currentNodeIndex > _maxNodeIndex)
                _maxNodeIndex = _currentNodeIndex;

            return true;
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
        /// 检查是否已探索到指定节点
        /// </summary>
        public bool IsExplored(int nodeIndex)
        {
            return nodeIndex <= _maxNodeIndex;
        }

        /// <summary>
        /// 获取已探索的节点列表
        /// </summary>
        public List<Location> GetExploredLocations()
        {
            var result = new List<Location>();
            for (int i = 0; i <= _maxNodeIndex && i < _path.Count; i++)
            {
                result.Add(_path[i]);
            }
            return result;
        }

        /// <summary>
        /// 重置地图
        /// </summary>
        public void Reset()
        {
            _currentNodeIndex = 0;
            _maxNodeIndex = 0;
        }
    }
}