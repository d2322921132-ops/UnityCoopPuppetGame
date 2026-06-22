using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fusion 玩家生成器 - 处理玩家进入/离开房间时的角色生成与销毁
/// SDK 导入后继承 SimulationBehaviour 并实现 ISpawned/IDespawned 接口
/// </summary>
public class FusionPlayerSpawner : MonoBehaviour // : SimulationBehaviour, ISpawned, IPlayerJoined, IPlayerLeft
{
    [Header("玩家预制体")]
    [SerializeField] private GameObject playerPrefab;

    [Header("生成配置")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private bool autoSpawnOnStart = true;

    [Header("引用")]
    [SerializeField] private FusionNetworkManager networkManager;

    // 玩家字典: PlayerRef -> NetworkPlayer
    private Dictionary<int, NetworkPlayer> spawnedPlayers = new Dictionary<int, NetworkPlayer>();

    public IReadOnlyDictionary<int, NetworkPlayer> SpawnedPlayers => spawnedPlayers;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FusionNetworkManager.Instance;
    }

    private void Start()
    {
        if (autoSpawnOnStart && networkManager != null)
        {
            networkManager.OnConnected += OnNetworkConnected;
            networkManager.OnDisconnected += OnNetworkDisconnected;
        }
    }

    /// <summary>
    /// 网络连接成功回调
    /// </summary>
    private void OnNetworkConnected()
    {
        Debug.Log("[FusionPlayerSpawner] 网络已连接，准备生成玩家");
        // SDK 导入后由 Fusion 自动调用 PlayerJoined
    }

    private void OnNetworkDisconnected()
    {
        Debug.Log("[FusionPlayerSpawner] 网络已断开，清理玩家");
        ClearAllPlayers();
    }

    /// <summary>
    /// 玩家加入房间 - SDK 导入后由 Fusion 自动调用
    /// </summary>
    public void PlayerJoined(int playerId, bool isLocal)
    {
        Debug.Log($"[FusionPlayerSpawner] 玩家加入: ID={playerId}, Local={isLocal}");

        if (spawnedPlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"[FusionPlayerSpawner] 玩家 {playerId} 已存在，跳过生成");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(playerId);
        Quaternion spawnRot = Quaternion.identity;

        GameObject playerObj;

        // SDK 导入后使用 Fusion 网络生成:
        // var networkObject = Runner.Spawn(playerPrefab, spawnPos, spawnRot, isLocal ? Runner.LocalPlayer : null);
        // playerObj = networkObject.gameObject;

        // 模拟模式
        playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);

        NetworkPlayer player = playerObj.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            player = playerObj.AddComponent<NetworkPlayer>();
        }

        player.Initialize(playerId, $"Player_{playerId}", isLocal);
        spawnedPlayers[playerId] = player;

        Debug.Log($"[FusionPlayerSpawner] 玩家生成完成: {playerObj.name} at {spawnPos}");
    }

    /// <summary>
    /// 玩家离开房间 - SDK 导入后由 Fusion 自动调用
    /// </summary>
    public void PlayerLeft(int playerId)
    {
        Debug.Log($"[FusionPlayerSpawner] 玩家离开: ID={playerId}");

        if (spawnedPlayers.TryGetValue(playerId, out NetworkPlayer player))
        {
            spawnedPlayers.Remove(playerId);

            // SDK 导入后:
            // if (player.TryGetComponent<NetworkObject>(out var netObj))
            //     Runner.Despawn(netObj);

            Destroy(player.gameObject);
        }
    }

    /// <summary>
    /// 获取本地玩家
    /// </summary>
    public NetworkPlayer GetLocalPlayer()
    {
        foreach (var kvp in spawnedPlayers)
        {
            if (kvp.Value.IsLocalPlayer)
                return kvp.Value;
        }
        return null;
    }

    /// <summary>
    /// 获取指定玩家
    /// </summary>
    public NetworkPlayer GetPlayer(int playerId)
    {
        spawnedPlayers.TryGetValue(playerId, out var player);
        return player;
    }

    /// <summary>
    /// 获取所有远程玩家
    /// </summary>
    public List<NetworkPlayer> GetRemotePlayers()
    {
        var list = new List<NetworkPlayer>();
        foreach (var kvp in spawnedPlayers)
        {
            if (!kvp.Value.IsLocalPlayer)
                list.Add(kvp.Value);
        }
        return list;
    }

    /// <summary>
    /// 清理所有玩家
    /// </summary>
    public void ClearAllPlayers()
    {
        foreach (var kvp in spawnedPlayers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        spawnedPlayers.Clear();
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    private Vector3 GetSpawnPosition(int playerId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = Mathf.Abs(playerId) % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // 默认位置
        return playerId % 2 == 0
            ? new Vector3(-3f, 1f, 0f)
            : new Vector3(3f, 1f, 0f);
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnConnected -= OnNetworkConnected;
            networkManager.OnDisconnected -= OnNetworkDisconnected;
        }
        ClearAllPlayers();
    }
}
