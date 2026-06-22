using UnityEngine;

/// <summary>
/// 移动端角色控制器 - 基于 Rigidbody 物理
/// 蓄力跳跃、输入缓冲 0.1s、Vector3.SmoothDamp 平滑同步
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterControllerMobile : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("跳跃参数")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float maxChargeJumpForce = 16f;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("输入缓冲")]
    [SerializeField] private float inputBufferTime = 0.1f;

    [Header("物理同步平滑")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("组件引用")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform modelTransform;
    [SerializeField] private Animator animator;
    [SerializeField] private LayerMask groundLayer = ~0;

    // 移动状态
    private Vector3 moveDirection;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private Vector3 smoothVelocity;

    // 跳跃状态
    private float chargeStartTime;
    private float lastJumpTime;
    private bool jumpBuffered;
    private bool isChargingJump;
    private bool isJumping;
    private int jumpCount;

    // 网络同步（远程玩家使用）
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 remoteVelocity;

    public bool IsGrounded { get; private set; }
    public bool IsMoving => rb.velocity.magnitude > 0.1f;
    public bool IsLocalPlayer { get; set; } = true;
    public float CurrentChargeTime => isChargingJump ? Time.time - chargeStartTime : 0f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (modelTransform == null)
            modelTransform = transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        // 订阅输入事件
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnJump += OnJumpInput;
            MobileInputManager.Instance.OnChargeJump += OnChargeJumpInput;
        }
    }

    private void OnDestroy()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnJump -= OnJumpInput;
            MobileInputManager.Instance.OnChargeJump -= OnChargeJumpInput;
        }
    }

    private void Update()
    {
        GroundCheck();

        if (IsLocalPlayer)
        {
            HandleMovementInput();
            HandleJumpState();
            UpdateAnimation();
            ApplyGravity();

            // 更新网络同步位置
            networkPosition = transform.position;
            networkRotation = modelTransform.rotation;
        }
        else
        {
            // 远程玩家：平滑插值
            SmoothRemoteTransform();
        }
    }

    private void FixedUpdate()
    {
        if (IsLocalPlayer)
        {
            ApplyMovement();
        }
    }

    /// <summary>
    /// 地面检测
    /// </summary>
    private void GroundCheck()
    {
        bool wasGrounded = IsGrounded;
        IsGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );

        if (IsGrounded && !wasGrounded)
        {
            isJumping = false;
            jumpCount = 0;
        }

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.SetGroundedState(IsGrounded);
        }

        Debug.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * (groundCheckDistance + 0.1f), 
            IsGrounded ? Color.green : Color.red);
    }

    /// <summary>
    /// 处理移动输入 - 基于相机视角
    /// </summary>
    private void HandleMovementInput()
    {
        Vector2 input = MobileInputManager.Instance.MovementInput;
        Camera mainCamera = Camera.main;

        if (input.magnitude > 0.1f && mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            moveDirection = cameraForward * input.y + cameraRight * input.x;
            moveDirection.Normalize();

            // 平滑旋转
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            modelTransform.rotation = Quaternion.Slerp(
                modelTransform.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed
            );

            targetVelocity = moveDirection * moveSpeed;
        }
        else
        {
            targetVelocity = Vector3.Lerp(targetVelocity, Vector3.zero, Time.deltaTime * acceleration);
        }
    }

    /// <summary>
    /// 使用 Rigidbody 施加移动力
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 horizontalVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
        Vector3 currentHorizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // 平滑加速
        Vector3 newVelocity = Vector3.SmoothDamp(
            currentHorizontal, 
            horizontalVelocity, 
            ref smoothVelocity, 
            smoothTime, 
            acceleration * 10f
        );

        rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
    }

    /// <summary>
    /// 跳跃输入处理（带输入缓冲）
    /// </summary>
    private void OnJumpInput()
    {
        if (!IsGrounded)
        {
            // 输入缓冲：离开地面后 0.1 秒内仍可跳跃
            jumpBuffered = true;
            Invoke(nameof(ResetJumpBuffer), inputBufferTime);
        }
        else
        {
            ExecuteJump(jumpForce);
        }
    }

    /// <summary>
    /// 蓄力跳跃输入处理
    /// </summary>
    private void OnChargeJumpInput(float chargeProgress)
    {
        if (!IsGrounded && !jumpBuffered) return;

        float chargeTime = Mathf.Clamp01(chargeProgress);
        float force = Mathf.Lerp(jumpForce, maxChargeJumpForce, chargeTime);
        ExecuteJump(force);

        Debug.Log($"[CharacterController] 蓄力跳跃 - 力度: {force:F1}, 进度: {chargeProgress:P}");
    }

    /// <summary>
    /// 执行跳跃
    /// </summary>
    private void ExecuteJump(float force)
    {
        // 重置垂直速度以实现一致的跳跃高度
        Vector3 velocity = rb.velocity;
        velocity.y = 0f;
        rb.velocity = velocity;

        // 使用 AddForce 施加跳跃力
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);

        isJumping = true;
        jumpCount++;
        lastJumpTime = Time.time;
        jumpBuffered = false;

        // 震动反馈
        HapticFeedback.Trigger(HapticType.Light);

        // 更新动画
        if (animator != null)
            animator.SetTrigger("Jump");
    }

    private void ResetJumpBuffer()
    {
        jumpBuffered = false;
    }

    /// <summary>
    /// 处理跳跃状态
    /// </summary>
    private void HandleJumpState()
    {
        if (IsGrounded && isJumping && Time.time - lastJumpTime > 0.2f)
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// 应用重力倍增
    /// </summary>
    private void ApplyGravity()
    {
        if (!IsGrounded && rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1f) * Time.deltaTime;
        }
    }

    /// <summary>
    /// 更新动画参数
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        animator.SetFloat("Speed", speed / moveSpeed);
        animator.SetBool("IsGrounded", IsGrounded);
        animator.SetBool("IsMoving", speed > 0.1f);
    }

    /// <summary>
    /// 远程玩家变换平滑同步
    /// </summary>
    private void SmoothRemoteTransform()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            networkPosition,
            ref currentVelocity,
            smoothTime
        );

        modelTransform.rotation = Quaternion.Slerp(
            modelTransform.rotation,
            networkRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    /// <summary>
    /// 设置网络位置（远程同步用）
    /// </summary>
    public void SetNetworkPosition(Vector3 position) => networkPosition = position;

    /// <summary>
    /// 设置网络旋转（远程同步用）
    /// </summary>
    public void SetNetworkRotation(Quaternion rotation) => networkRotation = rotation;

    /// <summary>
    /// 对外施加力（如锁链拉力）
    /// </summary>
    public void ApplyExternalForce(Vector3 force, ForceMode mode = ForceMode.Force)
    {
        rb.AddForce(force, mode);
    }

    /// <summary>
    /// 获取当前速度
    /// </summary>
    public Vector3 GetVelocity() => rb.velocity;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position - Vector3.up * groundCheckDistance, 0.05f);
    }
}