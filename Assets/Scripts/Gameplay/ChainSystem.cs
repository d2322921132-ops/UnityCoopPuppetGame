using UnityEngine;
using System;

/// <summary>
/// 锁链系统 - 双人协作核心机制
/// 管理两个玩家之间的锁链连接、张力检测、反向阻力
/// 张力达到阈值时触发 UI 警告和物理反馈
/// </summary>
public class ChainSystem : MonoBehaviour
{
    public static ChainSystem Instance { get; private set; }

    [Header("玩家引用")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;
    [SerializeField] private CharacterControllerMobile player1Controller;
    [SerializeField] private CharacterControllerMobile player2Controller;

    [Header("锁链物理参数")]
    [SerializeField] private float maxChainLength = 8f;
    [SerializeField] private float optimalChainLength = 4f;
    [SerializeField] private float tensionSpringForce = 10f;
    [SerializeField] private float tensionDamper = 2f;

    [Header("张力阈值")]
    [SerializeField] private float safeTensionThreshold = 0.3f;   // 绿色 → 黄色
    [SerializeField] private float dangerTensionThreshold = 0.7f; // 黄色 → 红色

    [Header("锁链断裂/坠落")]
    [SerializeField] private float breakTension = 1.2f;
    [SerializeField] private float tensionRecoveryRate = 0.5f;

    [Header("视觉效果")]
    [SerializeField] private LineRenderer chainRenderer;
    [SerializeField] private AnimationCurve chainWaveCurve;

    // 事件
    public event Action<TensionLevel, float> OnTensionChanged;
    public event Action OnChainBreak;
    public event Action OnPartnerPulled;

    // 张力状态
    private float currentTension;
    private float distance;
    private Vector3 pullDirection;
    private TensionLevel currentTensionLevel = TensionLevel.Safe;

    public float CurrentTension => currentTension;
    public float Distance => distance;
    public Vector3 PullDirection => pullDirection;
    public TensionLevel TensionLevel => currentTensionLevel;
    public float MaxChainLength => maxChainLength;
    public float NormalizedTension => Mathf.Clamp01(currentTension / breakTension);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        UpdateChainPhysics();
        UpdateChainVisual();
    }

    /// <summary>
    /// 更新锁链物理和张力
    /// </summary>
    private void UpdateChainPhysics()
    {
        // 计算距离和方向
        distance = Vector3.Distance(player1.position, player2.position);
        pullDirection = (player2.position - player1.position).normalized;

        // 计算张力值
        float tensionRaw;
        if (distance <= optimalChainLength)
        {
            // 在最佳距离内，张力为 0
            tensionRaw = 0f;
        }
        else
        {
            // 超过最佳距离，张力线性增加
            float overStretch = distance - optimalChainLength;
            float maxOverStretch = maxChainLength - optimalChainLength;
            tensionRaw = Mathf.Clamp01(overStretch / maxOverStretch);
        }

        // 使用 Lerp 平滑张力变化（阻尼效果）
        currentTension = Mathf.Lerp(currentTension, tensionRaw, Time.deltaTime * tensionDamper);

        // 如果超出最大距离，施加拉力
        if (distance > maxChainLength * 0.8f)
        {
            ApplyTensionForce();
        }

        // 如果张力超过断裂阈值
        if (tensionRaw > breakTension)
        {
            OnChainBreak?.Invoke();
            Debug.Log("[ChainSystem] 锁链断裂！");
        }

        // 更新张力等级
        UpdateTensionLevel();
    }

    /// <summary>
    /// 施加张力反向阻力
    /// </summary>
    private void ApplyTensionForce()
    {
        float forceMagnitude = tensionSpringForce * (distance - maxChainLength * 0.8f) / (maxChainLength * 0.2f);

        // 向对方施加拉力
        if (player1Controller != null)
        {
            Vector3 forceToPlayer1 = pullDirection * forceMagnitude;
            player1Controller.ApplyExternalForce(forceToPlayer1);
        }

        if (player2Controller != null)
        {
            Vector3 forceToPlayer2 = -pullDirection * forceMagnitude;
            player2Controller.ApplyExternalForce(forceToPlayer2);
        }

        OnPartnerPulled?.Invoke();
    }

