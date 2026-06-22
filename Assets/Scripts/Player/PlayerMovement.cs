using UnityEngine;

/// <summary>
/// PlayerMovement - 玩家移动控制器
/// 
/// 功能：
/// 1. 安卓触摸摇杆控制（左半屏）
/// 2. 自动添加 Rigidbody + Collider + NetworkTransform
/// 3. 位置同步（SDK 导入后启用 NetworkTransform）
/// 4. 跳跃、冲刺、技能按钮（右半屏）
/// 
/// 安卓打包注意（ProjectSettings 必须配置）：
/// - PlayerSettings > Android > Package Name: com.yourcompany.couplegame
/// - PlayerSettings > Android > Minimum API Level: Android 8.0 (API 26)
/// - PlayerSettings > Android > Target API Level: Automatic (highest installed)
/// - PlayerSettings > Other Settings > Internet Access: Require
/// - PlayerSettings > Publishing Settings > 创建 keystore 用于签名
/// - Edit > Project Settings > Input System Package: 确保启用（用于触摸输入）
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.3f;

    [Header("触摸控制")]
    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private Vector2 joystickCenter = new Vector2(200, 200);
    [SerializeField] private bool showDebugUI = true;

    [Header("组件引用（自动绑定）")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private Animator animator;

    // 运行时状态
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isDashing;
    private float dashTimer;
    private int touchId = -1;

    // 属性
    public bool IsLocalPlayer => true; // SDK 导入后根据 NetworkObject 判断
    public Vector3 Velocity => rb != null ? rb.velocity : Vector3.zero;

    private void Awake()
    {
        AutoBindComponents();
    }

    private void Start()
    {
        // SDK 导入后启用 NetworkTransform：
        // var netTransform = gameObject.AddComponent<NetworkTransform>();
        // netTransform.SyncScale = false;
        // netTransform.SyncRotation = false;
    }

    private void Update()
    {
        HandleTouchInput();
        HandleJumpInput();
        HandleDashInput();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // 地面检测
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);

        // 移动
        if (!isDashing)
        {
            Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y);
            rb.velocity = new Vector3(moveDir.x * moveSpeed, rb.velocity.y, moveDir.z * moveSpeed);

            // 朝向
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            // 冲刺中
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0) isDashing = false;
        }
    }

    #region 触摸输入处理

    /// <summary>
    /// 处理触摸摇杆输入
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            moveInput = Vector2.zero;
            touchId = -1;
            return;
        }

        // 查找左半屏的触摸
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 screenPos = touch.position;

            // 左半屏检测（屏幕宽度的左 50%）
            if (screenPos.x < Screen.width * 0.5f)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    touchId = touch.fingerId;
                    joystickCenter = screenPos;
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (touch.fingerId == touchId)
                    {
                        Vector2 delta = screenPos - joystickCenter;
                        float distance = delta.magnitude;

                        if (distance > joystickRadius)
                        {
                            delta = delta.normalized * joystickRadius;
                        }

                        moveInput = delta / joystickRadius;
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (touch.fingerId == touchId)
                    {
                        moveInput = Vector2.zero;
                        touchId = -1;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理跳跃输入（右半屏点击）
    /// </summary>
    private void HandleJumpInput()
    {
        if (!isGrounded) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && touch.position.x > Screen.width * 0.5f)
            {
                // 检查是否点击了跳跃按钮区域（屏幕右下）
                if (touch.position.y < Screen.height * 0.4f)
                {
                    Jump();
                    break;
                }
            }
        }

        // 键盘备用（编辑器测试）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }

    /// <summary>
    /// 处理冲刺输入（双击右半屏）
    /// </summary>
    private void HandleDashInput()
    {
        // 简化版：长按右半屏上区域冲刺
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Stationary && touch.position.x > Screen.width * 0.5f)
            {
                if (touch.position.y > Screen.height * 0.6f && !isDashing)
                {
                    Dash();
                    break;
                }
            }
        }

        // 键盘备用
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Dash();
        }
    }

    #endregion

    #region 动作方法

    private void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("[PlayerMovement] 跳跃");
    }

    private void Dash()
    {
        if (isDashing) return;
        isDashing = true;
        dashTimer = dashDuration;

        Vector3 dashDir = transform.forward;
        rb.velocity = dashDir * dashSpeed;

        Debug.Log("[PlayerMovement] 冲刺");
    }

    #endregion

    #region 自动绑定

    /// <summary>
    /// 自动绑定所需组件
    /// </summary>
    private void AutoBindComponents()
    {
        // Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Debug.Log("[PlayerMovement] 自动添加 Rigidbody");
        }

        // Collider
        col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<CapsuleCollider>();
            ((CapsuleCollider)col).height = 2f;
            ((CapsuleCollider)col).radius = 0.5f;
            ((CapsuleCollider)col).center = new Vector3(0, 1f, 0);
            Debug.Log("[PlayerMovement] 自动添加 CapsuleCollider");
        }

        // Animator（可选）
        animator = GetComponent<Animator>();

        // 确保有渲染器
        if (GetComponent<Renderer>() == null)
        {
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = Color.blue;
            Debug.Log("[PlayerMovement] 自动添加渲染器");
        }
    }

    #endregion

    #region 动画

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDashing", isDashing);
    }

    #endregion

    #region 调试

    private void OnGUI()
    {
        if (!showDebugUI) return;

        // 摇杆可视化
        if (touchId != -1)
        {
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTexture(new Rect(joystickCenter.x - joystickRadius, Screen.height - joystickCenter.y - joystickRadius, joystickRadius * 2, joystickRadius * 2), Texture2D.whiteTexture);

            Vector2 stickPos = joystickCenter + moveInput * joystickRadius;
            GUI.color = new Color(1, 1, 1, 0.6f);
            GUI.DrawTexture(new Rect(stickPos.x - 30, Screen.height - stickPos.y - 30, 60, 60), Texture2D.whiteTexture);
        }

        // 状态信息
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 20), $"Move: {moveInput:F2}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Dashing: {isDashing}");
    }

    #endregion
}
