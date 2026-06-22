using UnityEngine;
using System.Collections;

/// <summary>
/// 游戏引导启动器 - 自动创建物理锁链测试场景
/// 点击 Play 后自动执行，无需手动挂载到任何对象
/// 包含两个 Rigidbody 方块、Spring Joint、锁链视觉、张力条
/// 
/// 操作方式:
///   Player1 (蓝色): WASD + Space
///   Player2 (红色): 方向键 + Enter
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("启动模式")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableMultiplayer = false;
    [SerializeField] private string photonAppId = "";

    [Header("方块配置")]
    [SerializeField] private Material player1Material;
    [SerializeField] private Material player2Material;
    [SerializeField] private Vector3 player1StartPos = new Vector3(-3, 1, 0);
    [SerializeField] private Vector3 player2StartPos = new Vector3(3, 1, 0);

    [Header("Spring Joint 参数")]
    [SerializeField] private float springForce = 50f;
    [SerializeField] private float damper = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 6f;

    public static GameBootstrap Instance { get; private set; }

    public GameObject Player1 { get; private set; }
    public GameObject Player2 { get; private set; }
    public SpringJoint ChainJoint { get; private set; }

    /// <summary>
    /// 自动初始化 - 无需挂载到场景对象
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance != null) return;

        GameObject bootstrapObj = new GameObject("GameBootstrap");
        bootstrapObj.AddComponent<GameBootstrap>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (autoSetupOnStart)
        {
            StartCoroutine(SetupScene());
        }
    }

    private IEnumerator SetupScene()
    {
        yield return new WaitForSeconds(0.1f);

        // 1. 设置地面
        CreateGround();

        // 2. 创建两个方块
        Player1 = CreatePlayerCube("Player1_Cube", player1StartPos, player1Material, 0.2f, 0.5f, 1f);
        Player2 = CreatePlayerCube("Player2_Cube", player2StartPos, player2Material, 1f, 0.3f, 0.2f);

        // 3. 连接 Spring Joint
        SetupSpringJoint();

        // 4. 创建锁链视觉效果
        CreateChainVisual();

        // 5. 设置相机
        SetupCamera();

        // 6. 创建张力条 UI
        CreateTensionMeterUI();

        // 7. 连接 Photon
        if (enableMultiplayer && !string.IsNullOrEmpty(photonAppId))
        {
            SetupPhoton();
        }

        Debug.Log("[GameBootstrap] 场景初始化完成！");
        Debug.Log("  Player1 (蓝色): WASD 移动, Space 跳跃");
        Debug.Log("  Player2 (红色): 方向键移动, Enter 跳跃");
    }

    /// <summary>
    /// 创建地面
    /// </summary>
    private void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = Vector3.one * 5f;
        ground.transform.position = new Vector3(0, -0.5f, 0);

        // 添加物理材质
        var collider = ground.GetComponent<Collider>();
        if (collider != null)
        {
            PhysicMaterial groundMat = new PhysicMaterial("GroundMat");
            groundMat.staticFriction = 0.6f;
            groundMat.dynamicFriction = 0.6f;
            groundMat.bounciness = 0.1f;
            collider.material = groundMat;
        }

        // 添加围墙
        CreateWall("Wall_North", new Vector3(0, 2, -12), new Vector3(24, 4, 0.5f));
        CreateWall("Wall_South", new Vector3(0, 2, 12), new Vector3(24, 4, 0.5f));
        CreateWall("Wall_West", new Vector3(-12, 2, 0), new Vector3(0.5f, 4, 24));
        CreateWall("Wall_East", new Vector3(12, 2, 0), new Vector3(0.5f, 4, 24));
    }

    private void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.4f);
    }

    /// <summary>
    /// 创建玩家方块
    /// </summary>
    private GameObject CreatePlayerCube(string name, Vector3 position, Material material, float r, float g, float b)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = Vector3.one * 0.8f;

        // 材质
        Renderer renderer = cube.GetComponent<Renderer>();
        if (material != null)
        {
            renderer.material = material;
        }
        else
        {
            renderer.material.color = new Color(r, g, b);
        }

        // Rigidbody
        Rigidbody rb = cube.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 碰撞器
        var col = cube.GetComponent<Collider>();
        PhysicMaterial cubeMat = new PhysicMaterial("CubeMat");
        cubeMat.staticFriction = 0.3f;
        cubeMat.dynamicFriction = 0.3f;
        cubeMat.bounciness = 0.2f;
        col.material = cubeMat;

        // 添加输入脚本
        var controller = cube.AddComponent<CubeTestController>();
        controller.playerIndex = name.Contains("2") ? 1 : 0;

        // 添加弹簧关节标记
        if (name.Contains("Player1"))
        {
            cube.AddComponent<SpringJoint>();
        }

        return cube;
    }

    /// <summary>
    /// 设置 Spring Joint 连接两个方块
    /// </summary>
    private void SetupSpringJoint()
    {
        if (Player1 == null || Player2 == null) return;

        ChainJoint = Player1.GetComponent<SpringJoint>();
        ChainJoint.connectedBody = Player2.GetComponent<Rigidbody>();
        ChainJoint.spring = springForce;
        ChainJoint.damper = damper;
        ChainJoint.minDistance = minDistance;
        ChainJoint.maxDistance = maxDistance;
        ChainJoint.tolerance = 0.25f;
        ChainJoint.autoConfigureConnectedAnchor = false;
        ChainJoint.anchor = Vector3.zero;
        ChainJoint.connectedAnchor = Vector3.zero;

        Debug.Log($"[GameBootstrap] Spring Joint 已连接！");
        Debug.Log($"  弹簧力: {springForce}, 阻尼: {damper}, 距离: {minDistance}~{maxDistance}");
    }

    /// <summary>
    /// 创建锁链视觉效果
    /// </summary>
    private void CreateChainVisual()
    {
        GameObject chainObj = new GameObject("ChainVisual");
        var chainVisual = chainObj.AddComponent<ChainLinkVisual>();
        chainVisual.player1 = Player1.transform;
        chainVisual.player2 = Player2.transform;

        // LineRenderer
        LineRenderer lr = chainObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.positionCount = 20;
        lr.useWorldSpace = true;
        lr.startColor = Color.green;
        lr.endColor = Color.green;
        chainVisual.chainRenderer = lr;
    }

    /// <summary>
    /// 设置相机
    /// </summary>
    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
        }

        cam.transform.position = new Vector3(0, 10, -10);
        cam.transform.rotation = Quaternion.Euler(35, 0, 0);
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    /// <summary>
    /// 创建张力条 UI（简易版）
    /// </summary>
    private void CreateTensionMeterUI()
    {
        GameObject uiObj = new GameObject("TensionMeter");
        uiObj.AddComponent<TensionMeterSimple>();
    }

    /// <summary>
    /// 配置 Photon
    /// </summary>
    private void SetupPhoton()
    {
        if (!string.IsNullOrEmpty(photonAppId))
        {
            var photonSetup = gameObject.AddComponent<PhotonFusionSetup>();
            photonSetup.SetAppId(photonAppId);
        }
    }

    private void OnGUI()
    {
        if (Player1 == null || Player2 == null) return;

        // 计算距离和张力
        float dist = Vector3.Distance(Player1.transform.position, Player2.transform.position);
        float tension = Mathf.Clamp01((dist - minDistance) / (maxDistance - minDistance));

        GUILayout.BeginArea(new Rect(10, 10, 260, 160));
        GUILayout.BeginVertical("box");

        GUILayout.Label("🔄 锁链物理测试", new GUIStyle { 
            fontSize = 14, fontStyle = FontStyle.Bold, normal = new GUIStyleState { textColor = Color.white } 
        });
        GUILayout.Space(5);

        GUILayout.Label($"当前距离: {dist:F2} / {maxDistance:F2}");
        GUILayout.Label($"张力等级: {GetTensionText(tension)} ({tension:P})");

        // 简易张力条
        GUILayout.Space(5);
        Rect barRect = GUILayoutUtility.GetRect(200, 20);
        GUI.DrawTexture(barRect, Texture2D.grayTexture);

        Color barColor = tension < 0.3f ? Color.green : tension < 0.7f ? Color.yellow : Color.red;
        GUI.color = barColor;
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * tension, barRect.height);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.Space(5);
        GUILayout.Label("Player1 (蓝色): WASD + Space", new GUIStyle { fontSize = 10 });
        GUILayout.Label("Player2 (红色): 方向键 + Enter", new GUIStyle { fontSize = 10 });

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private string GetTensionText(float tension)
    {
        if (tension < 0.3f) return "🟢 安全";
        if (tension < 0.7f) return "🟡 注意";
        if (tension < 1f) return "🔴 危险";
        return "⛔ 即将断裂！";
    }
}