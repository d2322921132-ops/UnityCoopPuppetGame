using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if FUSION_ENABLED
using Fusion;
using Fusion.Sockets;
#endif

/// <summary>
/// 网络连接状态
/// </summary>
public enum NetworkState
{
    Disconnected,
    Initializing,
    SDKMissing,
    Connecting,
    Connected,
    JoiningRoom,
    InRoom,
    Disconnecting,
    Error
}

/// <summary>
/// 连接状态枚举（供 GameManager/UIManager 使用）
/// </summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Failed
}

/// <summary>
/// NetworkManager - 处理 Photon Fusion 的自动连接逻辑
/// 
/// 功能：
/// 1. 启动时自动连接 Photon 服务器
/// 2. 自动创建/加入名为 "CoupleRoom" 的房间
/// 3. 处理玩家加入/离开事件
/// 4. 跨平台支持：iOS + Android 公网互通
/// 
/// 挂载方式：在 Unity 中创建空物体，命名为 "NetworkManager"，将此脚本拖上去即可
/// 
/// 注意：需要在 Unity 的 Player Settings -> Scripting Define Symbols 中添加 FUSION_ENABLED
///       才能启用 Photon Fusion 网络功能。未添加时，将使用本地单机模式。
/// </summary>

#if FUSION_ENABLED

