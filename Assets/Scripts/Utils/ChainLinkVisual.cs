using UnityEngine;

/// <summary>
/// 锁链视觉组件 - 用 LineRenderer 在两名玩家之间绘制动态锁链
/// 颜色随张力变化：绿色 → 黄色 → 红色
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ChainLinkVisual : MonoBehaviour
{
    [Header("玩家引用")]
    public Transform player1;
    public Transform player2;

    [Header("锁链参数")]
    public LineRenderer chainRenderer;
    [SerializeField] private int segmentCount = 24;
    [SerializeField] private float waveAmplitude = 0.15f;
    [SerializeField] private float waveFrequency = 4f;

    [Header("张力参数")]
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float damper = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 6f;

    [Header("颜色")]
    public Color safeColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    private void Awake()
    {
        if (chainRenderer == null)
            chainRenderer = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        if (player1 == null || player2 == null || chainRenderer == null) return;

        Vector3 start = player1.position;
        Vector3 end = player2.position;
        float distance = Vector3.Distance(start, end);

        // 计算张力 (0~1)
        float tension = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));

        // 更新 LineRenderer
        chainRenderer.positionCount = segmentCount;
        Vector3[] positions = new Vector3[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 basePos = Vector3.Lerp(start, end, t);

            // 波浪效果
            float wave = Mathf.Sin(t * Mathf.PI * waveFrequency + Time.time * 2f) * waveAmplitude * (1f + tension);
            Vector3 right = Vector3.Cross((end - start).normalized, Vector3.up).normalized;
            if (right == Vector3.zero) right = Vector3.right;
            basePos += right * wave;

            positions[i] = basePos;
        }

        chainRenderer.SetPositions(positions);

        // 颜色
        Color color;
        if (tension < 0.3f)
            color = Color.Lerp(safeColor, warningColor, tension / 0.3f);
        else if (tension < 0.7f)
            color = Color.Lerp(warningColor, dangerColor, (tension - 0.3f) / 0.4f);
        else
            color = Color.Lerp(dangerColor, Color.black, (tension - 0.7f) / 0.3f);

        chainRenderer.startColor = color;
        chainRenderer.endColor = color;

        // 宽度随张力变化
        float width = 0.06f + tension * 0.06f;
        chainRenderer.startWidth = width;
        chainRenderer.endWidth = width;
    }

    private void OnDestroy()
    {
        if (chainRenderer != null)
        {
            chainRenderer.positionCount = 0;
        }
    }
}