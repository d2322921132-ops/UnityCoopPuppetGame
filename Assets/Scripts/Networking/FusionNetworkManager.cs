using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Photon Fusion 网络管理器 - 完整版（SDK 导入后启用 Fusion API）
/// 当前为兼容层，SDK 导入后取消注释 Fusion 相关代码
/// </summary>
public class FusionNetworkManager : MonoBehaviour
{
    public static FusionNetworkManager Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private FusionSettings settings;

    [Header("玩家预制体")]
    [SerializeField] private GameObject playerPrefab;

    [Header("生成点")]
    [SerializeField] private Transform[] spawnPoints;

    // 运行时状态
    [SerializeField] private bool isRunning = false;
    [SerializeField] private int localPlayerId = -1;
    [SerializeField] private List<NetworkPlayer> connectedPlayers = new List<NetworkPlayer>();

    // 事件
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<NetworkPlayer> OnPlayerJoined;
    public event Action<NetworkPlayer> OnPlayerLeft;
    public event Action<string> OnError;

    // 公共属性
    public bool IsRunning => isRunning;
    public int LocalPlayerId => localPlayerId;
    public IReadOnlyList<NetworkPlayer> ConnectedPlayers => connectedPlayers;
    public bool IsHost => GameLauncher.Instance?.IsHost ?? false;

    // Fusion 引用（SDK 导入后启用）
    // [SerializeField] private NetworkRunner runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 启动网络会话
    /// </summary>
    public async Task<bool> StartSession(string roomName = null)
    {
        if (isRunning)
        {
            Debug.LogWarning("[FusionNetworkManager] 网络会话已在运行中");
            return true;
        }

        string targetRoom = roomName ?? settings?.DefaultRoomName ?? "CoupleRoom";
        Debug.Log($"[FusionNetworkManager] 启动会话: {targetRoom}");

        // SDK 导入后启用以下代码：
        /*
        if (runner == null)
        {
            GameObject runnerObj = new GameObject("NetworkRunner");
            runner = runnerObj.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
        }

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = ConvertGameMode(settings.GameMode),
            SessionName = targetRoom,
            PlayerCount = settings.MaxPlayers,
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = new PhotonAppSettings()
            {
                AppIdFusion = settings.AppId,
                AppVersion = settings.AppVersion,
                FixedRegion = settings.Region,
                EnableProtocolFallback = true,
                Protocol = ConnectionProtocol.Udp
            }
        });

        if (result.Ok)
        {
            isRunning = true;
            localPlayerId = runner.LocalPlayer.PlayerId;
            Debug.Log($"[FusionNetworkManager] 会话启动成功！本地玩家 ID: {localPlayerId}");
            OnConnected?.Invoke();
            return true;
        }
        else
        {
            Debug.LogError($"[FusionNetworkManager] 启动失败: {result.ShutdownReason}");
            OnError?.Invoke(result.ShutdownReason.ToString());
            return false;
        }
        */

        // 模拟模式（SDK 导入前）
        await Task.Delay(1000);
        isRunning = true;
        localPlayerId = 1;
        Debug.Log("[FusionNetworkManager] 模拟: 会话启动成功");
        OnConnected?.Invoke();
        return true;
    }

    /// <summary>
    /// 停止网络会话
    /// </summary>
    public async Task StopSession()
    {
        if (!isRunning) return;

        Debug.Log("[FusionNetworkManager] 停止会话...");

        // SDK 导入后启用：
        // if (runner != null && runner.IsRunning)
        // {
        //     await runner.Shutdown();
        // }

        await Task.Delay(500);

        isRunning = false;
        localPlayerId = -1;
        connectedPlayers.Clear();
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 生成玩家角色
    /// </summary>
    public NetworkPlayer SpawnPlayer(int playerId, bool isLocal)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[FusionNetworkManager] Player Prefab 未设置！");
            return null;
        }

        // 选择生成点
        Vector3 spawnPos = GetSpawnPosition(playerId);
        Quaternion spawnRot = Quaternion.identity;

        // SDK 导入后启用 Fusion 的网络生成：
        // var networkObject = runner.Spawn(playerPrefab, spawnPos, spawnRot, isLocal ? runner.LocalPlayer : null);
        // var player = networkObject.GetComponent<NetworkPlayer>();

        // 模拟模式（SDK 导入前）
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
        var player = playerObj.GetComponent<NetworkPlayer>();

        if (player != null)
        {
            player.Initialize(playerId, $"Player_{playerId}", isLocal);
            connectedPlayers.Add(player);
            OnPlayerJoined?.Invoke(player);
        }

        return player;
    }

    /// <summary>
    /// 销毁玩家角色
    /// </summary>
    public void DespawnPlayer(NetworkPlayer player)
    {
        if (player == null) return;

        connectedPlayers.Remove(player);
        OnPlayerLeft?.Invoke(player);

        // SDK 导入后启用：
        // var no = player.GetComponent<NetworkObject>();
        // if (no != null) runner.Despawn(no);

        Destroy(player.gameObject);
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    private Vector3 GetSpawnPosition(int playerId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = playerId % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // 默认生成位置
        return playerId == 1 ? new Vector3(-3, 1, 0) : new Vector3(3, 1, 0);
    }

    /// <summary>
    /// 转换游戏模式
    /// </summary>
    /*
    private Fusion.GameMode ConvertGameMode(GameMode mode)
    {
        return mode switch
        {
            GameMode.Shared => Fusion.GameMode.Shared,
            GameMode.Host => Fusion.GameMode.Host,
            GameMode.Client => Fusion.GameMode.Client,
            GameMode.Server => Fusion.GameMode.Server,
            _ => Fusion.GameMode.Shared
        };
    }
    */

    /// <summary>
    /// 检查是否已连接
    /// </summary>
    public bool IsConnected()
    {
        // SDK 导入后：return runner != null && runner.IsRunning;
        return isRunning;
    }

    /// <summary>
    /// 获取当前玩家数量
    /// </summary>
    public int GetPlayerCount()
    {
        // SDK 导入后：return runner.SessionInfo.PlayerCount;
        return connectedPlayers.Count;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
