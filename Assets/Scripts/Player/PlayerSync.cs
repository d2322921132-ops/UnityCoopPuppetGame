using UnityEngine;

#if FUSION_ENABLED
using Fusion;
#endif

#if FUSION_ENABLED

/// <summary>
/// PlayerSync (Photon Fusion 版本) - 角色生成、移动同步、安卓触摸控制
/// 
/// 功能：
/// 1. 安卓触摸摇杆控制（左半屏）
/// 2. 使用 NetworkTransform 自动同步位置和旋转
/// 3. 使用 NetworkRigidbody 同步物理
/// 4. 本地玩家蓝色，远程玩家红色
/// 
/// 挂载方式：将此脚本拖到你的玩家预制体（Player Prefab）上，
/// 然后在 NetworkManager 的 Player Prefab 槽位拖入这个预制体
/// </summary>
public class PlayerSync : NetworkBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("触摸控制")]
    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private bool showDebugUI = true;

    [Header("组件引用")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Renderer playerRenderer;

    // 网络同步变量
    [Networked] private Vector3 NetworkPosition { get; set; }
    [Networked] private Quaternion NetworkRotation { get; set; }
    [Networked] private Vector3 NetworkVelocity { get; set; }

    // 触摸输入状态
    private Vector2 moveInput;
    private int touchId = -1;
    private Vector2 joystickCenter;
    private bool isGrounded;

    // 组件
    private NetworkTransform networkTransform;
    private NetworkRigidbody networkRigidbody;

    public override void Spawned()
    {
        // 获取或添加必要组件
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        playerRenderer = GetComponentInChildren<Renderer>();
        networkTransform = GetComponent<NetworkTransform>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        // 确保有碰撞器
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 1f, 0);
        }

        // 设置颜色：本地玩家蓝色，远程玩家红色
        if (playerRenderer != null)
        {
            playerRenderer.material.color = Object.HasInputAuthority ? Color.blue : Color.red;
        }

        // 配置 NetworkTransform
        if (networkTransform != null)
        {
            networkTransform.SyncPosition = true;
            networkTransform.SyncRotation = true;
            networkTransform.SyncScale = false;
            networkTransform.PositionInterpolationSpeed = 15f;
            networkTransform.RotationInterpolationSpeed = 10f;
        }

        Debug.Log($"[PlayerSync] 玩家生成: ID={Object.InputAuthority.PlayerId}, 本地={Object.HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        // 只有本地玩家可以控制
        if (Object.HasInputAuthority)
        {
            HandleLocalInput();
            ApplyMovement();
        }
        else
        {
            // 远程玩家使用插值
            InterpolateRemotePlayer();
        }
    }

    public override void Render()
    {
        // 渲染帧更新（用于平滑插值）
        if (!Object.HasInputAuthority && networkTransform != null)
        {
            // NetworkTransform 自动处理插值
        }
    }

    #region 本地输入处理

    private void HandleLocalInput()
    {
        // 只在有输入权限时处理
        if (!Object.HasInputAuthority) return;

        // 处理触摸输入
        HandleTouchInput();

        // 键盘备用（编辑器测试）
        if (Input.GetKey(KeyCode.W)) moveInput.y = 1;
        else if (Input.GetKey(KeyCode.S)) moveInput.y = -1;
        else if (moveInput.y > 0 && !Input.GetKey(KeyCode.W)) moveInput.y = 0;

        if (Input.GetKey(KeyCode.D)) moveInput.x = 1;
        else if (Input.GetKey(KeyCode.A)) moveInput.x = -1;
        else if (moveInput.x > 0 && !Input.GetKey(KeyCode.D)) moveInput.x = 0;

        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            if (touchId != -1)
            {
                moveInput = Vector2.zero;
                touchId = -1;
            }
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 screenPos = touch.position;

            // 左半屏摇杆
            if (screenPos.x < Screen.width * 0.5f)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchId = touch.fingerId;
                        joystickCenter = screenPos;
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
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
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == touchId)
                        {
                            moveInput = Vector2.zero;
                            touchId = -1;
                        }
                        break;
                }
            }

            // 右半屏跳跃（点击屏幕右下）
            if (touch.phase == TouchPhase.Began &&
                screenPos.x > Screen.width * 0.5f &&
                screenPos.y < Screen.height * 0.4f &&
                isGrounded)
            {
                Jump();
            }
        }
    }

    #endregion

    #region 移动逻辑

    private void ApplyMovement()
    {
        if (rb == null) return;

        // 地面检测
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);

        // 计算移动方向
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y);

        // 应用速度
        rb.velocity = new Vector3(moveDir.x * moveSpeed, rb.velocity.y, moveDir.z * moveSpeed);

        // 旋转朝向
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Runner.DeltaTime);
        }

        // 同步网络数据
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;
        NetworkVelocity = rb.velocity;
    }

    private void InterpolateRemotePlayer()
    {
        if (rb == null) return;

        // NetworkTransform 会自动处理插值
        // 这里可以添加额外的预测或平滑逻辑
    }

    private void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("[PlayerSync] 跳跃");
    }

    #endregion

    #region 调试 UI

    private void OnGUI()
    {
        if (!showDebugUI || !Object.HasInputAuthority) return;

        // 摇杆可视化
        if (touchId != -1)
        {
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTexture(
                new Rect(joystickCenter.x - joystickRadius, Screen.height - joystickCenter.y - joystickRadius,
                         joystickRadius * 2, joystickRadius * 2),
                Texture2D.whiteTexture);

            Vector2 stickPos = joystickCenter + moveInput * joystickRadius;
            GUI.color = new Color(1, 1, 1, 0.6f);
            GUI.DrawTexture(
                new Rect(stickPos.x - 30, Screen.height - stickPos.y - 30, 60, 60),
                Texture2D.whiteTexture);
        }

        // 状态信息
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 20), $"Player: {Object.InputAuthority.PlayerId}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Move: {moveInput:F2}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 70, 300, 20), $"Velocity: {rb.velocity.magnitude:F2}");
    }

    #endregion
}

