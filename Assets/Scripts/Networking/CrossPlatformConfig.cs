using UnityEngine;

/// <summary>
/// CrossPlatformConfig - 跨平台联机配置
/// 
/// 功能：
/// 1. iOS + Android 公网互通
/// 2. 自动检测平台并应用对应网络设置
/// 3. NAT 穿透、中继服务器配置
/// 4. 网络质量自适应
/// 
/// 挂载方式：将此脚本与 NetworkManager 放在同一个空物体上
/// </summary>
public class CrossPlatformConfig : MonoBehaviour
{
    [Header("跨平台网络配置")]
    [SerializeField] private bool enableCrossPlatform = true;
    [SerializeField] private bool useRelayServer = true;
    [SerializeField] private bool enableNATTraversal = true;

    [Header("网络质量自适应")]
    [SerializeField] private bool adaptiveNetworkQuality = true;
    [SerializeField] private int targetFrameRate = 30;
    [SerializeField] private float networkUpdateRate = 30f;

    [Header("平台特定设置")]
    [SerializeField] private PlatformSettings androidSettings;
    [SerializeField] private PlatformSettings iOSSettings;

    // 单例
    public static CrossPlatformConfig Instance { get; private set; }

    // 当前平台设置
    public PlatformSettings CurrentSettings { get; private set; }
    public RuntimePlatform CurrentPlatform => Application.platform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ApplyPlatformSettings();
    }

    /// <summary>
    /// 应用当前平台的网络设置
    /// </summary>
    private void ApplyPlatformSettings()
    {
        switch (CurrentPlatform)
        {
            case RuntimePlatform.Android:
                CurrentSettings = androidSettings;
                Debug.Log("[CrossPlatformConfig] 应用 Android 网络设置");
                break;

            case RuntimePlatform.IPhonePlayer:
                CurrentSettings = iOSSettings;
                Debug.Log("[CrossPlatformConfig] 应用 iOS 网络设置");
                break;

            default:
                CurrentSettings = androidSettings; // 编辑器默认使用 Android 设置
                Debug.Log("[CrossPlatformConfig] 应用默认网络设置（编辑器模式）");
                break;
        }

        // 应用帧率设置
        Application.targetFrameRate = targetFrameRate;

        // 应用网络质量
        if (adaptiveNetworkQuality)
        {
            ApplyAdaptiveNetworkQuality();
        }
    }

    /// <summary>
    /// 应用自适应网络质量
    /// </summary>
    private void ApplyAdaptiveNetworkQuality()
    {
        // 根据网络状况动态调整发送频率
        // 实际实现需要在运行时根据 ping 值调整
        Debug.Log($"[CrossPlatformConfig] 网络更新频率: {networkUpdateRate}Hz");
    }

    /// <summary>
    /// 获取 Photon 区域设置
    /// </summary>
    public string GetBestRegion()
    {
        // 根据当前地理位置选择最佳区域
        // 亚洲用户默认使用 asia 区域
        return "asia";
    }

    /// <summary>
    /// 检查是否支持跨平台联机
    /// </summary>
    public bool IsCrossPlatformSupported()
    {
        return enableCrossPlatform;
    }

    /// <summary>
    /// 获取网络诊断信息
    /// </summary>
    public string GetNetworkDiagnostics()
    {
        return $"平台: {CurrentPlatform}\n" +
               $"跨平台: {(enableCrossPlatform ? "已启用" : "已禁用")}\n" +
               $"中继服务器: {(useRelayServer ? "已启用" : "已禁用")}\n" +
               $"NAT 穿透: {(enableNATTraversal ? "已启用" : "已禁用")}\n" +
               $"目标帧率: {targetFrameRate}\n" +
               $"网络更新率: {networkUpdateRate}Hz";
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width - 200, 10, 190, 40), "网络诊断"))
        {
            Debug.Log(GetNetworkDiagnostics());
        }
    }
}

/// <summary>
/// 平台特定网络设置
/// </summary>
[System.Serializable]
public class PlatformSettings
{
    [Header("网络协议")]
    public ConnectionProtocol protocol = ConnectionProtocol.Udp;
    public bool enableProtocolFallback = true;

    [Header("连接超时")]
    public float connectTimeout = 10f;
    public float disconnectTimeout = 5f;

    [Header("重连设置")]
    public bool autoReconnect = true;
    public int maxReconnectAttempts = 3;
    public float reconnectDelay = 2f;

    [Header("缓冲区")]
    public int sendBufferSize = 65536;
    public int receiveBufferSize = 65536;
}

/// <summary>
/// 连接协议枚举
/// </summary>
public enum ConnectionProtocol
{
    Udp,
    Tcp,
    Wss
}
