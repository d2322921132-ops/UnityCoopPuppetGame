using UnityEngine;

/// <summary>
/// Photon App ID 配置 - 在 Unity Inspector 中填写你的 App ID
/// 创建方式: Assets → Create → Photon Configuration
/// 然后在 GameBootstrap 中拖入引用
/// </summary>
[CreateAssetMenu(fileName = "PhotonConfig", menuName = "Photon/Configuration", order = 1)]
public class PhotonAppIdConfig : ScriptableObject
{
    [Header("Photon Fusion 配置")]
    [SerializeField] private string appId = "";
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "asia";

    [Header("调试")]
    [SerializeField] private bool showStatusOnGUI = true;

    public string AppId => appId;
    public string AppVersion => appVersion;
    public string Region => region;

    /// <summary>
    /// 验证 App ID 是否为有效格式
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(appId)) return false;
        if (appId.Length < 10) return false;
        if (appId.Contains("your")) return false;
        return true;
    }

    /// <summary>
    /// 获取掩码后的 App ID（用于日志显示）
    /// </summary>
    public string GetMaskedAppId()
    {
        if (string.IsNullOrEmpty(appId) || appId.Length <= 8) return "未配置";
        return appId.Substring(0, 8) + "..." + appId.Substring(appId.Length - 4);
    }
}