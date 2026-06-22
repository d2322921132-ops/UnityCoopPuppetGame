using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

/// <summary>
/// 移动端动作按钮 - 右侧核心交互区
/// 支持：短按跳跃、长按蓄力跳跃、双击锁链交互
/// 使用 EventSystem 事件驱动，输入缓冲 0.1 秒
/// </summary>
public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("按钮配置")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField] private string buttonName = "ActionButton";

    [Header("时间参数")]
    [SerializeField] private float longPressThreshold = 0.3f;
    [SerializeField] private float doubleClickTimeWindow = 0.25f;
    [SerializeField] private float inputBufferingTime = 0.1f;

    [Header("蓄力指示")]
    [SerializeField] private RectTransform chargeIndicator;
    [SerializeField] private float maxChargeScale = 2f;

    [Header("事件")]
    public UnityEvent OnShortTap;
    public UnityEvent OnLongPress;
    public UnityEvent OnDoubleTap;
    public UnityEvent<float> OnChargeUpdate; // 蓄力进度 0~1

    private float pressStartTime;
    private float lastClickTime;
    private int clickCount;
    private bool isPressed;
    private bool isLongPressTriggered;
    private int touchId = -1;

    public bool IsPressed => isPressed;
    public bool IsLongPressTriggered => isLongPressTriggered;
    public float CurrentChargeTime => isPressed ? Time.time - pressStartTime : 0f;
    public float ChargeProgress => Mathf.Clamp01(CurrentChargeTime / 1.5f);

    private void Update()
    {
        if (isPressed)
        {
            float charge = ChargeProgress;
            OnChargeUpdate?.Invoke(charge);

            // 更新蓄力指示器
            if (chargeIndicator != null)
            {
                float scale = 1f + charge * (maxChargeScale - 1f);
                chargeIndicator.localScale = Vector3.Lerp(
                    chargeIndicator.localScale,
                    Vector3.one * scale,
                    Time.deltaTime * 10f
                );
            }

            // 检测长按触发
            if (!isLongPressTriggered && CurrentChargeTime >= longPressThreshold)
            {
                isLongPressTriggered = true;
                OnLongPress?.Invoke();
                Debug.Log($"[{buttonName}] 长按触发 - 蓄力跳跃");
            }
        }

        // 双击检测窗口
        if (clickCount > 0 && Time.time - lastClickTime > doubleClickTimeWindow)
        {
            clickCount = 0;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        touchId = eventData.pointerId;
        isPressed = true;
        isLongPressTriggered = false;
        pressStartTime = Time.time;

        // 缩放动画
        if (buttonRect != null)
        {
            buttonRect.localScale = Vector3.one * 0.85f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;

        isPressed = false;
        touchId = -1;

        // 恢复按钮大小
        if (buttonRect != null)
        {
            buttonRect.localScale = Vector3.one;
        }

        // 重置蓄力指示器
        if (chargeIndicator != null)
        {
            chargeIndicator.localScale = Vector3.one;
        }

        // 如果已经触发了长按，不再处理短按
        if (isLongPressTriggered)
        {
            isLongPressTriggered = false;
            return;
        }

        float pressDuration = Time.time - pressStartTime;

        // 双击检测
        clickCount++;
        if (clickCount >= 2 && Time.time - lastClickTime <= doubleClickTimeWindow)
        {
            clickCount = 0;
            OnDoubleTap?.Invoke();
            Debug.Log($"[{buttonName}] 双击触发 - 锁链交互");
            return;
        }

        lastClickTime = Time.time;

        // 短按 → 跳跃（带输入缓冲）
        if (pressDuration < longPressThreshold)
        {
            // 输入缓冲：使用 Invoke 延迟执行，在 0.1 秒内可被后续输入覆盖
            Invoke(nameof(ExecuteShortTap), inputBufferingTime);
        }
    }

    private void ExecuteShortTap()
    {
        // 如果按钮已被再次按下或已释放太久，取消执行
        if (isPressed) return;

        OnShortTap?.Invoke();
        Debug.Log($"[{buttonName}] 短按触发 - 跳跃");
    }

    /// <summary>
    /// 取消输入缓冲（防止误触）
    /// </summary>
    public void CancelBufferedInput()
    {
        CancelInvoke(nameof(ExecuteShortTap));
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ExecuteShortTap));
        isPressed = false;
        isLongPressTriggered = false;
        clickCount = 0;

        if (buttonRect != null)
            buttonRect.localScale = Vector3.one;
        if (chargeIndicator != null)
            chargeIndicator.localScale = Vector3.one;
    }
}