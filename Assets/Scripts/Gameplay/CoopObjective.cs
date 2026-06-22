using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 协作目标系统 - 管理双人协作任务和目标
/// </summary>
public class CoopObjective : MonoBehaviour
{
    [Header("目标配置")]
    [SerializeField] private string objectiveId;
    [SerializeField] private string objectiveName;
    [SerializeField] private string description;
    [SerializeField] private ObjectiveType type;
    [SerializeField] private int requiredAmount = 1;
    [SerializeField] private int rewardExp = 50;
    [SerializeField] private int rewardGold = 100;

    [Header("目标状态")]
    [SerializeField] private ObjectiveState state = ObjectiveState.Inactive;
    [SerializeField] private int currentAmount = 0;

    public string ObjectiveId => objectiveId;
    public string ObjectiveName => objectiveName;
    public ObjectiveState State => state;
    public float Progress => requiredAmount > 0 ? (float)currentAmount / requiredAmount : 0f;

    public event Action<CoopObjective> OnObjectiveStarted;
    public event Action<CoopObjective> OnObjectiveUpdated;
    public event Action<CoopObjective> OnObjectiveCompleted;

    private List<string> participatingPlayers = new List<string>();

    /// <summary>
    /// 激活目标
    /// </summary>
    public void ActivateObjective()
    {
        if (state != ObjectiveState.Inactive) return;

        state = ObjectiveState.Active;
        currentAmount = 0;
        participatingPlayers.Clear();

        OnObjectiveStarted?.Invoke(this);
        Debug.Log($"[CoopObjective] 目标激活: {objectiveName}");
    }

    /// <summary>
    /// 更新目标进度
    /// </summary>
    public void UpdateProgress(int amount, string playerId)
    {
        if (state != ObjectiveState.Active) return;

        currentAmount += amount;
        currentAmount = Mathf.Clamp(currentAmount, 0, requiredAmount);

        if (!participatingPlayers.Contains(playerId))
        {
            participatingPlayers.Add(playerId);
        }

        OnObjectiveUpdated?.Invoke(this);

        if (currentAmount >= requiredAmount)
        {
            CompleteObjective();
        }
    }

    /// <summary>
    /// 完成目标
    /// </summary>
    private void CompleteObjective()
    {
        state = ObjectiveState.Completed;

        // 发放奖励
        foreach (var playerId in participatingPlayers)
        {
            // 实际实现: 向每个参与玩家发放奖励
            Debug.Log($"[CoopObjective] 向玩家 {playerId} 发放奖励: {rewardExp} 经验, {rewardGold} 金币");
        }

        OnObjectiveCompleted?.Invoke(this);
        Debug.Log($"[CoopObjective] 目标完成: {objectiveName}");
    }

    /// <summary>
    /// 重置目标
    /// </summary>
    public void ResetObjective()
    {
        state = ObjectiveState.Inactive;
        currentAmount = 0;
        participatingPlayers.Clear();
    }
}

public enum ObjectiveType
{
    Collect,        // 收集物品
    Defeat,         // 击败敌人
    Reach,          // 到达地点
    Protect,        // 保护目标
    Puzzle,         // 解谜
    Cooperative     // 需要协作完成的任务
}

public enum ObjectiveState
{
    Inactive,       // 未激活
    Active,         // 进行中
    Completed,      // 已完成
    Failed          // 失败
}
