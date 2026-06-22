using UnityEngine;

/// <summary>
/// 玩家控制器 - 处理玩家输入、移动和网络同步
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("组件引用")]
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform modelTransform;

    [Header("网络同步")]
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private Vector3 networkPosition;
    [SerializeField] private Quaternion networkRotation;

    private Vector3 currentVelocity;
    private Vector3 moveDirection;
    private Camera mainCamera;

    public bool IsLocalPlayer => isLocalPlayer;
    public Vector3 CurrentPosition => transform.position;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (modelTransform == null)
            modelTransform = transform;
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            HandleLocalInput();
        }
        else
        {
            ApplyRemoteState();
        }
    }

    /// <summary>
    /// 处理本地玩家输入
    /// </summary>
    private void HandleLocalInput()
    {
        Vector2 input = InputManager.Instance.GetMovementInput();
        
        // 计算移动方向（基于相机视角）
        moveDirection = Vector3.zero;
        if (input.magnitude > 0.1f)
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
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 应用移动
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
        
        if (characterController != null)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }

        // 更新动画
        UpdateAnimation(input.magnitude);

        // 处理交互输入
        if (InputManager.Instance.GetInteractInput())
        {
            PerformInteraction();
        }

        // 同步网络位置
        networkPosition = transform.position;
        networkRotation = modelTransform.rotation;
    }

    /// <summary>
    /// 应用远程玩家的网络状态
    /// </summary>
    private void ApplyRemoteState()
    {
        // 使用插值平滑同步远程玩家位置
        transform.position = Vector3.SmoothDamp(
            transform.position,
            networkPosition,
            ref currentVelocity,
            smoothTime
        );

        modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, networkRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 执行交互动作
    /// </summary>
    private void PerformInteraction()
    {
        animator?.SetTrigger("Interact");
        
        // 触发震动反馈
        HapticFeedback.Trigger(HapticType.Light);
        
        Debug.Log("[PlayerController] 执行交互动作");
    }

    private void UpdateAnimation(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsMoving", speed > 0.1f);
        }
    }

    /// <summary>
    /// 设置网络位置（用于远程玩家同步）
    /// </summary>
    public void SetNetworkPosition(Vector3 position)
    {
        networkPosition = position;
    }

    /// <summary>
    /// 设置网络旋转（用于远程玩家同步）
    /// </summary>
    public void SetNetworkRotation(Quaternion rotation)
    {
        networkRotation = rotation;
    }

    /// <summary>
    /// 设置为本地玩家或远程玩家
    /// </summary>
    public void SetLocalPlayer(bool local)
    {
        isLocalPlayer = local;
        
        if (local)
        {
            // 本地玩家：启用相机跟随
            var cameraFollow = mainCamera?.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
            }
        }
    }
}
