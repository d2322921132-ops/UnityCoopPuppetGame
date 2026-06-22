using UnityEngine;

/// <summary>
/// 网络玩家 - 本地/远程玩家的网络表示
/// 继承 NetworkBehaviour（SDK 导入后启用）
/// 当前为占位实现，SDK 导入后替换为完整版本
/// </summary>
public class NetworkPlayer : MonoBehaviour // : NetworkBehaviour
{
    [Header("玩家信息")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int playerId = -1;
    [SerializeField] private bool isLocalPlayer = false;

    [Header("同步参数")]
    [SerializeField] private float syncInterpolationSpeed = 15f;
    [SerializeField] private float positionThreshold = 0.01f;
    [SerializeField] private float rotationThreshold = 1f;

    [Header("引用")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Renderer playerRenderer;

    // 网络同步状态
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;

    // 公共属性
    public string PlayerName => playerName;
    public int PlayerId => playerId;
    public bool IsLocalPlayer => isLocalPlayer;
    public bool HasInputAuthority => isLocalPlayer; // 本地玩家有输入权限
    public bool HasStateAuthority => isLocalPlayer; // 简化版：本地玩家有状态权限

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerRenderer == null) playerRenderer = GetComponent<Renderer>();

        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            // 远程玩家 - 插值同步位置
            InterpolateRemotePlayer();
        }
    }

    /// <summary>
    /// 初始化玩家
    /// </summary>
    public void Initialize(int id, string name, bool local)
    {
        playerId = id;
        playerName = name;
        isLocalPlayer = local;

        // 本地玩家使用蓝色，远程使用红色
        if (playerRenderer != null)
        {
            playerRenderer.material.color = local ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.3f, 0.2f);
        }

        Debug.Log($"[NetworkPlayer] 初始化: {name} (ID:{id}, Local:{local})");
    }

    /// <summary>
    /// 设置网络位置（用于接收远程同步数据）
    /// </summary>
    public void SetNetworkPosition(Vector3 pos, Quaternion rot, Vector3 vel)
    {
        networkPosition = pos;
        networkRotation = rot;
        networkVelocity = vel;
    }

    /// <summary>
    /// 远程玩家位置插值
    /// </summary>
    private void InterpolateRemotePlayer()
    {
        if (rb != null)
        {
            // 使用 Rigidbody 插值
            rb.position = Vector3.Lerp(rb.position, networkPosition, Time.deltaTime * syncInterpolationSpeed);
            rb.rotation = Quaternion.Slerp(rb.rotation, networkRotation, Time.deltaTime * syncInterpolationSpeed);
            rb.velocity = Vector3.Lerp(rb.velocity, networkVelocity, Time.deltaTime * syncInterpolationSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * syncInterpolationSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * syncInterpolationSpeed);
        }
    }

    /// <summary>
    /// 获取当前状态数据（用于发送给其他玩家）
    /// </summary>
    public PlayerStateData GetStateData()
    {
        return new PlayerStateData
        {
            Position = transform.position,
            Rotation = transform.rotation,
            Velocity = rb != null ? rb.velocity : Vector3.zero
        };
    }

    private void OnDestroy()
    {
        Debug.Log($"[NetworkPlayer] 销毁: {playerName}");
    }
}

/// <summary>
/// 玩家状态数据 - 用于网络同步
/// </summary>
[System.Serializable]
public struct PlayerStateData
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
}
