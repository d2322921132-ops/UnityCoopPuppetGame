using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 输入管理器 - 处理移动端触摸输入
/// 实现极低延迟的触摸摇杆与点击交互
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("摇杆配置")]
    [SerializeField] private FloatingJoystick movementJoystick;
    [SerializeField] private float joystickDeadzone = 0.15f;

    [Header("交互配置")]
    [SerializeField] private float interactCooldown = 0.3f;

    [Header("输入状态")]
    [SerializeField] private Vector2 movementInput;
    [SerializeField] private bool interactPressed;
    [SerializeField] private bool interactHeld;

    private float interactTimer;
    private bool isInitialized;

    public Vector2 MovementInput => movementInput;
    public bool IsMoving => movementInput.magnitude > joystickDeadzone;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateMovementInput();
        UpdateInteractInput();

        if (interactTimer > 0)
        {
            interactTimer -= Time.deltaTime;
        }
    }

    private void UpdateMovementInput()
    {
        if (movementJoystick != null)
        {
            Vector2 rawInput = movementJoystick.Direction;
            
            if (rawInput.magnitude < joystickDeadzone)
            {
                movementInput = Vector2.zero;
            }
            else
            {
                movementInput = rawInput.normalized * Mathf.Min(rawInput.magnitude, 1f);
            }
        }
        else
        {
            movementInput = GetTouchMovementInput();
        }
    }

    private Vector2 GetTouchMovementInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return Vector2.zero;
            }

            if (touch.position.x < Screen.width * 0.5f)
            {
                Vector2 touchDelta = touch.deltaPosition;
                return touchDelta.normalized * Mathf.Min(touchDelta.magnitude / 10f, 1f);
            }
        }

        return Vector2.zero;
    }

    private void UpdateInteractInput()
    {
        interactPressed = false;
        interactHeld = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            
            if (touch.position.x > Screen.width * 0.5f)
            {
                if (touch.phase == TouchPhase.Began && interactTimer <= 0)
                {
                    interactPressed = true;
                    interactTimer = interactCooldown;
                    HapticFeedback.Trigger(HapticType.Medium);
                }
                
                if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
                {
                    interactHeld = true;
                }
            }
        }

        // 编辑器/PC 测试支持
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width * 0.5f && interactTimer <= 0)
        {
            interactPressed = true;
            interactTimer = interactCooldown;
        }
        if (Input.GetMouseButton(0) && Input.mousePosition.x > Screen.width * 0.5f)
        {
            interactHeld = true;
        }
#endif
    }

    public Vector2 GetMovementInput()
    {
        return movementInput;
    }

    public bool GetInteractInput()
    {
        return interactPressed;
    }

    public bool GetInteractHeld()
    {
        return interactHeld;
    }

    public void SetJoystick(FloatingJoystick joystick)
    {
        movementJoystick = joystick;
    }

    public void DisableInput()
    {
        isInitialized = false;
        movementInput = Vector2.zero;
    }

    public void EnableInput()
    {
        isInitialized = true;
    }
}

/// <summary>
/// 浮动摇杆组件
/// </summary>
public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("摇杆组件")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("摇杆参数")]
    [SerializeField] private float handleRange = 1f;

    private Canvas canvas;
    private Camera cam;
    private Vector2 input;
    private Vector2 origin;
    private CanvasGroup canvasGroup;

    public Vector2 Direction => input;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        cam = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
        input = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        joystickBackground.anchoredPosition = GetAnchoredPosition(eventData.position);
        canvasGroup.alpha = 1f;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 direction = eventData.position - origin;
        input = direction.magnitude > 0 ? direction.normalized : Vector2.zero;
        
        float maxDistance = joystickBackground.sizeDelta.x * 0.5f * handleRange;
        float currentDistance = Mathf.Min(direction.magnitude, maxDistance);
        
        Vector2 handlePosition = input * currentDistance;
        joystickHandle.anchoredPosition = handlePosition;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        canvasGroup.alpha = 0f;
    }

    private Vector2 GetAnchoredPosition(Vector2 screenPosition)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground.parent as RectTransform,
            screenPosition,
            cam,
            out Vector2 localPoint))
        {
            origin = screenPosition;
            return localPoint;
        }
        return Vector2.zero;
    }
}