/// <summary>
/// Photon Fusion 版本的 NetworkManager，提供完整的网络联机功能
/// </summary>
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Photon 配置")]
    [SerializeField] private string appId = "941e34e0-fd3a-4b71-a30a-92bfad5d9e82";
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "asia"; // asia / eu / us / cn
    [SerializeField] private string roomName = "CoupleRoom";
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private GameMode gameMode = GameMode.Shared;

    [Header("玩家预制体")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("生成点")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("运行时状态")]
    [SerializeField] private NetworkState currentState = NetworkState.Disconnected;
    [SerializeField] private int playerCount = 0;
    [SerializeField] private bool isHost = false;

    // 单例
    public static NetworkManager Instance { get; private set; }

    // 公共属性
    public NetworkRunner Runner { get; private set; }
    public NetworkState CurrentState => currentState;
    public bool IsConnected => currentState == NetworkState.Connected || currentState == NetworkState.InRoom;
    public bool IsInRoom => currentState == NetworkState.InRoom;
    public int PlayerCount => playerCount;
    public bool IsSessionHost => isHost;

    // ConnectionStatus 属性（供 GameManager/UIManager 使用）
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    // 事件
    public event Action OnServerConnected;
    public event Action OnJoinedRoom;
    public event Action OnDisconnected;
    public event Action<PlayerRef> OnPlayerJoinedEvent;
    public event Action<PlayerRef> OnPlayerLeftEvent;
    public event Action<string> OnError;
    public event Action<ConnectionStatus> OnConnectionStatusChanged;
    public event Action OnConnectedToMaster;

    // 玩家字典
    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        UpdateState(NetworkState.Initializing, "正在初始化...");
        await Task.Delay(500);
        await StartGameAsync();
    }

    /// <summary>
    /// 启动游戏 - 自动连接并创建/加入房间
    /// </summary>
    public async Task StartGameAsync()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Debug.LogWarning("[NetworkManager] 游戏已在运行中");
            return;
        }

        UpdateState(NetworkState.Connecting, "正在连接服务器...");

        // 创建 NetworkRunner
        GameObject runnerObj = new GameObject("NetworkRunner");
        runnerObj.transform.SetParent(transform);
        Runner = runnerObj.AddComponent<NetworkRunner>();
        Runner.ProvideInput = true;

        // 注册回调
        Runner.AddCallbacks(this);

        // 配置 Photon 设置
        var photonSettings = new PhotonAppSettings
        {
            AppIdFusion = appId,
            AppVersion = appVersion,
            FixedRegion = region,
            Protocol = ConnectionProtocol.Udp,
            EnableProtocolFallback = true,
            Server = string.Empty,
            Port = 0,
            ProxyServer = string.Empty,
            EnableLobbyStatistics = false,
            UseNameServer = true
        };

        // 启动游戏
        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            SceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = photonSettings
        });

        if (result.Ok)
        {
            UpdateState(NetworkState.InRoom, $"已加入房间: {roomName}");
            isHost = Runner.IsSharedModeMasterClient;
            SetConnectionStatus(ConnectionStatus.Connected);
            OnServerConnected?.Invoke();
            OnConnectedToMaster?.Invoke();
            OnJoinedRoom?.Invoke();
            Debug.Log($"[NetworkManager] 连接成功！玩家 ID: {Runner.LocalPlayer.PlayerId}, 是否主机: {isHost}");
        }
        else
        {
            UpdateState(NetworkState.Error, $"连接失败: {result.ShutdownReason}");
            SetConnectionStatus(ConnectionStatus.Failed);
            OnError?.Invoke(result.ShutdownReason.ToString());
            Debug.LogError($"[NetworkManager] 连接失败: {result.ShutdownReason}");
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (Runner != null && Runner.IsRunning)
        {
            UpdateState(NetworkState.Disconnecting, "正在断开...");
            SetConnectionStatus(ConnectionStatus.Disconnected);
            await Runner.Shutdown();
        }

        spawnedPlayers.Clear();
        playerCount = 0;
        UpdateState(NetworkState.Disconnected, "已断开");
        SetConnectionStatus(ConnectionStatus.Disconnected);
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    public async Task ReconnectAsync()
    {
        await ShutdownAsync();
        await Task.Delay(1000);
        await StartGameAsync();
    }

    #region INetworkRunnerCallbacks 回调

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkManager] 玩家加入: {player.PlayerId}");
        playerCount = runner.SessionInfo.PlayerCount;

        // 只有主机或 Shared 模式下的玩家可以生成对象
        if (runner.IsSharedModeMasterClient || runner.LocalPlayer == player)
        {
            SpawnPlayer(runner, player);
        }

        OnPlayerJoinedEvent?.Invoke(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkManager] 玩家离开: {player.PlayerId}");
        playerCount = runner.SessionInfo.PlayerCount;

        // 清理玩家对象
        if (spawnedPlayers.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            spawnedPlayers.Remove(player);
        }

        OnPlayerLeftEvent?.Invoke(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 输入处理在 PlayerSync 中进行
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.LogWarning($"[NetworkManager] 玩家 {player.PlayerId} 输入丢失");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UpdateState(NetworkState.Disconnected, $"连接关闭: {shutdownReason}");
        SetConnectionStatus(ConnectionStatus.Disconnected);
        spawnedPlayers.Clear();
        OnDisconnected?.Invoke();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        UpdateState(NetworkState.Connected, "已连接到服务器");
        SetConnectionStatus(ConnectionStatus.Connected);
        OnServerConnected?.Invoke();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        UpdateState(NetworkState.Disconnected, $"断开连接: {reason}");
        SetConnectionStatus(ConnectionStatus.Disconnected);
        OnDisconnected?.Invoke();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log($"[NetworkManager] 连接请求: {request.RemoteAddress}");
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        UpdateState(NetworkState.Error, $"连接失败: {reason}");
        OnError?.Invoke(reason.ToString());
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新 ConnectionStatus 并触发事件
    /// </summary>
    private void SetConnectionStatus(ConnectionStatus status)
    {
        var oldStatus = Status;
        Status = status;
        if (oldStatus != status)
        {
            OnConnectionStatusChanged?.Invoke(status);
        }
    }

    /// <summary>
    /// 生成玩家角色
    /// </summary>
    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkManager] Player Prefab 未设置！");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(player);
        Quaternion spawnRot = Quaternion.identity;

        var networkObject = runner.Spawn(playerPrefab, spawnPos, spawnRot, player);

        if (networkObject != null)
        {
            spawnedPlayers[player] = networkObject;
            Debug.Log($"[NetworkManager] 玩家 {player.PlayerId} 生成在 {spawnPos}");

            // 设置玩家颜色（本地蓝色，远程红色）
            var renderer = networkObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = player == runner.LocalPlayer ? Color.blue : Color.red;
            }
        }
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // 默认位置
        return player.PlayerId == 1 ? new Vector3(-3, 1, 0) : new Vector3(3, 1, 0);
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    private void UpdateState(NetworkState state, string message)
    {
        currentState = state;
        Debug.Log($"[NetworkManager] {state}: {message}");
    }

    #endregion

    #region 公共网络操作方法（供 GameManager/UIManager 调用）

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public void ConnectToServer()
    {
        SetConnectionStatus(ConnectionStatus.Connecting);
        _ = StartGameAsync();
    }

    /// <summary>
    /// 加入指定房间
    /// </summary>
    public void JoinRoom(string roomName)
    {
        if (Runner != null && Runner.IsRunning)
        {
            Debug.Log($"[NetworkManager] 已在房间中，尝试切换到房间: {roomName}");
            // Fusion Shared 模式下，需要重新连接以切换房间
            _ = ReconnectAsync();
        }
        else
        {
            this.roomName = roomName;
            ConnectToServer();
        }
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void CreateRoom(string roomName)
    {
        this.roomName = roomName;
        SetConnectionStatus(ConnectionStatus.Connecting);
        _ = StartGameAsync();
    }

    /// <summary>
    /// 随机加入房间
    /// </summary>
    public void JoinRandomRoom()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Debug.Log("[NetworkManager] 已在房间中");
            return;
        }

        SetConnectionStatus(ConnectionStatus.Connecting);
        _ = StartGameAsync();
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        SetConnectionStatus(ConnectionStatus.Disconnected);
        _ = ShutdownAsync();
    }

    #endregion

    private void OnDestroy()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown();
        }

        if (Instance == this)
            Instance = null;
    }
}

