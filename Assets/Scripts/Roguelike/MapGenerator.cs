using System;
using System.Collections.Generic;

namespace Game1
{
    /// <summary>
    /// 地图生成器配置
    /// </summary>
    [Serializable]
    public class MapGeneratorConfig
    {
        public int nodeCount = 10;           // 节点数量
        public int branchFactor = 2;           // 分支系数
        public float specialNodeChance = 0.3f; // 特殊节点概率
        public float bossNodeChance = 0.1f;    // BOSS节点概率
        public float marketNodeChance = 0.2f;  // 市集节点概率
        public int seed;                      // 随机种子
    }

    /// <summary>
    /// 地图生成器
    /// </summary>
    public class MapGenerator
    {
        private MapGeneratorConfig _config;
        private System.Random _random;

        public MapGenerator(MapGeneratorConfig config)
        {
            _config = config ?? new MapGeneratorConfig();
            _random = new System.Random(_config.seed);
        }

        /// <summary>
        /// 生成新地图
        /// </summary>
        public WorldMap Generate()
        {
            var worldMap = new WorldMap();
            var seed = _random.Next().ToString();
            worldMap.Generate(seed);

            // TODO: 实现具体生成算法
            // 1. 生成起始节点
            // 2. 生成中间路径节点
            // 3. 添加分支和特殊节点
            // 4. 生成终点和BOSS节点

            return worldMap;
        }

        /// <summary>
        /// 使用指定种子生成地图
        /// </summary>
        public WorldMap Generate(string seed)
        {
            var config = new MapGeneratorConfig { seed = seed.GetHashCode() };
            var generator = new MapGenerator(config);
            return generator.Generate();
        }

        /// <summary>
        /// 确定地点类型
        /// </summary>
        private LocationType DetermineLocationType(int index, int total)
        {
            float roll = (float)_random.NextDouble();

            if (index == 0) return LocationType.Start;
            if (index == total - 1) return LocationType.Goal;

            if (roll < _config.bossNodeChance) return LocationType.Boss;
            if (roll < _config.bossNodeChance + _config.marketNodeChance) return LocationType.Market;
            if (roll < _config.bossNodeChance + _config.marketNodeChance + _config.specialNodeChance) return LocationType.Dungeon;

            return LocationType.Wilderness;
        }

        /// <summary>
        /// 生成地点名称
        /// </summary>
        private string GenerateLocationName(LocationType type)
        {
            // TODO: 根据类型生成名称
            return type.ToString();
        }

        /// <summary>
        /// 计算旅行时间
        /// </summary>
        private float CalculateTravelTime(LocationType type)
        {
            return type switch
            {
                LocationType.City => 30f,
                LocationType.Wilderness => 20f,
                LocationType.Market => 15f,
                LocationType.Dungeon => 45f,
                LocationType.Boss => 60f,
                _ => 25f,
            };
        }
    }

    /// <summary>
    /// 特质系统 (Roguelike)
    /// </summary>
    [Serializable]
    public class Trait
    {
        public string traitId;
        public string traitName;
        public string description;
        public TraitType type;
        public float value;

        public enum TraitType
        {
            Positive,
            Negative,
            Neutral,
        }
    }

    /// <summary>
    /// 特质集合
    /// </summary>
    public class TraitCollection
    {
        private List<Trait> _traits = new();

        public void AddTrait(Trait trait)
        {
            _traits.Add(trait);
        }

        public void RemoveTrait(string traitId)
        {
            _traits.RemoveAll(t => t.traitId == traitId);
        }

        public float GetTraitBonus(string bonusType)
        {
            float total = 0f;
            foreach (var trait in _traits)
            {
                // TODO: 计算加成
            }
            return total;
        }
    }

    /// <summary>
    /// 遭遇生成器
    /// </summary>
    public class EncounterGenerator
    {
        private System.Random _random;

        public EncounterGenerator(int seed)
        {
            _random = new System.Random(seed);
        }

        /// <summary>
        /// 生成随机遭遇
        /// </summary>
        public IGameEvent GenerateEncounter()
        {
            int roll = _random.Next(100);

            if (roll < 40) // 40% 战斗
            {
                return new CombatEvent
                {
                    enemyCount = _random.Next(1, 4),
                    enemyStrength = _random.Next(10, 50)
                };
            }
            else if (roll < 70) // 30% 交易
            {
                return new TradeEvent();
            }
            else // 30% 随机
            {
                // TODO: 返回随机事件
                return new TradeEvent();
            }
        }
    }
}
