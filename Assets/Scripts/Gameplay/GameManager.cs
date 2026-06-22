using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 游戏管理器 - 核心游戏逻辑控制器
/// 负责游戏状态管理、存档数据收集、场景切换、联机房间创建
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏状态")]
    [SerializeField] private GameState currentState = GameState.MainMenu;
    [SerializeField] private string currentScene;
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int currentExp;
    [SerializeField] private int gold;

    [Header("玩家数据")]
    [SerializeField] private Vector3 playerPosition;
    [SerializeField] private string playerName = "Player";

    [Header("联机房间配置")]
    [SerializeField] private string defaultRoomName = "CoopRoom";
    [SerializeField] private int maxPlayersPerRoom = 2;
    [SerializeField] private bool autoJoinRoom = true;

    [Header("场景列表")]
    [SerializeField] private List<string> gameScenes = new List<string>
    {
        "MainMenu",
        "MainGame",
        "Level1",
        "Level2"
    };

    public GameState CurrentState => currentState;
    public string CurrentScene => currentScene;
    public int PlayerLevel => playerLevel;
    public int Gold => gold;
    public string PlayerName => playerName;
    public bool IsInRoom { get; private set; }

    public event Action<GameState> OnGameStateChanged;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action<bool> OnRoomJoined;
    public event Action<string> OnSceneLoaded;

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

    private void Start()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnLoadComplete += HandleSaveDataLoaded;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectionStatusChanged += HandleConnectionStatusChanged;
            NetworkManager.Instance.OnConnectedToMaster += OnConnectedToMaster;
        }
    }

    private void OnDestroy()
    {
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnLoadComplete -= HandleSaveDataLoaded;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectionStatusChanged -= HandleConnectionStatusChanged;
            NetworkManager.Instance.OnConnectedToMaster -= OnConnectedToMaster;
        }
    }

    #region 游戏状态管理

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                MobileInputManager.Instance?.EnableInput();
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                MobileInputManager.Instance?.DisableInput();
                OnGamePaused?.Invoke();
                break;
            case GameState.MainMenu:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0.5f;
                break;
        }
    }

    public void PauseGame()
    {
        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        ChangeState(GameState.Playing);
        OnGameResumed?.Invoke();
    }

    #endregion

    #region 场景管理

    public void LoadScene(string sceneName)
    {
        if (!gameScenes.Contains(sceneName))
        {
            Debug.LogWarning($"[GameManager] 场景 '{sceneName}' 不在场景列表中");
        }

        currentScene = sceneName;
        SceneManager.LoadScene(sceneName);
        AudioManager.Instance?.SwitchBGMForScene(sceneName);
        OnSceneLoaded?.Invoke(sceneName);
    }

    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            yield return null;
        }

        currentScene = sceneName;
        AudioManager.Instance?.SwitchBGMForScene(sceneName);
        OnSceneLoaded?.Invoke(sceneName);
    }

    public void LoadMainGameScene()
    {
        LoadScene("MainGame");
        ChangeState(GameState.Playing);
    }

    #endregion

    #region 联机房间管理

    /// <summary>
    /// 连接到服务器并自动创建/加入房间
    /// </summary>
    public void StartMultiplayer()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[GameManager] NetworkManager 未初始化");
            return;
        }

        if (NetworkManager.Instance.Status == ConnectionStatus.Connected)
        {
            // 已连接，直接创建/加入房间
            CreateOrJoinRoom();
        }
        else
        {
            // 先连接服务器
            NetworkManager.Instance.ConnectToServer();
        }
    }

    /// <summary>
    /// 创建或加入房间
    /// </summary>
    public void CreateOrJoinRoom(string roomName = null)
    {
        string targetRoom = string.IsNullOrEmpty(roomName) ? defaultRoomName : roomName;

        if (NetworkManager.Instance == null) return;

        // 尝试加入已有房间
        NetworkManager.Instance.JoinRoom(targetRoom);
    }

    /// <summary>
    /// 创建新房间
    /// </summary>
    public void CreateRoom(string roomName = null)
    {
        string targetRoom = string.IsNullOrEmpty(roomName) ? defaultRoomName : roomName;
        NetworkManager.Instance?.CreateRoom(targetRoom);
        IsInRoom = true;
        OnRoomJoined?.Invoke(true);
    }

    /// <summary>
    /// 随机加入房间
    /// </summary>
    public void JoinRandomRoom()
    {
        NetworkManager.Instance?.JoinRandomRoom();
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public void LeaveRoom()
    {
        NetworkManager.Instance?.Disconnect();
        IsInRoom = false;
        OnRoomJoined?.Invoke(false);
    }

    /// <summary>
    /// 连接成功后自动加入房间
    /// </summary>
    private void OnConnectedToMaster()
    {
        Debug.Log("[GameManager] 已连接到 Photon Master Server");

        if (autoJoinRoom)
        {
            CreateOrJoinRoom();
        }
    }

    #endregion

    #region 存档管理

    public GameSaveData CollectSaveData()
    {
        GameSaveData saveData = new GameSaveData
        {
            playerName = playerName,
            playerLevel = playerLevel,
            currentExp = currentExp,
            gold = gold,
            currentScene = currentScene,
            playerPosition = new Vector3Save(playerPosition),
            saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return saveData;
    }

    public void ApplySaveData(GameSaveData saveData)
    {
        if (saveData == null) return;

        playerName = saveData.playerName;
        playerLevel = saveData.playerLevel;
        currentExp = saveData.currentExp;
        gold = saveData.gold;
        currentScene = saveData.currentScene;
        playerPosition = saveData.playerPosition.ToVector3();

        Debug.Log($"[GameManager] 存档数据已应用 - 等级: {playerLevel}, 金币: {gold}");
    }

    private void HandleSaveDataLoaded(GameSaveData saveData)
    {
        if (saveData != null)
        {
            ApplySaveData(saveData);
            
            if (!string.IsNullOrEmpty(saveData.currentScene))
            {
                LoadScene(saveData.currentScene);
            }
        }
        else
        {
            Debug.Log("[GameManager] 未找到存档，开始新游戏");
        }
    }

    #endregion

    #region 网络状态处理

    private void HandleConnectionStatusChanged(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.Connected:
                Debug.Log("[GameManager] 网络已连接，可以开始多人游戏");
                break;
            case ConnectionStatus.Disconnected:
                Debug.LogWarning("[GameManager] 网络已断开");
                IsInRoom = false;
                break;
            case ConnectionStatus.Failed:
                Debug.LogError("[GameManager] 网络连接失败");
                break;
        }
    }

    #endregion

    #region 玩家数据

    public void UpdatePlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    public void AddExperience(int exp)
    {
        currentExp += exp;
        
        int expNeeded = GetExpForLevel(playerLevel + 1);
        while (currentExp >= expNeeded)
        {
            currentExp -= expNeeded;
            playerLevel++;
            OnLevelUp();
            expNeeded = GetExpForLevel(playerLevel + 1);
        }
    }

    public void AddGold(int amount)
    {
        gold += amount;
        gold = Mathf.Max(0, gold);
    }

    private void OnLevelUp()
    {
        Debug.Log($"[GameManager] 升级！当前等级: {playerLevel}");
        AudioManager.Instance?.PlaySFX("LevelUp");
    }

    private int GetExpForLevel(int level)
    {
        return level * 100;
    }

    #endregion
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Loading
}