#else

/// <summary>
/// 本地单机版本的 NetworkManager（无 Photon Fusion SDK 时使用）
/// 
/// 提供与 Fusion 版本相同的公共接口，但不执行实际的网络连接。
/// 所有事件仍会正常触发，确保 GameManager 和 UIManager 无需修改即可正常工作。
/// 
/// 要启用 Fusion 联机功能，请在 Unity 的 Player Settings -> Scripting Define Symbols 中添加 FUSION_ENABLED
/// </summary>
public class NetworkManager : MonoBehaviour
{
    [Header("本地模式配置（无 Fusion SDK）")]
    [SerializeField] private string roomName = "CoupleRoom";

    [Header("运行时状态")]
    [SerializeField] private NetworkState currentState = NetworkState.Disconnected;
    [SerializeField] private int playerCount = 1;
    [SerializeField] private bool isHost = true;

    // 单例
    public static NetworkManager Instance { get; private set; }

    // 公共属性
    public NetworkState CurrentState => currentState;
    public bool IsConnected => currentState == NetworkState.Connected || currentState == NetworkState.InRoom;
    public bool IsInRoom => currentState == NetworkState.InRoom;
    public int PlayerCount => playerCount;
    public bool IsSessionHost => isHost;

    // ConnectionStatus 属性（供 GameManager/UIManager 使用）
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    // 事件（与 Fusion 版本保持相同的签名）
    public event Action OnServerConnected;
    public event Action OnJoinedRoom;
    public event Action OnDisconnected;
    public event Action<int> OnPlayerJoinedEvent;         // Fusion 版用 PlayerRef，本地版用 int 代替
    public event Action<int> OnPlayerLeftEvent;
    public event Action<string> OnError;
    public event Action<ConnectionStatus> OnConnectionStatusChanged;
    public event Action OnConnectedToMaster;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[NetworkManager] 本地单机模式启动（未检测到 Photon Fusion SDK）");
    }

    private void Start()
    {
        // 本地模式下，自动模拟一个 "已连接" 的状态，方便单机游戏正常进行
        SimulateLocalConnection();
    }

    /// <summary>
    /// 模拟本地连接（单机模式下的替代方案）
    /// </summary>
    private async void SimulateLocalConnection()
    {
        UpdateState(NetworkState.Connecting, "本地模式：正在初始化...");
        SetConnectionStatus(ConnectionStatus.Connecting);
        await Task.Delay(300);

        UpdateState(NetworkState.Connected, "本地模式：已就绪");
        SetConnectionStatus(ConnectionStatus.Connected);
        OnServerConnected?.Invoke();
        OnConnectedToMaster?.Invoke();

        await Task.Delay(200);

        UpdateState(NetworkState.InRoom, $"本地模式：房间 {roomName}");
        SetConnectionStatus(ConnectionStatus.Connected);
        OnJoinedRoom?.Invoke();

        Debug.Log("[NetworkManager] 本地模式就绪 - 联机功能不可用，但可以正常进行单机游戏");
    }

    #region 私有方法

    /// <summary>
    /// 更新 ConnectionStatus 并触发事件
    /// </summary>
    private void SetConnectionStatus(ConnectionStatus status)
    {
        var oldStatus = Status;
        Status = status;
        if (oldStatus != status)
        {
            OnConnectionStatusChanged?.Invoke(status);
        }
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    private void UpdateState(NetworkState state, string message)
    {
        currentState = state;
        Debug.Log($"[NetworkManager] {state}: {message}");
    }

    #endregion

    #region 公共网络操作方法（供 GameManager/UIManager 调用）

    /// <summary>
    /// 连接到服务器（本地模式下仅模拟连接状态）
    /// </summary>
    public void ConnectToServer()
    {
        if (IsConnected)
        {
            Debug.LogWarning("[NetworkManager] 本地模式：已处于连接状态");
            return;
        }

        SetConnectionStatus(ConnectionStatus.Connecting);
        UpdateState(NetworkState.Connecting, "本地模式：模拟连接中...");
        _ = SimulateLocalConnection();
    }

    /// <summary>
    /// 加入指定房间（本地模式下仅输出日志）
    /// </summary>
    public void JoinRoom(string roomName)
    {
        Debug.Log($"[NetworkManager] 本地模式：尝试加入房间 '{roomName}'（单机模式无实际效果）");
        this.roomName = roomName;
        SetConnectionStatus(ConnectionStatus.Connected);
        UpdateState(NetworkState.InRoom, $"本地模式：已加入房间 {roomName}");
        OnJoinedRoom?.Invoke();
    }

    /// <summary>
    /// 创建房间（本地模式下仅输出日志）
    /// </summary>
    public void CreateRoom(string roomName)
    {
        Debug.Log($"[NetworkManager] 本地模式：尝试创建房间 '{roomName}'（单机模式无实际效果）");
        this.roomName = roomName;
        SetConnectionStatus(ConnectionStatus.Connecting);
        UpdateState(NetworkState.InRoom, $"本地模式：已创建房间 {roomName}");
        SetConnectionStatus(ConnectionStatus.Connected);
        OnJoinedRoom?.Invoke();
    }

    /// <summary>
    /// 随机加入房间（本地模式下仅输出日志）
    /// </summary>
    public void JoinRandomRoom()
    {
        Debug.Log("[NetworkManager] 本地模式：随机加入房间（单机模式无实际效果）");
        SetConnectionStatus(ConnectionStatus.Connecting);
        _ = SimulateLocalConnection();
    }

    /// <summary>
    /// 断开连接（本地模式下重置状态）
    /// </summary>
    public void Disconnect()
    {
        Debug.Log("[NetworkManager] 本地模式：断开连接");
        SetConnectionStatus(ConnectionStatus.Disconnected);
        UpdateState(NetworkState.Disconnected, "本地模式：已断开");
        playerCount = 0;
        OnDisconnected?.Invoke();
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

#endif
