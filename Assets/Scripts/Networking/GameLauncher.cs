using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// 游戏网络启动器 - 程序启动时自动初始化 Photon Fusion NetworkRunner
/// 并创建名为 'CoupleRoom' 的联机房间
/// 
/// 使用方式:
/// 1. 将本脚本挂载到场景中的空 GameObject（如 "NetworkManager"）
/// 2. 在 Inspector 中拖入 FusionSettings.asset
/// 3. 运行后自动连接并创建房间
/// </summary>
public class GameLauncher : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private FusionSettings fusionSettings;
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("运行时状态")]
    [SerializeField] private NetworkState currentState = NetworkState.Disconnected;
    [SerializeField] private string currentRoomName = "";
    [SerializeField] private int playerCount = 0;
    [SerializeField] private bool isHost = false;

    // 静态实例
    public static GameLauncher Instance { get; private set; }

    // 公共事件
    public event Action<NetworkState> OnNetworkStateChanged;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action<string> OnError;

    // 公共属性
    public NetworkState CurrentState => currentState;
    public string CurrentRoomName => currentRoomName;
    public int PlayerCount => playerCount;
    public bool IsHost => isHost;
    public bool IsConnected => currentState == NetworkState.Connected || currentState == NetworkState.InRoom;
    public FusionSettings Settings => fusionSettings;

    // Fusion 引用（SDK 导入后解注释）
    // private NetworkRunner runner;

    private void Awake()
    {
        // 单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // 尝试查找配置
        if (fusionSettings == null)
        {
            fusionSettings = FindFusionSettings();
        }
    }

    private void Start()
    {
        if (autoStartOnAwake)
        {
            LaunchAsync();
        }
    }

    /// <summary>
    /// 启动网络连接 - 异步入口
    /// </summary>
    public async void LaunchAsync()
    {
        try
        {
            await LaunchNetworkAsync();
        }
        catch (Exception ex)
        {
            LogError($"网络启动异常: {ex.Message}");
            UpdateState(NetworkState.Error);
            OnError?.Invoke(ex.Message);
        }
    }

    /// <summary>
    /// 核心启动逻辑 - 初始化 NetworkRunner 并创建房间
    /// </summary>
    private async Task LaunchNetworkAsync()
    {
        // 验证配置
        if (fusionSettings == null)
        {
            LogError("FusionSettings 未配置！请创建 FusionSettings.asset 并拖入 Inspector。");
            UpdateState(NetworkState.Error);
            OnError?.Invoke("FusionSettings 未配置");
            return;
        }

        if (!fusionSettings.IsValid())
        {
            LogError($"App ID 无效: {fusionSettings.GetMaskedAppId()}");
            UpdateState(NetworkState.Error);
            OnError?.Invoke("App ID 无效");
            return;
        }

        UpdateState(NetworkState.Initializing);
        Log($"正在初始化 Photon Fusion...");
        Log($"App ID: {fusionSettings.GetMaskedAppId()}");
        Log($"区域: {fusionSettings.Region}");
        Log($"模式: {fusionSettings.GameMode}");

        // 检查 Photon Fusion SDK 是否已导入
        if (!IsFusionSDKAvailable())
        {
            LogWarning("Photon Fusion SDK 未检测到！请先导入 SDK。");
            LogWarning("1. 下载: https://downloads.photonengine.com/download/latest/photon-fusion-sdk-2");
            LogWarning("2. 导入: Assets > Import Package > Custom Package");
            UpdateState(NetworkState.SDKMissing);
            OnError?.Invoke("Photon Fusion SDK 未导入");
            return;
        }

        UpdateState(NetworkState.Connecting);
        Log("正在连接 Photon 服务器...");

        // 实际 Fusion 初始化代码（SDK 导入后启用）
        // await InitializeFusionRunner();

        // 模拟连接流程（SDK 导入前用于测试）
        await SimulateConnectionAsync();
    }

    /// <summary>
    /// 创建或加入房间
    /// </summary>
    public async void JoinOrCreateRoom(string roomName = null)
    {
        string targetRoom = roomName ?? fusionSettings?.DefaultRoomName ?? "CoupleRoom";
        currentRoomName = targetRoom;

        Log($"正在创建/加入房间: {targetRoom}");
        UpdateState(NetworkState.JoiningRoom);

        // 实际 Fusion 房间创建代码（SDK 导入后启用）
        // var result = await runner.StartGame(new StartGameArgs()
        // {
        //     GameMode = GetFusionGameMode(),
        //     SessionName = targetRoom,
        //     PlayerCount = fusionSettings.MaxPlayers,
        //     CustomPhotonAppSettings = new PhotonAppSettings()
        //     {
        //         AppIdFusion = fusionSettings.AppId,
        //         AppVersion = fusionSettings.AppVersion,
        //         FixedRegion = fusionSettings.Region
        //     }
        // });
        //
        // if (result.Ok)
        // {
        //     UpdateState(NetworkState.InRoom);
        //     isHost = runner.IsSharedModeMasterClient || runner.IsServer;
        //     Log($"成功进入房间: {targetRoom}");
        //     OnConnected?.Invoke();
        // }
        // else
        // {
        //     LogError($"进入房间失败: {result.ShutdownReason}");
        //     UpdateState(NetworkState.Error);
        //     OnError?.Invoke(result.ShutdownReason.ToString());
        // }

        // 模拟房间创建（SDK 导入前用于测试）
        await Task.Delay(1000);
        UpdateState(NetworkState.InRoom);
        isHost = true;
        playerCount = 1;
        Log($"模拟: 成功创建房间 '{targetRoom}'");
        Log($"当前玩家数: {playerCount}/{fusionSettings?.MaxPlayers ?? 2}");
        OnConnected?.Invoke();
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async void Disconnect()
    {
        Log("正在断开连接...");
        UpdateState(NetworkState.Disconnecting);

        // 实际 Fusion 断开代码（SDK 导入后启用）
        // if (runner != null && runner.IsRunning)
        // {
        //     await runner.Shutdown();
        // }

        await Task.Delay(500);

        currentRoomName = "";
        playerCount = 0;
        isHost = false;
        UpdateState(NetworkState.Disconnected);
        Log("已断开连接");
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    public void Reconnect()
    {
        Disconnect();
        LaunchAsync();
    }

    #region 私有方法

    /// <summary>
    /// 检查 Photon Fusion SDK 是否可用
    /// </summary>
    private bool IsFusionSDKAvailable()
    {
        // 检查关键类型是否存在
        var networkRunnerType = Type.GetType("Fusion.NetworkRunner, Fusion.Runtime");
        return networkRunnerType != null;
    }

    /// <summary>
    /// 查找项目中的 FusionSettings
    /// </summary>
    private FusionSettings FindFusionSettings()
    {
#if UNITY_EDITOR
        // 编辑器模式下搜索资源
        var guids = UnityEditor.AssetDatabase.FindAssets("t:FusionSettings");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<FusionSettings>(path);
        }
#endif
        return null;
    }

    /// <summary>
    /// 模拟连接（SDK 导入前测试用）
    /// </summary>
    private async Task SimulateConnectionAsync()
    {
        await Task.Delay(1500);
        Log("模拟: 已连接到 Photon 服务器");
        UpdateState(NetworkState.Connected);
        OnConnected?.Invoke();

        // 自动创建房间
        JoinOrCreateRoom();
    }

    /// <summary>
    /// 更新网络状态
    /// </summary>
    private void UpdateState(NetworkState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        Log($"网络状态: {newState}");
        OnNetworkStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// 获取 Fusion 游戏模式
    /// </summary>
    // private Fusion.GameMode GetFusionGameMode()
    // {
    //     return fusionSettings.GameMode switch
    //     {
    //         GameMode.Shared => Fusion.GameMode.Shared,
    //         GameMode.Host => Fusion.GameMode.Host,
    //         GameMode.Client => Fusion.GameMode.Client,
    //         GameMode.Server => Fusion.GameMode.Server,
    //         _ => Fusion.GameMode.Shared
    //     };
    // }

    private void Log(string message)
    {
        if (fusionSettings != null && fusionSettings.ShowDebugLogs)
        {
            Debug.Log($"[GameLauncher] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[GameLauncher] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameLauncher] {message}");
    }

    #endregion

    #region 生命周期

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion
}

// NetworkState 枚举已在 NetworkManager.cs 中定义
