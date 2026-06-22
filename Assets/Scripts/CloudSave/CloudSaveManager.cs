using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// 云存档管理器 - 负责 Firebase 实时数据库的存档读写
/// 实现跨设备进度同步、自动备份和冲突解决
/// </summary>
public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }

    [Header("存档配置")]
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveInterval = 30f;
    [SerializeField] private string saveFileName = "game_save.json";

    [Header("本地存档路径")]
    [SerializeField] private string localSavePath;

    private float autoSaveTimer;
    private bool isInitialized;
    private GameSaveData currentSaveData;

    public bool IsInitialized => isInitialized;
    public GameSaveData CurrentSave => currentSaveData;

    public event Action<bool> OnSaveComplete;
    public event Action<GameSaveData> OnLoadComplete;
    public event Action OnAutoSaveTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        localSavePath = Application.persistentDataPath + "/" + saveFileName;
        isInitialized = true;
    }

    private void Update()
    {
        if (!enableAutoSave || !isInitialized) return;

        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            TriggerAutoSave();
        }
    }

    /// <summary>
    /// 保存游戏进度
    /// </summary>
    public void SaveGame(GameSaveData saveData)
    {
        if (saveData == null)
        {
            OnSaveComplete?.Invoke(false);
            return;
        }

        try
        {
            saveData.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(saveData);
            
            // 保存到本地
            System.IO.File.WriteAllText(localSavePath, json);
            
            // 更新当前存档
            currentSaveData = saveData;

            // 模拟云端保存（实际使用时替换为 Firebase SDK 调用）
            Debug.Log($"[CloudSaveManager] 存档已保存: {localSavePath}");
            OnSaveComplete?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSaveManager] 存档保存失败: {ex.Message}");
            OnSaveComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// 从本地加载游戏进度
    /// </summary>
    public void LoadGame()
    {
        try
        {
            if (System.IO.File.Exists(localSavePath))
            {
                string json = System.IO.File.ReadAllText(localSavePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                if (saveData != null)
                {
                    currentSaveData = saveData;
                    Debug.Log("[CloudSaveManager] 存档加载成功");
                    OnLoadComplete?.Invoke(saveData);
                    return;
                }
            }

            Debug.Log("[CloudSaveManager] 未找到存档数据，创建新存档");
            currentSaveData = new GameSaveData();
            OnLoadComplete?.Invoke(currentSaveData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSaveManager] 存档加载失败: {ex.Message}");
            OnLoadComplete?.Invoke(null);
        }
    }

    /// <summary>
    /// 检查是否有存档
    /// </summary>
    public bool HasSaveData()
    {
        return System.IO.File.Exists(localSavePath);
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public void DeleteSave()
    {
        try
        {
            if (System.IO.File.Exists(localSavePath))
            {
                System.IO.File.Delete(localSavePath);
                currentSaveData = null;
                Debug.Log("[CloudSaveManager] 存档已删除");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSaveManager] 删除存档失败: {ex.Message}");
        }
    }

    private void TriggerAutoSave()
    {
        OnAutoSaveTriggered?.Invoke();
        
        var currentSave = GameManager.Instance?.CollectSaveData();
        if (currentSave != null)
        {
            SaveGame(currentSave);
        }
    }

    /// <summary>
    /// 导出存档为 JSON 字符串
    /// </summary>
    public string ExportSaveToJson()
    {
        if (currentSaveData != null)
        {
            return JsonUtility.ToJson(currentSaveData, true);
        }
        return null;
    }

    /// <summary>
    /// 从 JSON 导入存档
    /// </summary>
    public void ImportSaveFromJson(string json)
    {
        try
        {
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData != null)
            {
                SaveGame(saveData);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudSaveManager] 导入存档失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 游戏存档数据结构
/// </summary>
[Serializable]
public class GameSaveData
{
    public string playerName = "Player";
    public int playerLevel = 1;
    public int currentExp = 0;
    public int gold = 0;
    public long saveTimestamp;
    public string currentScene = "MainMenu";
    public Vector3Save playerPosition = new Vector3Save(Vector3.zero);
    public List<string> unlockedLevels = new List<string>();
    public List<InventoryItemSave> inventory = new List<InventoryItemSave>();
    public Dictionary<string, string> gameSettings = new Dictionary<string, string>();

    public GameSaveData()
    {
        unlockedLevels = new List<string>();
        inventory = new List<InventoryItemSave>();
        gameSettings = new Dictionary<string, string>();
        saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}

[Serializable]
public struct Vector3Save
{
    public float x, y, z;

    public Vector3Save(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class InventoryItemSave
{
    public string itemId;
    public int quantity;
    public int durability;
}
