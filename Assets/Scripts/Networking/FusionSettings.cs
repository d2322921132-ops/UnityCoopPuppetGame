using UnityEngine;

/// <summary>
/// Photon Fusion 全局配置 - ScriptableObject
/// 包含 App ID、版本、区域等核心网络参数
/// </summary>
[CreateAssetMenu(fileName = "FusionSettings", menuName = "Photon/Fusion Settings", order = 0)]
public class FusionSettings : ScriptableObject
{
    [Header("Photon 应用配置")]
    [SerializeField] private string appId = "941e34e0-fd3a-4b71-a30a-92bfad5d9e82";
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "asia"; // asia, eu, us, cn

    [Header("房间配置")]
    [SerializeField] private string defaultRoomName = "CoupleRoom";
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private GameMode gameMode = GameMode.Shared;

    [Header("网络优化")]
    [SerializeField] private int sendRate = 30;
    [SerializeField] private bool enableDeltaCompression = true;

    [Header("调试")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool simulateLatency = false;
    [SerializeField] private float simulatedLatency = 0.1f;

    // 公共访问属性
    public string AppId => appId;
    public string AppVersion => appVersion;
    public string Region => region;
    public string DefaultRoomName => defaultRoomName;
    public int MaxPlayers => maxPlayers;
    public GameMode GameMode => gameMode;
    public int SendRate => sendRate;
    public bool EnableDeltaCompression => enableDeltaCompression;
    public bool ShowDebugLogs => showDebugLogs;
    public bool SimulateLatency => simulateLatency;
    public float SimulatedLatency => simulatedLatency;

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(appId)) return false;
        if (appId.Length < 10) return false;
        if (appId.Contains("your") || appId.Contains("xxx")) return false;
        return true;
    }

    /// <summary>
    /// 获取掩码后的 App ID（用于日志显示）
    /// </summary>
    public string GetMaskedAppId()
    {
        if (string.IsNullOrEmpty(appId) || appId.Length <= 8)
            return "未配置";
        return appId.Substring(0, 8) + "..." + appId.Substring(appId.Length - 4);
    }

    private void OnValidate()
    {
        if (maxPlayers < 2) maxPlayers = 2;
        if (maxPlayers > 20) maxPlayers = 20;
        if (sendRate < 10) sendRate = 10;
        if (sendRate > 60) sendRate = 60;
    }
}

/// <summary>
/// 游戏模式枚举 - 对应 Fusion 的网络拓扑
/// </summary>
public enum GameMode
{
    /// <summary>共享模式 - 云端房间拥有 StateAuthority，适合休闲游戏</summary>
    Shared,
    /// <summary>主机模式 - 一个玩家作为主机，其他连接</summary>
    Host,
    /// <summary>客户端模式 - 连接到指定主机</summary>
    Client,
    /// <summary>专用服务器模式</summary>
    Server
}
