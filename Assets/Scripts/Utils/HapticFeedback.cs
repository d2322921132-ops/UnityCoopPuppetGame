using UnityEngine;

/// <summary>
/// 震动反馈工具类 - 封装移动端触觉反馈
/// </summary>
public static class HapticFeedback
{
    public static void Trigger(HapticType type)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        TriggerAndroidHaptic(type);
#elif UNITY_IOS && !UNITY_EDITOR
        TriggeriOSHaptic(type);
#else
        Debug.Log($"[HapticFeedback] {type} 震动反馈触发");
#endif
    }

#if UNITY_ANDROID
    private static void TriggerAndroidHaptic(HapticType type)
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    long[] pattern = GetVibrationPattern(type);
                    if (pattern.Length > 0)
                    {
                        vibrator.Call("vibrate", pattern, -1);
                    }
                }
            }
        }
    }
#endif

#if UNITY_IOS
    private static void TriggeriOSHaptic(HapticType type)
    {
        switch (type)
        {
            case HapticType.Light:
                Handheld.Vibrate();
                break;
            case HapticType.Medium:
                Handheld.Vibrate();
                break;
            case HapticType.Heavy:
                Handheld.Vibrate();
                break;
        }
    }
#endif

    private static long[] GetVibrationPattern(HapticType type)
    {
        switch (type)
        {
            case HapticType.Light:
                return new long[] { 0, 20 };
            case HapticType.Medium:
                return new long[] { 0, 40 };
            case HapticType.Heavy:
                return new long[] { 0, 60, 50, 30 };
            default:
                return new long[] { 0, 30 };
        }
    }
}

public enum HapticType
{
    Light,
    Medium,
    Heavy
}
