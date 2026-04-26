using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game1.GamePlay
{
    /// <summary>
    /// Simulated player stats (mirrors PlayerActor.Stats)
    /// </summary>
    [Serializable]
    public class AStats
    {
        public int maxHp = 20;
        public int currentHp = 20;
        public int attack = 3;
        public int defense = 5;
        public float speed = 1f;
        public float critChance = 0.1f;
        public float dodgeChance = 0.05f;
        public float critDamageMultiplier = 1.5f;

        public void Reset() => currentHp = maxHp;
        public bool IsDead => currentHp <= 0;
    }

    /// <summary>
    /// Simulated carry items (mirrors PlayerActor.CarryItems)
    /// </summary>
    [Serializable]
    public class ACarryItems
    {
        public int gold = 0;
        public List<string> ownedModuleIds = new();
        public int maxCapacity = 100;
        public int currentLoad = 0;
    }

    /// <summary>
    /// Simulated travel state (mirrors PlayerActor.TravelState)
    /// </summary>
    [Serializable]
    public class ATravelState
    {
        public enum State { Idle, Traveling, Arrived, EventPending }
        public State currentState = State.Idle;
        public float progress = 0f;
        public float realTimeRequired = 10f;
        public string currentLocationId = "";
        public string nextLocationId = "";
    }

    /// <summary>
    /// Simulated player module interface
    /// </summary>
    public interface ASimulatedModule
    {
        string moduleId { get; }
        string moduleName { get; }
        string GetBonus(string bonusType);
        void Tick(float deltaTime);
        void OnActivate();
        void OnDeactivate();
    }

    /// <summary>
    /// Simulated player (mirrors PlayerActor with procedural generation)
    /// </summary>
    [Serializable]
    public class ASimulatedPlayer
    {
        #region Identity
        public string id;
        public string actorName;
        public int level;
        #endregion

        #region State
        public AStats stats = new();
        public ACarryItems carryItems = new();
        public ATravelState travelState = new();
        public List<ASimulatedModule> modules = new();
        #endregion

        #region RNG
        private System.Random _rng;
        private int _seed;
        #endregion

        // Events
        public event System.Action<int> onLevelUp;
        public event System.Action onDeath;
        public event System.Action<int> onGoldChanged;

        #region Constructors
        public ASimulatedPlayer()
        {
            id = Guid.NewGuid().ToString();
            actorName = "行者";
            level = 1;
            _seed = Environment.TickCount;
            _rng = new System.Random(_seed);
        }

        public ASimulatedPlayer(int seed)
        {
            id = Guid.NewGuid().ToString();
            actorName = "行者";
            level = 1;
            _seed = seed;
            _rng = new System.Random(seed);
        }
        #endregion

        #region Procedural Generation
        /// <summary>
        /// Generate player with random stats based on seed
        /// </summary>
        public void Generate(int seed, int targetLevel = 1)
        {
            _seed = seed;
            _rng = new System.Random(seed);
            level = Mathf.Max(1, targetLevel);

            // Scale stats with level
            float levelScale = 1f + (level - 1) * 0.1f;
            stats.maxHp = Mathf.RoundToInt(20 * levelScale);
            stats.currentHp = stats.maxHp;
            stats.attack = Mathf.RoundToInt(3 * levelScale);
            stats.defense = Mathf.RoundToInt(5 * levelScale);
            stats.critChance = 0.1f + Mathf.Min(level * 0.01f, 0.2f);
            stats.dodgeChance = 0.05f + Mathf.Min(level * 0.005f, 0.15f);

            // Random starting gold
            carryItems.gold = _rng.Next(0, 100 * level);
        }

        /// <summary>
        /// Generate player with specific stats
        /// </summary>
        public void Generate(int seed, int hp, int attack, int defense, int gold)
        {
            Generate(seed, 1);
            stats.maxHp = hp;
            stats.currentHp = hp;
            stats.attack = attack;
            stats.defense = defense;
            carryItems.gold = gold;
        }
        #endregion

        #region Bonus Calculation
        public float GetTotalBonus(string bonusType)
        {
            float total = 0f;
            foreach (var module in modules)
            {
                if (float.TryParse(module.GetBonus(bonusType), out float value))
                    total += value;
            }
            return total;
        }
        #endregion

        #region Combat
        public void TakeDamage(int damage)
        {
            // Apply dodge
            if (_rng.NextDouble() < stats.dodgeChance)
            {
                Debug.Log($"[{actorName}] Dodged the attack!");
                return;
            }

            // Apply damage (minimum 1)
            int actualDamage = Mathf.Max(1, damage - stats.defense);
            stats.currentHp -= actualDamage;
            Debug.Log($"[{actorName}] Took {actualDamage} damage. HP: {stats.currentHp}/{stats.maxHp}");

            if (stats.currentHp <= 0)
            {
                stats.currentHp = 0;
                onDeath?.Invoke();
            }
        }

        public int CalculateDamage(out bool isCrit)
        {
            isCrit = _rng.NextDouble() < stats.critChance;
            int baseDamage = stats.attack;
            return isCrit ? Mathf.RoundToInt(baseDamage * stats.critDamageMultiplier) : baseDamage;
        }

        public void Heal(int amount)
        {
            stats.currentHp = Mathf.Min(stats.maxHp, stats.currentHp + amount);
        }

        public void Revive()
        {
            stats.currentHp = stats.maxHp;
            Debug.Log($"[{actorName}] Revived!");
        }
        #endregion

        #region Leveling
        public void AddExp(int exp)
        {
            int expForNextLevel = CalculateExpForNextLevel();
            int totalExp = exp;

            while (totalExp >= expForNextLevel)
            {
                totalExp -= expForNextLevel;
                LevelUp();
                expForNextLevel = CalculateExpForNextLevel();
            }
        }

        public void LevelUp()
        {
            level++;
            float levelScale = 1f + (level - 1) * 0.15f;
            stats.maxHp = Mathf.RoundToInt(20 * levelScale);
            stats.attack = Mathf.RoundToInt(3 * levelScale);
            stats.defense = Mathf.RoundToInt(5 * levelScale);
            stats.currentHp = stats.maxHp; // Full heal on level up

            Debug.Log($"[{actorName}] Level Up! Now level {level}");
            onLevelUp?.Invoke(level);
        }

        public int CalculateExpForNextLevel()
        {
            return Mathf.RoundToInt(100 * Mathf.Pow(level, 1.5f));
        }
        #endregion

        #region Gold
        public void AddGold(int amount)
        {
            carryItems.gold += amount;
            onGoldChanged?.Invoke(carryItems.gold);
        }

        public bool SpendGold(int amount)
        {
            if (carryItems.gold >= amount)
            {
                carryItems.gold -= amount;
                onGoldChanged?.Invoke(carryItems.gold);
                return true;
            }
            return false;
        }
        #endregion

        #region RNG Access
        public int RandomInt(int min, int max) => _rng.Next(min, max);
        public float RandomFloat() => (float)_rng.NextDouble();
        public bool RandomChance(float probability) => _rng.NextDouble() < probability;
        public int Seed => _seed;
        #endregion

        #region Module Management
        public void AddModule(ASimulatedModule module)
        {
            modules.Add(module);
            module.OnActivate();
        }

        public void RemoveModule(string moduleId)
        {
            modules.RemoveAll(m =>
            {
                if (m.moduleId == moduleId)
                {
                    m.OnDeactivate();
                    return true;
                }
                return false;
            });
        }

        public T GetModule<T>() where T : class, ASimulatedModule
        {
            foreach (var module in modules)
                if (module is T t) return t;
            return null;
        }
        #endregion

        #region Tick
        public void Tick(float deltaTime)
        {
            foreach (var module in modules)
                module.Tick(deltaTime);
        }
        #endregion

        #region State
        public bool IsDead => stats.currentHp <= 0;
        public bool IsTraveling => travelState.currentState == ATravelState.State.Traveling;
        public float TravelProgress => travelState.progress;
        #endregion
    }
}