#else

/// <summary>
/// PlayerSync (本地单机版本) - 无 Photon Fusion SDK 时使用的替代实现
/// 
/// 提供基本的本地移动和触摸控制功能，不做网络同步。
/// 保持与 Fusion 版本相同的公共接口，确保其他脚本无需修改。
/// 
/// 要启用 Fusion 网络同步功能，请在 Unity 的 Player Settings -> Scripting Define Symbols 中添加 FUSION_ENABLED
/// </summary>
public class PlayerSync : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("触摸控制")]
    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private bool showDebugUI = true;

    [Header("组件引用")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Renderer playerRenderer;

    // 触摸输入状态
    private Vector2 moveInput;
    private int touchId = -1;
    private Vector2 joystickCenter;
    private bool isGrounded;

    private void Start()
    {
        // 获取或添加必要组件
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        playerRenderer = GetComponentInChildren<Renderer>();

        // 确保有碰撞器
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 1f, 0);
        }

        // 本地模式下设置玩家颜色为蓝色
        if (playerRenderer != null)
        {
            playerRenderer.material.color = Color.blue;
        }

        Debug.Log("[PlayerSync] 本地单机模式：玩家初始化完成");
    }

    private void FixedUpdate()
    {
        // 本地模式下直接处理输入和移动
        HandleLocalInput();
        ApplyMovement();
    }

    #region 本地输入处理

    /// <summary>
    /// 处理本地输入（触摸摇杆 + 键盘）
    /// </summary>
    private void HandleLocalInput()
    {
        // 处理触摸输入
        HandleTouchInput();

        // 键盘备用（编辑器测试）
        if (Input.GetKey(KeyCode.W)) moveInput.y = 1;
        else if (Input.GetKey(KeyCode.S)) moveInput.y = -1;
        else if (moveInput.y > 0 && !Input.GetKey(KeyCode.W)) moveInput.y = 0;

        if (Input.GetKey(KeyCode.D)) moveInput.x = 1;
        else if (Input.GetKey(KeyCode.A)) moveInput.x = -1;
        else if (moveInput.x > 0 && !Input.GetKey(KeyCode.D)) moveInput.x = 0;

        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    /// <summary>
    /// 处理触摸摇杆输入
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            if (touchId != -1)
            {
                moveInput = Vector2.zero;
                touchId = -1;
            }
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 screenPos = touch.position;

            // 左半屏摇杆
            if (screenPos.x < Screen.width * 0.5f)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchId = touch.fingerId;
                        joystickCenter = screenPos;
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
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
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.fingerId == touchId)
                        {
                            moveInput = Vector2.zero;
                            touchId = -1;
                        }
                        break;
                }
            }

            // 右半屏跳跃（点击屏幕右下）
            if (touch.phase == TouchPhase.Began &&
                screenPos.x > Screen.width * 0.5f &&
                screenPos.y < Screen.height * 0.4f &&
                isGrounded)
            {
                Jump();
            }
        }
    }

    #endregion

    #region 移动逻辑

    /// <summary>
    /// 应用移动（本地物理模拟）
    /// </summary>
    private void ApplyMovement()
    {
        if (rb == null) return;

        // 地面检测
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);

        // 计算移动方向
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y);

        // 应用速度
        rb.velocity = new Vector3(moveDir.x * moveSpeed, rb.velocity.y, moveDir.z * moveSpeed);

        // 旋转朝向（本地版使用 Time.fixedDeltaTime 代替 Runner.DeltaTime）
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 跳跃
    /// </summary>
    private void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        Debug.Log("[PlayerSync] 跳跃");
    }

    #endregion

    #region 调试 UI

    private void OnGUI()
    {
        if (!showDebugUI) return;

        // 摇杆可视化
        if (touchId != -1)
        {
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTexture(
                new Rect(joystickCenter.x - joystickRadius, Screen.height - joystickCenter.y - joystickRadius,
                         joystickRadius * 2, joystickRadius * 2),
                Texture2D.whiteTexture);

            Vector2 stickPos = joystickCenter + moveInput * joystickRadius;
            GUI.color = new Color(1, 1, 1, 0.6f);
            GUI.DrawTexture(
                new Rect(stickPos.x - 30, Screen.height - stickPos.y - 30, 60, 60),
                Texture2D.whiteTexture);
        }

        // 状态信息（本地模式显示简化信息）
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 300, 20), "Player: 本地玩家");
        GUI.Label(new Rect(10, 30, 300, 20), $"Move: {moveInput:F2}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Grounded: {isGrounded}");
        if (rb != null)
        {
            GUI.Label(new Rect(10, 70, 300, 20), $"Velocity: {rb.velocity.magnitude:F2}");
        }
    }

    #endregion
}

#endif
