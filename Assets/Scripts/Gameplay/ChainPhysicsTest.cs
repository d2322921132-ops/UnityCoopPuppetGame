using UnityEngine;

/// <summary>
/// 锁链物理测试场景控制器
/// 用于 MainGame 测试场景，创建两个带 Rigidbody 的方块并用 Spring Joint 连接
/// </summary>
public class ChainPhysicsTest : MonoBehaviour
{
    [Header("方块配置")]
    [SerializeField] private GameObject playerCubePrefab;
    [SerializeField] private Material player1Material;
    [SerializeField] private Material player2Material;

    [Header("初始位置")]
    [SerializeField] private Vector3 player1StartPos = new Vector3(-2, 1, 0);
    [SerializeField] private Vector3 player2StartPos = new Vector3(2, 1, 0);

    [Header("Spring Joint 配置")]
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float damper = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float tolerance = 0.25f;

    [Header("物理材质")]
    [SerializeField] private PhysicMaterial cubePhysicsMaterial;

    private GameObject player1Cube;
    private GameObject player2Cube;
    private SpringJoint chainJoint;

    void Start()
    {
        CreateTestCubes();
        SetupSpringJoint();
        SetupCamera();
    }

    /// <summary>
    /// 创建两个测试方块
    /// </summary>
    private void CreateTestCubes()
    {
        // 玩家1方块（蓝色）
        player1Cube = CreateCube("Player1_Cube", player1StartPos, player1Material);
        
        // 玩家2方块（红色）
        player2Cube = CreateCube("Player2_Cube", player2StartPos, player2Material);

        Debug.Log("[ChainPhysicsTest] 测试方块已创建");
    }

    /// <summary>
    /// 创建单个方块
    /// </summary>
    private GameObject CreateCube(string name, Vector3 position, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * 0.8f;

        // 设置材质
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.material = material;
        }

        // 添加 Rigidbody
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 添加物理材质
        Collider col = cube.GetComponent<Collider>();
        if (col != null && cubePhysicsMaterial != null)
        {
            col.material = cubePhysicsMaterial;
        }

        // 添加测试控制器脚本
        cube.AddComponent<ChainCubeController>();

        return cube;
    }

    /// <summary>
    /// 设置 Spring Joint 连接两个方块
    /// </summary>
    private void SetupSpringJoint()
    {
        if (player1Cube == null || player2Cube == null) return;

        // 在 Player1 上添加 Spring Joint，连接到 Player2
        chainJoint = player1Cube.AddComponent<SpringJoint>();
        chainJoint.connectedBody = player2Cube.GetComponent<Rigidbody>();
        
        // 配置 Spring Joint 参数
        chainJoint.spring = springForce;
        chainJoint.damper = damper;
        chainJoint.minDistance = minDistance;
        chainJoint.maxDistance = maxDistance;
        chainJoint.tolerance = tolerance;
        chainJoint.autoConfigureConnectedAnchor = false;
        chainJoint.anchor = Vector3.zero;
        chainJoint.connectedAnchor = Vector3.zero;

        Debug.Log($"[ChainPhysicsTest] Spring Joint 已连接: {player1Cube.name} <-> {player2Cube.name}");
        Debug.Log($"  - Spring Force: {springForce}");
        Debug.Log($"  - Damper: {damper}");
        Debug.Log($"  - Min Distance: {minDistance}");
        Debug.Log($"  - Max Distance: {maxDistance}");
    }

    /// <summary>
    /// 设置相机视角
    /// </summary>
    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("MainCamera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }

        // 相机位置在两个方块中间上方
        Vector3 midPoint = (player1StartPos + player2StartPos) / 2f;
        mainCamera.transform.position = midPoint + new Vector3(0, 8, -6);
        mainCamera.transform.LookAt(midPoint);

        // 添加相机跟随脚本
        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
    }

    /// <summary>
    /// 获取当前锁链距离
    /// </summary>
    public float GetCurrentDistance()
    {
        if (player1Cube == null || player2Cube == null) return 0f;
        return Vector3.Distance(player1Cube.transform.position, player2Cube.transform.position);
    }

    /// <summary>
    /// 获取当前张力（基于距离）
    /// </summary>
    public float GetCurrentTension()
    {
        float distance = GetCurrentDistance();
        if (distance <= minDistance) return 0f;
        return Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
    }

    void OnGUI()
    {
        // 在屏幕上显示调试信息
        GUILayout.BeginArea(new Rect(10, 10, 250, 120));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== 锁链物理测试 ===", new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Label($"当前距离: {GetCurrentDistance():F2}");
        GUILayout.Label($"当前张力: {GetCurrentTension():P}");
        GUILayout.Label($"Spring Force: {springForce}");
        GUILayout.Label($"Damper: {damper}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

/// <summary>
/// 锁链测试方块控制器 - 用于 ChainPhysicsTest 场景
/// </summary>
public class ChainCubeController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 地面检测
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        // 获取输入
        float horizontal = 0f;
        float vertical = 0f;

        if (gameObject.name.Contains("Player1"))
        {
            // WASD 控制 Player1
            horizontal = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
            vertical = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;

            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
        else
        {
            // 方向键控制 Player2
            horizontal = Input.GetKey(KeyCode.RightArrow) ? 1f : Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f;
            vertical = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;

            if (Input.GetKeyDown(KeyCode.Return) && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        // 应用移动
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * moveSpeed;
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
    }
}
