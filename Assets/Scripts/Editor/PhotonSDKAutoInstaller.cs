using UnityEngine;
using System;
using System.IO;
using System.Net;
#if UNITY_EDITOR
using UnityEditor;
using System.Threading.Tasks;
#endif

/// <summary>
/// PhotonSDKAutoInstaller - 打开项目时自动下载并导入 Photon Fusion 2 SDK
/// 
/// 功能：
/// 1. 自动检测 SDK 是否已安装
/// 2. 如果未安装，自动下载最新版 Fusion 2 SDK
/// 3. 自动导入到项目中
/// 4. 完全零手动操作
/// 
/// 使用方法：什么都不用做，打开 Unity 项目自动执行
/// </summary>
public class PhotonSDKAutoInstaller
{
#if UNITY_EDITOR
    private const string SDK_DOWNLOAD_URL = "https://downloads.photonengine.com/download/fusion/photon-fusion-2.0.12-stable-1861.unitypackage?pre=sp";
    private const string SDK_VERSION = "2.0.12";
    private const string SDK_FILENAME = "photon-fusion-2.unitypackage";
    private const string FUSION_RUNTIME_PATH = "Assets/Photon/Fusion/Fusion.Runtime.dll";

    [InitializeOnLoadMethod]
    private static async void AutoCheckAndInstall()
    {
        // 延迟 2 秒，等 Unity 完全加载
        await Task.Delay(2000);

        // 检查是否已安装
        if (IsFusionInstalled())
        {
            Debug.Log($"[PhotonSDK] Fusion {SDK_VERSION} 已安装，无需重复安装");
            return;
        }

        Debug.Log("[PhotonSDK] 检测到 Photon Fusion 未安装，开始自动下载...");
        Debug.Log($"[PhotonSDK] 下载地址: {SDK_DOWNLOAD_URL}");

        // 显示进度
        bool downloadComplete = false;
        string errorMsg = null;

        try
        {
            // 下载到临时目录
            string tempPath = Path.Combine(Path.GetTempPath(), SDK_FILENAME);
            
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += (sender, e) =>
                {
                    float progress = e.ProgressPercentage;
                    EditorUtility.DisplayProgressBar(
                        "下载 Photon Fusion SDK",
                        $"正在下载 Fusion {SDK_VERSION}... {progress:F0}%",
                        progress / 100f
                    );
                };

                await Task.Run(() => client.DownloadFile(SDK_DOWNLOAD_URL, tempPath));
            }

            EditorUtility.ClearProgressBar();

            if (!File.Exists(tempPath))
            {
                Debug.LogError($"[PhotonSDK] 下载失败，文件不存在: {tempPath}");
                return;
            }

            Debug.Log($"[PhotonSDK] 下载完成: {tempPath} ({GetFileSize(tempPath)})");

            // 导入 package
            Debug.Log("[PhotonSDK] 正在导入 Photon Fusion SDK...");
            AssetDatabase.ImportPackage(tempPath, true);
            
            // 删除临时文件
            try { File.Delete(tempPath); } catch { }

            // 刷新
            AssetDatabase.Refresh();

            Debug.Log("[PhotonSDK] Photon Fusion SDK 导入成功！");
            EditorUtility.DisplayDialog(
                "Photon Fusion SDK",
                $"Photon Fusion {SDK_VERSION} 已成功安装！\n\n请重启 Unity 编辑器以完成初始化。",
                "确定"
            );
        }
        catch (WebException ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[PhotonSDK] 下载失败: {ex.Message}");
            EditorUtility.DisplayDialog(
                "下载失败",
                $"Photon Fusion SDK 下载失败：\n{ex.Message}\n\n请手动下载：\n{SDK_DOWNLOAD_URL}",
                "确定"
            );
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[PhotonSDK] 安装异常: {ex.Message}");
        }
    }

    private static bool IsFusionInstalled()
    {
        return System.IO.File.Exists(FUSION_RUNTIME_PATH);
    }

    private static string GetFileSize(string path)
    {
        long bytes = new FileInfo(path).Length;
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        return $"{bytes / (1024 * 1024):F1} MB";
    }

    /// <summary>
    /// 菜单项：手动触发安装
    /// </summary>
    [MenuItem("Tools/Photon Fusion/自动安装 SDK")]
    private static void ManualInstall()
    {
        AutoCheckAndInstall();
    }

    /// <summary>
    /// 菜单项：检查安装状态
    /// </summary>
    [MenuItem("Tools/Photon Fusion/检查安装状态")]
    private static void CheckStatus()
    {
        if (IsFusionInstalled())
        {
            EditorUtility.DisplayDialog("Photon Fusion", "Photon Fusion SDK 已安装 ✓", "确定");
        }
        else
        {
            bool install = EditorUtility.DisplayDialog(
                "Photon Fusion",
                "Photon Fusion SDK 未安装。\n\n点击「安装」自动下载并导入。",
                "安装", "取消"
            );
            if (install) AutoCheckAndInstall();
        }
    }
#endif
}
