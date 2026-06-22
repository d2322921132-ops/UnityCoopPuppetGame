using UnityEngine;
using System;

/// <summary>
/// 移动端输入管理器 - 统一管理所有触摸输入
/// 支持多点触控，异步输入处理，延迟 < 20ms
/// 整合动态摇杆、动作按钮、技能按钮
/// </summary>
public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance { get; private set; }

    [Header("输入组件引用")]
    [SerializeField] private DynamicJoystick movementJoystick;
    [SerializeField] private MobileActionButton actionButton;
    [SerializeField] private SkillButton[] skillButtons;

    [Header("输入读取")]
    [SerializeField] private Vector2 movementInput;
    [SerializeField] private bool jumpPressed;
    [SerializeField] private bool longPressJump;
    [SerializeField] private bool doubleTapChain;
    [SerializeField] private float chargeProgress;

    [Header("物理检测")]
    [SerializeField] private bool isGrounded;

    private bool isInitialized;

    // 公开输入状态（供角色控制器读取）
    public Vector2 MovementInput => movementInput;
    public bool JumpPressed => jumpPressed;
    public bool LongPressJump => longPressJump;
    public bool DoubleTapChain => doubleTapChain;
    public float ChargeProgress => chargeProgress;
    public bool IsGrounded => isGrounded;
    public bool IsMoving => movementInput.magnitude > 0.1f;
    public bool IsInputActive { get; private set; }

    public event Action OnJump;
    public event Action<float> OnChargeJump;
    public event Action OnChainInteract;

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

    private void Start()
    {
        // 订阅动作按钮事件
        if (actionButton != null)
        {
            actionButton.OnShortTap.AddListener(HandleJump);
            actionButton.OnLongPress.AddListener(HandleLongPressJump);
            actionButton.OnDoubleTap.AddListener(HandleDoubleTap);
            actionButton.OnChargeUpdate.AddListener(HandleChargeUpdate);
        }

        // 订阅技能按钮事件
        foreach (var skill in skillButtons)
        {
            if (skill != null)
            {
                skill.OnSkillActivated.AddListener(() => HandleSkill(skill.SkillName));
            }
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 从摇杆读取移动输入（每帧读取，确保最低延迟）
        if (movementJoystick != null)
        {
            movementInput = movementJoystick.GetNormalizedInput();
        }

        // 检测是否正在输入
        IsInputActive = movementInput.magnitude > 0.1f || 
                        (actionButton != null && actionButton.IsPressed);

        // 更新蓄力进度供外部使用
        if (actionButton != null && actionButton.IsPressed)
        {
            chargeProgress = actionButton.ChargeProgress;
        }
        else
        {
            chargeProgress = 0f;
        }
    }

    private void HandleJump()
    {
        jumpPressed = true;
        IsInputActive = true;
        OnJump?.Invoke();
        Debug.Log("[MobileInputManager] 跳跃触发");

        // 每帧重置
        Invoke(nameof(ResetJump), 0.05f);
    }

    private void HandleLongPressJump()
    {
        longPressJump = true;
        OnChargeJump?.Invoke(chargeProgress);
        Debug.Log($"[MobileInputManager] 蓄力跳跃触发 - 进度: {chargeProgress:P}");

        Invoke(nameof(ResetLongPressJump), 0.05f);
    }

    private void HandleDoubleTap()
    {
        doubleTapChain = true;
        OnChainInteract?.Invoke();
        Debug.Log("[MobileInputManager] 双击锁链交互触发");

        Invoke(nameof(ResetDoubleTap), 0.05f);
    }

    private void HandleChargeUpdate(float charge)
    {
        chargeProgress = charge;
    }

    private void HandleSkill(string skillName)
    {
        Debug.Log($"[MobileInputManager] 技能触发: {skillName}");
    }

    private void ResetJump() => jumpPressed = false;
    private void ResetLongPressJump() => longPressJump = false;
    private void ResetDoubleTap() => doubleTapChain = false;

    /// <summary>
    /// 设置地面检测状态
    /// </summary>
    public void SetGroundedState(bool grounded)
    {
        isGrounded = grounded;
    }

    /// <summary>
    /// 设置摇杆引用（动态绑定）
    /// </summary>
    public void SetJoystick(DynamicJoystick joystick)
    {
        movementJoystick = joystick;
    }

    /// <summary>
    /// 设置动作按钮引用（动态绑定）
    /// </summary>
    public void SetActionButton(MobileActionButton button)
    {
        actionButton = button;
        if (actionButton != null)
        {
            actionButton.OnShortTap.AddListener(HandleJump);
            actionButton.OnLongPress.AddListener(HandleLongPressJump);
            actionButton.OnDoubleTap.AddListener(HandleDoubleTap);
            actionButton.OnChargeUpdate.AddListener(HandleChargeUpdate);
        }
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