namespace Game1.Modules.Combat
{
    /// <summary>
    /// 战斗系统接口 (用于VContainer DI)
    /// </summary>
    public interface ICombatSystem
    {
        /// <summary>
        /// 执行战斗
        /// </summary>
        CombatResult ExecuteCombat(PlayerActor player, int enemyHp, int enemyArmor, int enemyDamage, string enemyName = "敌人");

        /// <summary>
        /// 计算伤害
        /// </summary>
        int CalculateDamage(int attack, int defense);

        /// <summary>
        /// 快速计算能否战胜
        /// </summary>
        bool CanVictory(int playerDamage, int playerArmor, int playerHp, int enemyDamage, int enemyArmor, int enemyHp);
    }
}
