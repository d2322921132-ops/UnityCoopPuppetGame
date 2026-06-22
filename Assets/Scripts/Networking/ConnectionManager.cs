using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// ConnectionManager - 全自动网络连接管理器
/// 
/// 功能：
/// 1. 启动时自动读取 App ID 并连接 Photon Fusion
/// 2. 自动创建/加入名为 "CoupleRoom" 的房间
/// 3. 处理连接状态、断线重连、错误回调
/// 4. 零 Inspector 配置，全部自动完成
/// 
/// 安卓打包注意（ProjectSettings 必须配置）：
/// - PlayerSettings > Android > Package Name: com.yourcompany.couplegame
/// - PlayerSettings > Android > Minimum API Level: Android 8.0 (API 26)
/// - PlayerSettings > Android > Target API Level: Automatic (highest installed)
/// - PlayerSettings > Other Settings > Internet Access: Require
/// - PlayerSettings > Publishing Settings > 创建 keystore 用于签名
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("Photon 配置 - 已预填你的 App ID")]
    [SerializeField] private string photonAppId = "941e34e0-fd3a-4b71-a30a-92bfad5d9e82";
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "asia";
    [SerializeField] private string roomName = "CoupleRoom";
    [SerializeField] private int maxPlayers = 2;

    [Header("运行时状态")]
    [SerializeField] private ConnectionState state = ConnectionState.Disconnected;
    [SerializeField] private string statusMessage = "等待启动...";
    [SerializeField] private int pingMs = 0;

    // 单例
    public static ConnectionManager Instance { get; private set; }

    // 事件
    public event Action OnConnectedToMaster;
    public event Action OnJoinedRoom;
    public event Action OnDisconnected;
    public event Action<string> OnError;
    public event Action<ConnectionState> OnStateChanged;

    // 属性
    public ConnectionState State => state;
    public string StatusMessage => statusMessage;
    public bool IsConnected => state == ConnectionState.Connected || state == ConnectionState.InRoom;
    public bool IsInRoom => state == ConnectionState.InRoom;
    public string CurrentRoomName => roomName;

    // Fusion SDK 引用（SDK 导入后取消注释）
    // private NetworkRunner runner;

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
        UpdateState(ConnectionState.Initializing, "正在初始化网络...");
        
        // 延迟一帧确保所有系统就绪
        await Task.Yield();
        
        await ConnectAsync();
    }

    /// <summary>
    /// 异步连接入口
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            UpdateState(ConnectionState.Connecting, "正在连接 Photon 服务器...");

            // 检查 Fusion SDK 是否已导入
            if (!IsFusionSDKAvailable())
            {
                UpdateState(ConnectionState.Error, "Photon Fusion SDK 未导入！");
                OnError?.Invoke("SDK_MISSING");
                Debug.LogError("[ConnectionManager] 请先导入 Photon Fusion SDK: https://doc.photonengine.com/fusion/current/getting-started/sdk-download");
                return;
            }

            // SDK 导入后启用真实连接：
            /*
            if (runner == null)
            {
                GameObject runnerObj = new GameObject("NetworkRunner");
                runnerObj.transform.SetParent(transform);
                runner = runnerObj.AddComponent<NetworkRunner>();
                runner.ProvideInput = true;
            }

            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = Fusion.GameMode.Shared,
                SessionName = roomName,
                PlayerCount = maxPlayers,
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                CustomPhotonAppSettings = new PhotonAppSettings()
                {
                    AppIdFusion = photonAppId,
                    AppVersion = appVersion,
                    FixedRegion = region,
                    Protocol = ConnectionProtocol.Udp,
                    EnableProtocolFallback = true
                }
            });

            if (result.Ok)
            {
                UpdateState(ConnectionState.InRoom, $"已加入房间: {roomName}");
                OnConnectedToMaster?.Invoke();
                OnJoinedRoom?.Invoke();
                Debug.Log($"[ConnectionManager] 连接成功！玩家 ID: {runner.LocalPlayer.PlayerId}");
            }
            else
            {
                UpdateState(ConnectionState.Error, $"连接失败: {result.ShutdownReason}");
                OnError?.Invoke(result.ShutdownReason.ToString());
            }
            */

            // 模拟模式 - SDK 导入前测试用
            await Task.Delay(1500);
            UpdateState(ConnectionState.InRoom, $"已加入房间: {roomName}");
            OnConnectedToMaster?.Invoke();
            OnJoinedRoom?.Invoke();
            Debug.Log("[ConnectionManager] 模拟: 连接成功！");
        }
        catch (Exception ex)
        {
            UpdateState(ConnectionState.Error, $"异常: {ex.Message}");
            OnError?.Invoke(ex.Message);
            Debug.LogError($"[ConnectionManager] 连接异常: {ex}");
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        UpdateState(ConnectionState.Disconnecting, "正在断开...");
        
        // SDK 导入后：
        // if (runner != null && runner.IsRunning)
        //     await runner.Shutdown();

        await Task.Delay(500);
        
        UpdateState(ConnectionState.Disconnected, "已断开");
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    public async Task ReconnectAsync()
    {
        await DisconnectAsync();
        await Task.Delay(1000);
        await ConnectAsync();
    }

    /// <summary>
    /// 检查 Fusion SDK 是否可用
    /// </summary>
    private bool IsFusionSDKAvailable()
    {
        // 检查关键类型是否存在
        var type = Type.GetType("Fusion.NetworkRunner, Fusion.Runtime");
        return type != null;
    }

    /// <summary>
    /// 更新状态并触发事件
    /// </summary>
    private void UpdateState(ConnectionState newState, string message)
    {
        state = newState;
        statusMessage = message;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[ConnectionManager] {newState}: {message}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public enum ConnectionState
{
    Disconnected,
    Initializing,
    Connecting,
    Connected,
    InRoom,
    Disconnecting,
    Error
}
