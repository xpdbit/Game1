using System.Collections.Generic;
using UnityEngine;

namespace Game1.Events.Editor
{
    /// <summary>
    /// 战斗节点 - 触发与敌人的战斗
    /// </summary>
    [System.Serializable]
    public class CombatNode : EventTreeNodeBase
    {
        [SerializeField] private string _enemyId;
        [SerializeField] private bool _isBoss;
        [SerializeField] private int _difficultyLevel = 1;
        [SerializeField] private bool _allowFlee = true;
        [SerializeField] private string _victoryNodeId;
        [SerializeField] private string _defeatNodeId;
        [SerializeField] private string _fleeNodeId;
        [SerializeField] private List<CombatReward> _additionalRewards = new();

        public string enemyId
        {
            get => _enemyId;
            set => _enemyId = value;
        }

        public bool isBoss
        {
            get => _isBoss;
            set => _isBoss = value;
        }

        public int difficultyLevel
        {
            get => _difficultyLevel;
            set => _difficultyLevel = Mathf.Max(1, value);
        }

        public bool allowFlee
        {
            get => _allowFlee;
            set => _allowFlee = value;
        }

        public string victoryNodeId
        {
            get => _victoryNodeId;
            set => _victoryNodeId = value;
        }

        public string defeatNodeId
        {
            get => _defeatNodeId;
            set => _defeatNodeId = value;
        }

        public string fleeNodeId
        {
            get => _fleeNodeId;
            set => _fleeNodeId = value;
        }

        public List<CombatReward> additionalRewards
        {
            get => _additionalRewards;
            set => _additionalRewards = value;
        }

        public override string NodeType => "Combat";

        /// <summary>
        /// 获取战斗结果分支连接的节点
        /// </summary>
        public override List<EventTreeNodeBase> GetOutputNodes()
        {
            var nodes = new List<EventTreeNodeBase>();
            // 包含胜利、失败、逃跑三个分支
            return nodes;
        }

        public override bool Validate(out string errorMessage)
        {
            if (!base.Validate(out errorMessage))
                return false;

            if (string.IsNullOrEmpty(_enemyId))
            {
                errorMessage = "CombatNode的enemyId不能为空";
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 战斗奖励扩展
    /// </summary>
    [System.Serializable]
    public class CombatReward
    {
        [SerializeField] private string _type;
        [SerializeField] private int _value;
        [SerializeField] private float _chance = 1f;

        public string type
        {
            get => _type;
            set => _type = value;
        }

        public int value
        {
            get => _value;
            set => _value = value;
        }

        public float chance
        {
            get => _chance;
            set => _chance = Mathf.Clamp01(value);
        }
    }
}
