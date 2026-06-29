using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        // 设置构建选项
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        
        // 获取所有场景路径
        string[] scenes = GetEnabledScenes();
        buildPlayerOptions.scenes = scenes;
        
        // 设置输出路径
        string buildPath = Path.Combine(Application.dataPath, "../Builds/Android/");
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);
            
        buildPlayerOptions.locationPathName = Path.Combine(buildPath, "UnityCoopPuppetGame.apk");
        
        // 设置构建目标
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;
        
        // 设置 Android 特定选项
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        
        // 开始构建
        Debug.Log("开始构建 Android APK...");
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;
        
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"构建成功！APK 大小: {summary.totalSize / 1024 / 1024} MB");
            Debug.Log($"APK 位置: {buildPlayerOptions.locationPathName}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("构建失败！");
        }
    }
    
    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}
