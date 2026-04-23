using System;
using UnityEngine;

namespace Game1
{
    /// <summary>
    /// 队伍成员数据结构
    /// </summary>
    [Serializable]
    public class TeamMemberData
    {
        /// <summary>
        /// 成员唯一ID
        /// </summary>
        public int id;

        /// <summary>
        /// 成员名称
        /// </summary>
        public string name;

        /// <summary>
        /// 等级
        /// </summary>
        public int level = 1;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public int hp = 20;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int maxHp = 20;

        /// <summary>
        /// 攻击力
        /// </summary>
        public int attack = 5;

        /// <summary>
        /// 防御力
        /// </summary>
        public int defense = 3;

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => hp > 0;

        /// <summary>
        /// HP百分比
        /// </summary>
        public float hpPercent => maxHp > 0 ? (float)hp / maxHp : 0;

        public TeamMemberData() { }

        public TeamMemberData(string name, int level = 1, int hp = 20, int attack = 5, int defense = 3)
        {
            this.name = name;
            this.level = level;
            this.hp = hp;
            this.maxHp = hp;
            this.attack = attack;
            this.defense = defense;
        }

        /// <summary>
        /// 造成伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            int actualDamage = Mathf.Max(1, damage - defense);
            hp -= actualDamage;
            if (hp < 0) hp = 0;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            hp += amount;
            if (hp > maxHp) hp = maxHp;
        }

        /// <summary>
        /// 升级
        /// </summary>
        public void LevelUp()
        {
            level++;
            maxHp += 5;
            hp = maxHp;  // 升级满血
            attack += 2;
            defense += 1;
        }

        /// <summary>
        /// 获取战斗力
        /// </summary>
        public int GetCombatPower()
        {
            return attack + defense + maxHp / 2;
        }
    }
}