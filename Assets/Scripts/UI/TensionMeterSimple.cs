using UnityEngine;

/// <summary>
/// 简易张力条 UI - 屏幕左上角显示锁链距离和张力量
/// 由 GameBootstrap 自动创建
/// </summary>
public class TensionMeterSimple : MonoBehaviour
{
    private Transform player1;
    private Transform player2;
    private float minDist = 2f;
    private float maxDist = 6f;

    private void Start()
    {
        // 延时获取引用（等待 GameBootstrap 创建完方块）
        Invoke(nameof(FindPlayers), 0.5f);
    }

    private void FindPlayers()
    {
        GameObject p1 = GameObject.Find("Player1_Cube");
        GameObject p2 = GameObject.Find("Player2_Cube");

        if (p1 != null) player1 = p1.transform;
        if (p2 != null) player2 = p2.transform;

        // 从 GameBootstrap 读取参数
        if (GameBootstrap.Instance != null)
        {
            minDist = GameBootstrap.Instance.ChainJoint?.minDistance ?? 2f;
            maxDist = GameBootstrap.Instance.ChainJoint?.maxDistance ?? 6f;
        }
    }

    private void OnGUI()
    {
        if (player1 == null || player2 == null) return;

        float dist = Vector3.Distance(player1.position, player2.position);
        float tension = Mathf.Clamp01((dist - minDist) / (maxDist - minDist));

        // 上方居中张力条
        float barWidth = 220;
        float barHeight = 18;
        float x = (Screen.width - barWidth) / 2f;
        float y = 12;

        GUILayout.BeginArea(new Rect(x - 40, y - 8, barWidth + 80, 80));
        GUILayout.BeginVertical();

        GUILayout.Label($"锁链张力", new GUIStyle { 
            fontSize = 12, fontStyle = FontStyle.Bold,
            normal = new GUIStyleState { textColor = Color.white },
            alignment = TextAnchor.MiddleCenter
        });

        // 背景
        Rect bgRect = GUILayoutUtility.GetRect(barWidth, barHeight);
        GUI.Box(bgRect, "");

        // 填充
        Color barColor = tension < 0.3f ? Color.green : tension < 0.7f ? Color.yellow : Color.red;
        GUI.color = barColor;
        Rect fillRect = new Rect(bgRect.x + 2, bgRect.y + 2, (bgRect.width - 4) * tension, bgRect.height - 4);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 标签
        string label = tension < 0.3f ? "安全" : tension < 0.7f ? "注意！" : tension < 1f ? "危险！" : "即将断裂！";
        GUI.Label(new Rect(bgRect.x + bgRect.width + 8, bgRect.y, 60, bgRect.height), label, new GUIStyle {
            fontSize = 11, fontStyle = FontStyle.Bold,
            normal = new GUIStyleState { textColor = barColor }
        });

        GUILayout.Space(4);
        GUILayout.Label($"距离: {dist:F1} / {maxDist:F1} m", new GUIStyle {
            fontSize = 10, normal = new GUIStyleState { textColor = Color.gray },
            alignment = TextAnchor.MiddleCenter
        });

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}