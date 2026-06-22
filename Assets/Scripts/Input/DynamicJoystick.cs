using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 动态虚拟摇杆 - 左下角 30% 屏幕区域
/// 触摸即跟随手指位置出现，松手即消失
/// 支持多点触控，独立处理移动输入
/// </summary>
public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("摇杆组件")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("摇杆参数")]
    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0.15f;
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float backgroundScale = 1.2f;

    [Header("屏幕区域限制")]
    [Range(0f, 1f)]
    [SerializeField] private float screenAreaLeft = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float screenAreaRight = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float screenAreaBottom = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float screenAreaTop = 0.5f;

    private Canvas canvas;
    private Camera cam;
    private Vector2 input = Vector2.zero;
    private Vector2 origin;
    private int touchId = -1;
    private bool isPressed = false;
    private float targetAlpha = 0f;

    public Vector2 Direction => input;
    public bool IsPressed => isPressed;
    public float Magnitude => input.magnitude;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        cam = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        joystickBackground.localScale = Vector3.zero;
    }

    private void Update()
    {
        // 平滑淡入淡出
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // 背景缩放动画
        Vector3 targetScale = isPressed ? Vector3.one * backgroundScale : Vector3.zero;
        joystickBackground.localScale = Vector3.Lerp(
            joystickBackground.localScale,
            targetScale,
            Time.deltaTime * fadeSpeed
        );

        // 手柄回中动画
        if (!isPressed)
        {
            joystickHandle.anchoredPosition = Vector2.Lerp(
                joystickHandle.anchoredPosition,
                Vector2.zero,
                Time.deltaTime * fadeSpeed
            );
            input = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 检查触摸位置是否在允许的屏幕区域
        Vector2 touchPos = eventData.position;
        float screenX = touchPos.x / Screen.width;
        float screenY = touchPos.y / Screen.height;

        if (screenX < screenAreaLeft || screenX > screenAreaRight ||
            screenY < screenAreaBottom || screenY > screenAreaTop)
            return;

        touchId = eventData.pointerId;
        isPressed = true;
        targetAlpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 将摇杆背景定位到触摸位置
        Vector2 anchoredPos = GetAnchoredPosition(touchPos);
        joystickBackground.anchoredPosition = anchoredPos;
        origin = touchPos;

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;

        Vector2 direction = eventData.position - origin;
        float distance = direction.magnitude;

        // 计算摇杆范围的最大距离
        float maxDistance = joystickBackground.sizeDelta.x * 0.5f * handleRange;

        if (distance > deadZone)
        {
            // 归一化方向并应用范围限制
            input = direction.normalized * Mathf.Min(distance, maxDistance) / maxDistance;
        }
        else
        {
            input = Vector2.zero;
        }

        // 更新手柄位置（带范围限制）
        float clampedDistance = Mathf.Min(distance, maxDistance);
        Vector2 handlePos = direction.normalized * clampedDistance;
        joystickHandle.anchoredPosition = handlePos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != touchId) return;

        isPressed = false;
        targetAlpha = 0f;
        canvasGroup.blocksRaycasts = false;
        touchId = -1;
        input = Vector2.zero;
    }

    private Vector2 GetAnchoredPosition(Vector2 screenPosition)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground.parent as RectTransform,
            screenPosition,
            cam,
            out Vector2 localPoint))
        {
            return localPoint;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 获取归一化的移动输入向量
    /// </summary>
    public Vector2 GetNormalizedInput()
    {
        float magnitude = input.magnitude;
        if (magnitude < deadZone)
            return Vector2.zero;
        return input.normalized * Mathf.Min(magnitude, 1f);
    }
}