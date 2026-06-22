using UnityEngine;
using System;

/// <summary>
/// 玩家属性系统 - 管理玩家的生命值、经验值、等级等属性
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("基础属性")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int defense = 5;
    [SerializeField] private float moveSpeed = 5f;

    [Header("经验值曲线")]
    [SerializeField] private AnimationCurve expCurve = AnimationCurve.EaseInOut(0, 100, 50, 5000);

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Level => level;
    public int CurrentExp => currentExp;
    public int AttackPower => attackPower;
    public int Defense => defense;
    public float MoveSpeed => moveSpeed;

    public float HealthPercentage => (float)currentHealth / maxHealth;
    public float ExpPercentage => (float)currentExp / GetExpForNextLevel();

    public event Action<int> OnHealthChanged;
    public event Action<int> OnLevelUp;
    public event Action<int> OnExpGained;
    public event Action OnDeath;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// 添加经验值
    /// </summary>
    public void AddExperience(int exp)
    {
        currentExp += exp;
        OnExpGained?.Invoke(exp);

        // 检查升级
        while (currentExp >= GetExpForNextLevel())
        {
            currentExp -= GetExpForNextLevel();
            LevelUp();
        }
    }

    /// <summary>
    /// 升级
    /// </summary>
    private void LevelUp()
    {
        level++;

        // 提升属性
        maxHealth += 20;
        currentHealth = maxHealth;
        attackPower += 3;
        defense += 2;
        moveSpeed += 0.1f;

        OnLevelUp?.Invoke(level);
        Debug.Log($"[PlayerStats] 升级！当前等级: {level}");
    }

    /// <summary>
    /// 获取下一级所需经验值
    /// </summary>
    public int GetExpForNextLevel()
    {
        return Mathf.RoundToInt(expCurve.Evaluate(level));
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerStats] 玩家死亡");
        OnDeath?.Invoke();
    }

    /// <summary>
    /// 复活
    /// </summary>
    public void Revive()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// 从存档数据加载
    /// </summary>
    public void LoadFromSave(GameSaveData saveData)
    {
        if (saveData == null) return;

        level = saveData.playerLevel;
        currentExp = saveData.currentExp;

        // 根据等级重新计算属性
        maxHealth = 100 + (level - 1) * 20;
        currentHealth = maxHealth;
        attackPower = 10 + (level - 1) * 3;
        defense = 5 + (level - 1) * 2;
    }
}
