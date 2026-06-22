using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 锁链张力条 UI - 屏幕顶部正中心
/// 颜色：绿色(安全) → 黄色(警告) → 红色(危险)
/// 让玩家无需盯着人物模型，瞄一眼顶部即可知锁链状态
/// </summary>
public class ChainTensionMeter : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private RectTransform meterContainer;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image dangerFlashImage;
    [SerializeField] private Text tensionLabel;
    [SerializeField] private Text tipsText;

    [Header("尺寸配置")]
    [SerializeField] private float meterWidth = 200f;
    [SerializeField] private float meterHeight = 20f;

    [Header("颜色配置")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color criticalColor = new Color(0.5f, 0f, 0f);

    [Header("动画参数")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float flashFrequency = 4f;

    [Header("提示文字")]
    [SerializeField] private string safeText = "安全";
    [SerializeField] private string warningText = "注意！";
    [SerializeField] private string dangerText = "危险！";
    [SerializeField] private string criticalText = "即将断裂！";

    private float currentFill;
    private float targetFill;
    private float fillVelocity;
    private TensionLevel currentLevel = TensionLevel.Safe;
    private CanvasGroup canvasGroup;
    private bool isFlashing;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 设置张力条尺寸
        if (meterContainer != null)
        {
            meterContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, meterWidth);
            meterContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, meterHeight);
        }

        // 初始隐藏
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        // 订阅锁链系统的张力变化事件
        if (ChainSystem.Instance != null)
        {
            ChainSystem.Instance.OnTensionChanged += OnTensionUpdate;
        }
    }

    private void OnDestroy()
    {
        if (ChainSystem.Instance != null)
        {
            ChainSystem.Instance.OnTensionChanged -= OnTensionUpdate;
        }
    }

    private void Update()
    {
        // 平滑更新填充值
        currentFill = Mathf.SmoothDamp(currentFill, targetFill, ref fillVelocity, smoothTime);

        if (fillImage != null)
        {
            fillImage.fillAmount = currentFill;
        }

        // 危险闪烁效果
        if (isFlashing && dangerFlashImage != null)
        {
            float alpha = (Mathf.Sin(Time.time * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            dangerFlashImage.color = new Color(1f, 1f, 1f, alpha * 0.5f);
        }

        // 更新张力条可见性
        float targetAlpha = currentFill > 0.01f ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
    }

    /// <summary>
    /// 张力更新回调
    /// </summary>
    private void OnTensionUpdate(TensionLevel level, float normalizedTension)
    {
        currentLevel = level;
        targetFill = normalizedTension;

        // 更新颜色
        UpdateMeterColor(level);

        // 更新标签文字
        UpdateLabel(level);

        // 更新闪烁状态
        isFlashing = level == TensionLevel.Danger || level == TensionLevel.Critical;

        // 危险时显示提示文字
        if (tipsText != null)
        {
            tipsText.gameObject.SetActive(level >= TensionLevel.Danger);
            switch (level)
            {
                case TensionLevel.Danger:
                    tipsText.text = "⚠ 锁链紧绷！";
                    break;
                case TensionLevel.Critical:
                    tipsText.text = "⚠⚠ 快松手！要断了！";
                    break;
            }
        }
    }

    /// <summary>
    /// 更新张力条颜色
    /// </summary>
    private void UpdateMeterColor(TensionLevel level)
    {
        Color targetColor;

        switch (level)
        {
            case TensionLevel.Safe:
                targetColor = safeColor;
                break;
            case TensionLevel.Warning:
                targetColor = warningColor;
                break;
            case TensionLevel.Danger:
                targetColor = dangerColor;
                break;
            case TensionLevel.Critical:
                targetColor = criticalColor;
                break;
            default:
                targetColor = safeColor;
                break;
        }

        if (fillImage != null)
            fillImage.color = targetColor;
    }

    /// <summary>
    /// 更新标签文字
    /// </summary>
    private void UpdateLabel(TensionLevel level)
    {
        if (tensionLabel == null) return;

        switch (level)
        {
            case TensionLevel.Safe:
                tensionLabel.text = safeText;
                tensionLabel.color = safeColor;
                break;
            case TensionLevel.Warning:
                tensionLabel.text = warningText;
                tensionLabel.color = warningColor;
                break;
            case TensionLevel.Danger:
                tensionLabel.text = dangerText;
                tensionLabel.color = dangerColor;
                break;
            case TensionLevel.Critical:
                tensionLabel.text = criticalText;
                tensionLabel.color = criticalColor;
                break;
        }
    }

    /// <summary>
    /// 强制显示/隐藏张力条
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}