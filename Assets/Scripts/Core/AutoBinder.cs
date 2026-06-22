using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// AutoBinder - 游戏启动时自动完成所有组件绑定
/// 
/// 功能：
/// 1. 自动创建 NetworkManager 并绑定 ConnectionManager
/// 2. 自动创建 Player 并绑定 PlayerMovement
/// 3. 自动设置相机跟随
/// 4. 自动配置场景基础环境（地面、灯光）
/// 5. 零 Inspector 拖拽，全部代码自动完成
/// 
/// 安卓打包注意（ProjectSettings 必须配置）：
/// - PlayerSettings > Android > Package Name: com.yourcompany.couplegame
/// - PlayerSettings > Android > Minimum API Level: Android 8.0 (API 26)
/// - PlayerSettings > Android > Target API Level: Automatic (highest installed)
/// - PlayerSettings > Other Settings > Internet Access: Require
/// - PlayerSettings > Other Settings > Scripting Backend: IL2CPP（推荐）或 Mono
/// - PlayerSettings > Other Settings > Target Architectures: ARMv7 + ARM64
/// - PlayerSettings > Publishing Settings > 创建 keystore 用于签名
/// - Edit > Project Settings > XR Plug-in Management > 如需 VR 则启用
/// </summary>
public class AutoBinder : MonoBehaviour
{
    [Header("自动生成配置")]
    [SerializeField] private bool autoCreateNetworkManager = true;
    [SerializeField] private bool autoCreatePlayer = true;
    [SerializeField] private bool autoSetupCamera = true;
    [SerializeField] private bool autoSetupEnvironment = true;

    [Header("玩家配置")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPos = new Vector3(0, 1, 0);
    [SerializeField] private Color playerColor = Color.blue;

    [Header("相机配置")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 10, -8);
    [SerializeField] private float cameraSmoothSpeed = 5f;

    // 运行时引用
    public static AutoBinder Instance { get; private set; }
    public GameObject LocalPlayer { get; private set; }
    public Camera MainCamera { get; private set; }
    public ConnectionManager ConnectionManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        Debug.Log("[AutoBinder] 开始自动绑定...");

        if (autoSetupEnvironment)
            SetupEnvironment();

        if (autoCreateNetworkManager)
            SetupNetworkManager();

        if (autoCreatePlayer)
            await CreatePlayerAsync();

        if (autoSetupCamera)
            SetupCamera();

        Debug.Log("[AutoBinder] 自动绑定完成！");
    }

    #region 网络管理器

    private void SetupNetworkManager()
    {
        // 查找或创建 NetworkManager
        GameObject netObj = GameObject.Find("NetworkManager");
        if (netObj == null)
        {
            netObj = new GameObject("NetworkManager");
            netObj.AddComponent<ConnectionManager>();
            Debug.Log("[AutoBinder] 自动创建 NetworkManager + ConnectionManager");
        }

        ConnectionManager = netObj.GetComponent<ConnectionManager>();
        if (ConnectionManager == null)
        {
            ConnectionManager = netObj.AddComponent<ConnectionManager>();
        }
    }

    #endregion

    #region 玩家创建

    private async System.Threading.Tasks.Task CreatePlayerAsync()
    {
        // 等待网络初始化
        await System.Threading.Tasks.Task.Delay(100);

        GameObject player;

        if (playerPrefab != null)
        {
            player = Instantiate(playerPrefab, playerSpawnPos, Quaternion.identity);
        }
        else
        {
            player = CreateDefaultPlayer();
        }

        player.name = "LocalPlayer";
        LocalPlayer = player;

        // 确保有 PlayerMovement
        var movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = player.AddComponent<PlayerMovement>();
        }

        // 设置颜色
        var renderer = player.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = playerColor;
        }

        Debug.Log("[AutoBinder] 玩家创建完成");
    }

    private GameObject CreateDefaultPlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.transform.position = playerSpawnPos;

        // 移除默认碰撞器（PlayerMovement 会自动添加）
        var defaultCol = player.GetComponent<Collider>();
        if (defaultCol != null) Destroy(defaultCol);

        return player;
    }

    #endregion

    #region 相机设置

    private void SetupCamera()
    {
        MainCamera = Camera.main;
        if (MainCamera == null)
        {
            GameObject camObj = new GameObject("MainCamera");
            MainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            Debug.Log("[AutoBinder] 自动创建 MainCamera");
        }

        // 添加跟随脚本
        var follow = MainCamera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = MainCamera.gameObject.AddComponent<CameraFollow>();
        }

        // 等待玩家创建完成后设置目标
        StartCoroutine(SetCameraTargetDelayed(follow));
    }

    private System.Collections.IEnumerator SetCameraTargetDelayed(CameraFollow follow)
    {
        yield return new WaitForSeconds(0.2f);

        if (LocalPlayer != null)
        {
            follow.SetTarget(LocalPlayer.transform);
            follow.SetOffset(cameraOffset);
            follow.SetSmoothSpeed(cameraSmoothSpeed);
            Debug.Log("[AutoBinder] 相机已绑定到玩家");
        }
    }

    #endregion

    #region 环境设置

    private void SetupEnvironment()
    {
        // 创建地面
        if (GameObject.Find("Ground") == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);

            var renderer = ground.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            renderer.material.color = new Color(0.2f, 0.3f, 0.2f);

            Debug.Log("[AutoBinder] 自动创建地面");
        }

        // 创建方向光
        if (GameObject.Find("DirectionalLight") == null && GameObject.Find("Sun") == null)
        {
            GameObject light = new GameObject("DirectionalLight");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.5f;
            lightComp.color = new Color(1f, 0.95f, 0.8f);
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            Debug.Log("[AutoBinder] 自动创建方向光");
        }

        // 创建环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.8f;
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

/// <summary>
/// 相机跟随组件 - 自动绑定用
/// </summary>
public class CameraFollow : MonoBehaviour
{
    private Transform target;
    private Vector3 offset = new Vector3(0, 10, -8);
    private float smoothSpeed = 5f;

    public void SetTarget(Transform t) => target = t;
    public void SetOffset(Vector3 o) => offset = o;
    public void SetSmoothSpeed(float s) => smoothSpeed = s;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
