using UnityEngine;

/// <summary>
/// Photon Fusion 配置向导
/// 用于在 Unity 编辑器中配置 Photon App ID
/// </summary>
public class PhotonFusionSetup : MonoBehaviour
{
    [Header("Photon Fusion 配置")]
    [SerializeField] private string photonAppId = "";
    [SerializeField] private string photonAppVersion = "1.0";
    [SerializeField] private string region = "asia"; // asia, eu, us, etc.

    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;

    public string PhotonAppId => photonAppId;
    public string PhotonAppVersion => photonAppVersion;

    private void Awake()
    {
        ValidateConfiguration();
    }

    /// <summary>
    /// 验证 Photon 配置
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(photonAppId))
        {
            Debug.LogError(
                "[PhotonFusionSetup] Photon App ID 未配置！\n" +
                "请按以下步骤获取 App ID:\n" +
                "1. 访问 https://www.photonengine.com/ 注册账号\n" +
                "2. 创建一个新的 Fusion 应用\n" +
                "3. 复制 App ID 到本脚本的 photonAppId 字段"
            );
        }
        else
        {
            Debug.Log($"[PhotonFusionSetup] Photon App ID 已配置: {photonAppId.Substring(0, 8)}...");
        }
    }

    /// <summary>
    /// 设置 Photon App ID（运行时调用）
    /// </summary>
    public void SetAppId(string appId)
    {
        photonAppId = appId;
        Debug.Log("[PhotonFusionSetup] App ID 已更新");
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(Screen.width - 260, 10, 250, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== Photon Fusion 配置 ===", new GUIStyle { fontSize = 12, fontStyle = FontStyle.Bold });

        if (string.IsNullOrEmpty(photonAppId))
        {
            GUILayout.Label("状态: 未配置", new GUIStyle { normal = new GUIStyleState { textColor = Color.red } });
            GUILayout.Label("请在 Inspector 中填写 App ID");
        }
        else
        {
            GUILayout.Label("状态: 已配置", new GUIStyle { normal = new GUIStyleState { textColor = Color.green } });
            GUILayout.Label($"App ID: {photonAppId.Substring(0, 8)}...");
            GUILayout.Label($"版本: {photonAppVersion}");
            GUILayout.Label($"区域: {region}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
