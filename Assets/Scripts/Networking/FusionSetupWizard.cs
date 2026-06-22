using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Photon Fusion 设置向导 - 一键配置工具
/// 在 Unity 编辑器中自动创建 FusionSettings.asset 并配置 App ID
/// 
/// 使用方法:
/// 1. 菜单栏: Tools > Photon Fusion > Setup Wizard
/// 2. 粘贴你的 App ID
/// 3. 点击 "Create Configuration"
/// </summary>
public class FusionSetupWizard : MonoBehaviour
{
    [Header("Photon 配置")]
    [SerializeField] private string appId = "941e34e0-fd3a-4b71-a30a-92bfad5d9e82";
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "asia";

    [Header("房间配置")]
    [SerializeField] private string defaultRoomName = "CoupleRoom";
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private GameMode gameMode = GameMode.Shared;

    [Header("操作")]
    [SerializeField] private bool createConfigOnStart = false;

    private void Start()
    {
        if (createConfigOnStart)
        {
            CreateConfiguration();
        }
    }

    /// <summary>
    /// 创建 FusionSettings 配置文件
    /// </summary>
    public void CreateConfiguration()
    {
#if UNITY_EDITOR
        // 确保目录存在
        string configPath = "Assets/Resources";
        if (!Directory.Exists(configPath))
        {
            Directory.CreateDirectory(configPath);
            AssetDatabase.Refresh();
        }

        // 创建或更新 FusionSettings
        string assetPath = $"{configPath}/FusionSettings.asset";
        FusionSettings settings = AssetDatabase.LoadAssetAtPath<FusionSettings>(assetPath);

        bool isNew = settings == null;
        if (isNew)
        {
            settings = ScriptableObject.CreateInstance<FusionSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
        }

        // 使用反射设置私有字段（因为属性是只读的）
        SetFieldValue(settings, "appId", appId);
        SetFieldValue(settings, "appVersion", appVersion);
        SetFieldValue(settings, "region", region);
        SetFieldValue(settings, "defaultRoomName", defaultRoomName);
        SetFieldValue(settings, "maxPlayers", maxPlayers);
        SetFieldValue(settings, "gameMode", gameMode);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FusionSetupWizard] FusionSettings 已{(isNew ? "创建" : "更新")}: {assetPath}");
        Debug.Log($"  App ID: {appId.Substring(0, 8)}...");
        Debug.Log($"  区域: {region}");
        Debug.Log($"  默认房间: {defaultRoomName}");
#else
        Debug.LogWarning("[FusionSetupWizard] 配置创建仅在编辑器模式下可用");
#endif
    }

    /// <summary>
    /// 验证 App ID 格式
    /// </summary>
    public bool ValidateAppId()
    {
        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("[FusionSetupWizard] App ID 不能为空！");
            return false;
        }

        if (appId.Length < 10)
        {
            Debug.LogError("[FusionSetupWizard] App ID 格式无效（长度不足）");
            return false;
        }

        // GUID 格式验证
        if (!Guid.TryParse(appId, out _))
        {
            Debug.LogWarning("[FusionSetupWizard] App ID 不是标准 GUID 格式，但可能仍然有效");
        }

        Debug.Log("[FusionSetupWizard] App ID 验证通过");
        return true;
    }

    /// <summary>
    /// 使用反射设置私有字段
    /// </summary>
    private void SetFieldValue(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

#if UNITY_EDITOR

    [MenuItem("Tools/Photon Fusion/Setup Wizard")]
    private static void OpenWizard()
    {
        // 创建临时游戏对象来运行向导
        GameObject wizardObj = new GameObject("FusionSetupWizard_Temp");
        var wizard = wizardObj.AddComponent<FusionSetupWizard>();

        // 尝试加载现有配置
        var existing = Resources.Load<FusionSettings>("FusionSettings");
        if (existing != null)
        {
            wizard.appId = existing.AppId;
            wizard.appVersion = existing.AppVersion;
            wizard.region = existing.Region;
            wizard.defaultRoomName = existing.DefaultRoomName;
            wizard.maxPlayers = existing.MaxPlayers;
            wizard.gameMode = existing.GameMode;
            Debug.Log("[FusionSetupWizard] 已加载现有配置");
        }

        // 自动创建配置
        if (wizard.ValidateAppId())
        {
            wizard.CreateConfiguration();
        }

        // 清理临时对象
        DestroyImmediate(wizardObj);

        // 选中创建的配置文件
        var config = Resources.Load<FusionSettings>("FusionSettings");
        if (config != null)
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }

    [MenuItem("Tools/Photon Fusion/Create FusionSettings")]
    private static void QuickCreateConfig()
    {
        OpenWizard();
    }

    [MenuItem("Tools/Photon Fusion/Validate Configuration")]
    private static void ValidateConfiguration()
    {
        var settings = Resources.Load<FusionSettings>("FusionSettings");
        if (settings == null)
        {
            Debug.LogError("[FusionSetupWizard] 未找到 FusionSettings！请先运行 Setup Wizard");
            return;
        }

        if (settings.IsValid())
        {
            Debug.Log("[FusionSetupWizard] 配置验证通过 ✓");
            Debug.Log($"  App ID: {settings.GetMaskedAppId()}");
            Debug.Log($"  版本: {settings.AppVersion}");
            Debug.Log($"  区域: {settings.Region}");
            Debug.Log($"  房间: {settings.DefaultRoomName}");
            Debug.Log($"  最大玩家: {settings.MaxPlayers}");
        }
        else
        {
            Debug.LogError("[FusionSetupWizard] 配置验证失败！App ID 无效");
        }
    }

#endif
}
