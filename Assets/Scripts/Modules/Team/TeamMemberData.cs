using System;
using System.Collections.Generic;
using System.Xml.Serialization;
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
        /// 速度
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// 治疗加成（被动技能加成）
        /// </summary>
        public float healBonus = 0f;

        /// <summary>
        /// 暴击加成（被动技能加成）
        /// </summary>
        public float critBonus = 0f;

        /// <summary>
        /// 最大生命加成（被动技能加成）
        /// </summary>
        public int maxHpBonus = 0;

        /// <summary>
        /// 魅力（交易NPC态度）
        /// </summary>
        public int charisma = 1;

        /// <summary>
        /// 智慧（暴击率）
        /// </summary>
        public int wisdom = 1;

        /// <summary>
        /// 职业
        /// </summary>
        public JobType job = JobType.None;

        /// <summary>
        /// 武器模板ID
        /// </summary>
        public string weaponTemplateId = "";

        /// <summary>
        /// 护甲模板ID
        /// </summary>
        public string armorTemplateId = "";

        /// <summary>
        /// 饰品1模板ID
        /// </summary>
        public string accessory1TemplateId = "";

        /// <summary>
        /// 饰品2模板ID
        /// </summary>
        public string accessory2TemplateId = "";

        /// <summary>
        /// 坐骑模板ID
        /// </summary>
        public string mountTemplateId = "";

        /// <summary>
        /// 被动技能ID列表
        /// </summary>
        public List<string> passiveSkillIds = new();

        /// <summary>
        /// 主动技能ID列表
        /// </summary>
        public List<string> activeSkillIds = new();

        /// <summary>
        /// 终极技能ID
        /// </summary>
        public string ultimateSkillId = "";

        /// <summary>
        /// 是否存活
        /// </summary>
        [XmlIgnore]
        public bool IsAlive => hp > 0;

        /// <summary>
        /// HP百分比
        /// </summary>
        [XmlIgnore]
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
            int actualHeal = (int)(amount * (1 + healBonus));
            hp += actualHeal;
            if (hp > maxHp) hp = maxHp;
        }

        /// <summary>
        /// 升级
        /// </summary>
        public void LevelUp()
        {
            level++;
            maxHp += 5;
            hp = maxHp;
            attack += 2;
            defense += 1;
        }

        /// <summary>
        /// 获取总攻击力（基础 + 职业加成 + 装备加成）
        /// </summary>
        public int GetTotalAttack()
        {
            int bonus = JobSystem.instance.GetJobAttributeBonus(job, AttributeType.Attack, level);
            return attack + bonus;
        }

        /// <summary>
        /// 获取总防御力
        /// </summary>
        public int GetTotalDefense()
        {
            int bonus = JobSystem.instance.GetJobAttributeBonus(job, AttributeType.Defense, level);
            return defense + bonus;
        }

        /// <summary>
        /// 获取总速度
        /// </summary>
        public float GetSpeed()
        {
            float bonus = 0;
            var equipmentBonus = EquipmentSystem.instance.GetTotalEquipmentBonus(this);
            return speed * equipmentBonus.speedMultiplier + bonus;
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