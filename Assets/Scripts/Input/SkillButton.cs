using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 技能按钮 - 右下角半透明技能槽
/// 用于情侣互动技能：拉对方一把、互相拥抱回血等
/// </summary>
public class SkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("技能配置")]
    [SerializeField] private string skillName = "Pull Partner";
    [SerializeField] private float cooldownTime = 5f;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text cooldownText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("技能事件")]
    public UnityEvent OnSkillActivated;

    private bool isOnCooldown = false;
    private float cooldownRemaining = 0f;
    private int touchId = -1;

    public bool IsOnCooldown => isOnCooldown;
    public string SkillName => skillName;

    private void Update()
    {
        if (isOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            float progress = cooldownRemaining / cooldownTime;

            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = progress;

            if (cooldownText != null)
                cooldownText.text = Mathf.CeilToInt(cooldownRemaining).ToString();

            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                if (cooldownOverlay != null)
                    cooldownOverlay.gameObject.SetActive(false);
                if (cooldownText != null)
                    cooldownText.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isOnCooldown) return;

        touchId = eventData.pointerId;
        transform.localScale = Vector3.one * 0.85f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;

        transform.localScale = Vector3.one;
        touchId = -1;

        if (isOnCooldown) return;

        ActivateSkill();
    }

    private void ActivateSkill()
    {
        OnSkillActivated?.Invoke();
        StartCooldown();

        // 震动反馈
        HapticFeedback.Trigger(HapticType.Medium);

        Debug.Log($"[SkillButton] 技能触发: {skillName}");
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownRemaining = cooldownTime;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(true);
            cooldownOverlay.fillAmount = 1f;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = Mathf.CeilToInt(cooldownTime).ToString();
        }
    }

    public void ResetCooldown()
    {
        isOnCooldown = false;
        cooldownRemaining = 0f;
        if (cooldownOverlay != null)
            cooldownOverlay.gameObject.SetActive(false);
        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);
    }
}