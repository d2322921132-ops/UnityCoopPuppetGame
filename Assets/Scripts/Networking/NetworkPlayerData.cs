using UnityEngine;
using System;

/// <summary>
/// 网络玩家数据 - 用于同步玩家状态
/// </summary>
[Serializable]
public class NetworkPlayerData
{
    public string playerId;
    public string playerName;
    public Vector3Save position;
    public Vector3Save rotation;
    public Vector2Save input;
    public bool isInteracting;
    public int health;
    public int maxHealth;
    public int level;

    public NetworkPlayerData()
    {
        position = new Vector3Save(Vector3.zero);
        rotation = new Vector3Save(Vector3.zero);
        input = new Vector2Save(Vector2.zero);
        health = 100;
        maxHealth = 100;
        level = 1;
    }
}

[Serializable]
public struct Vector2Save
{
    public float x, y;

    public Vector2Save(Vector2 vector)
    {
        x = vector.x;
        y = vector.y;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}
