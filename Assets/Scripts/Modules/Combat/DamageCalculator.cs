using UnityEngine;

namespace Game1.Modules.Combat
{
    /// <summary>
    /// 伤害计算器
    /// 统一游戏内所有伤害计算公式，消除重复逻辑
    /// 
    /// 公式：damage = max(1, floor(attack * (1 - defense / (defense + 100))))
    /// 防御提供递减减伤：100防御 = 50%减伤，200防御 ≈ 67%减伤
    /// 暴击判定由调用方处理（参考critMultiplier在调用方乘算）
    /// </summary>
    public static class DamageCalculator
    {
        private const float DEFENSE_DENOMINATOR = 100f;
        private const int MINIMUM_DAMAGE = 1;

        /// <summary>
        /// 计算攻击对防御方造成的伤害
        /// </summary>
        /// <param name="attack">攻击力</param>
        /// <param name="defense">防御力</param>
        /// <param name="critChance">暴击率 (0~1)</param>
        /// <param name="critMultiplier">暴击倍率（仅用于判定逻辑，倍数由调用方应用）</param>
        /// <param name="isCrit">是否触发暴击</param>
        /// <returns>基础伤害值（不包含暴击加成）</returns>
        public static int CalculateDamage(int attack, int defense, float critChance, float critMultiplier, out bool isCrit)
        {
            // 百分比减伤（递减公式）
            float reduction = defense / (defense + DEFENSE_DENOMINATOR);
            int baseDamage = Mathf.Max(MINIMUM_DAMAGE, Mathf.FloorToInt(attack * (1f - reduction)));

            // 暴击判定（使用暴击率而非等级）
            if (critChance > 0f && Random.value < critChance)
            {
                isCrit = true;
                return baseDamage; // 暴击倍率由调用方应用
            }

            isCrit = false;
            return baseDamage;
        }

        /// <summary>
        /// 计算包含暴击加成的最终伤害
        /// </summary>
        /// <param name="attack">攻击力</param>
        /// <param name="defense">防御力</param>
        /// <param name="critChance">暴击率</param>
        /// <param name="critMultiplier">暴击倍率</param>
        /// <returns>最终伤害（可能含暴击加成）</returns>
        public static int CalculateFinalDamage(int attack, int defense, float critChance, float critMultiplier)
        {
            int baseDamage = CalculateDamage(attack, defense, critChance, critMultiplier, out bool isCrit);

            if (isCrit)
            {
                return Mathf.FloorToInt(baseDamage * critMultiplier);
            }

            return baseDamage;
        }

        /// <summary>
        /// 快速估算能否战胜（基于DPS比较）
        /// </summary>
        /// <param name="playerDamage">玩家攻击力</param>
        /// <param name="playerArmor">玩家防御力</param>
        /// <param name="playerHp">玩家血量</param>
        /// <param name="enemyDamage">敌攻击力</param>
        /// <param name="enemyArmor">敌防御力</param>
        /// <param name="enemyHp">敌血量</param>
        /// <param name="threshold">胜率阈值（默认0.8）</param>
        /// <returns>是否可能战胜</returns>
        public static bool CanVictory(int playerDamage, int playerArmor, int playerHp,
                                      int enemyDamage, int enemyArmor, int enemyHp,
                                      float threshold = 0.8f)
        {
            float playerDps = (float)playerDamage / Mathf.Max(1, enemyArmor);
            float enemyDps = (float)enemyDamage / Mathf.Max(1, playerArmor);

            float playerEffectiveHp = playerHp / Mathf.Max(0.1f, enemyDps);
            float enemyEffectiveHp = enemyHp / Mathf.Max(0.1f, playerDps);

            return playerEffectiveHp > enemyEffectiveHp * threshold;
        }
    }
}