    /// <summary>
    /// 更新张力等级
    /// </summary>
    private void UpdateTensionLevel()
    {
        float normalizedTension = NormalizedTension;
        TensionLevel newLevel;

        if (normalizedTension <= safeTensionThreshold)
        {
            newLevel = TensionLevel.Safe;       // 绿色
        }
        else if (normalizedTension <= dangerTensionThreshold)
        {
            newLevel = TensionLevel.Warning;    // 黄色
        }
        else if (normalizedTension < 1f)
        {
            newLevel = TensionLevel.Danger;     // 红色
        }
        else
        {
            newLevel = TensionLevel.Critical;   // 断裂边缘
        }

        if (newLevel != currentTensionLevel)
        {
            currentTensionLevel = newLevel;
            OnTensionChanged?.Invoke(newLevel, normalizedTension);
        }
        else
        {
            // 持续更新张力值
            OnTensionChanged?.Invoke(currentTensionLevel, normalizedTension);
        }
    }

    /// <summary>
    /// 锁链视觉效果 - 使用 LineRenderer 绘制波浪形锁链
    /// </summary>
    private void UpdateChainVisual()
    {
        if (chainRenderer == null) return;

        int segments = 20;
        Vector3[] positions = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 basePos = Vector3.Lerp(player1.position, player2.position, t);

            // 添加波浪效果（张力越大波浪越明显）
            float waveHeight = chainWaveCurve.Evaluate(t) * (0.2f + currentTension * 0.5f);
            float waveX = Mathf.Sin(t * Mathf.PI * 4 + Time.time * 2f) * waveHeight;
            float waveZ = Mathf.Cos(t * Mathf.PI * 3 + Time.time * 1.5f) * waveHeight;

            basePos += transform.right * waveX + transform.forward * waveZ;
            positions[i] = basePos;
        }

        chainRenderer.positionCount = segments;
        chainRenderer.SetPositions(positions);

        // 张力颜色映射
        Color tensionColor = GetTensionColor();
        chainRenderer.startColor = tensionColor;
        chainRenderer.endColor = tensionColor;

        // 宽度随张力变化
        chainRenderer.startWidth = 0.08f + currentTension * 0.04f;
        chainRenderer.endWidth = 0.08f + currentTension * 0.04f;
    }

    /// <summary>
    /// 获取张力颜色
    /// </summary>
    public Color GetTensionColor()
    {
        float normalized = NormalizedTension;

        if (normalized <= safeTensionThreshold)
        {
            // 绿色 → 黄色
            float t = normalized / safeTensionThreshold;
            return Color.Lerp(Color.green, Color.yellow, t);
        }
        else if (normalized <= dangerTensionThreshold)
        {
            // 黄色 → 红色
            float t = (normalized - safeTensionThreshold) / (dangerTensionThreshold - safeTensionThreshold);
            return Color.Lerp(Color.yellow, Color.red, t);
        }
        else
        {
            // 红色 → 深红（濒临断裂）
            float t = Mathf.Clamp01((normalized - dangerTensionThreshold) / (1f - dangerTensionThreshold));
            return Color.Lerp(Color.red, new Color(0.5f, 0f, 0f), t);
        }
    }

    /// <summary>
    /// 初始化锁链系统（设置玩家引用）
    /// </summary>
    public void Setup(Transform p1, Transform p2, CharacterControllerMobile c1, CharacterControllerMobile c2)
    {
        player1 = p1;
        player2 = p2;
        player1Controller = c1;
        player2Controller = c2;
    }
}

public enum TensionLevel
{
    Safe,       // 绿色 - 安全
    Warning,    // 黄色 - 警告
    Danger,     // 红色 - 危险
    Critical    // 深红 - 濒临断裂
}