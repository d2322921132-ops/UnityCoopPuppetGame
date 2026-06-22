using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// UI 管理器 - 管理游戏内所有 UI 界面
/// 包含锁链张力条、移动端输入 UI 等
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("主界面")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("连接状态 UI")]
    [SerializeField] private Text connectionStatusText;
    [SerializeField] private Image connectionStatusIcon;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;
    [SerializeField] private Color connectingColor = Color.yellow;

    [Header("移动端输入 UI")]
    [SerializeField] private DynamicJoystick movementJoystick;
    [SerializeField] private MobileActionButton actionButton;
    [SerializeField] private SkillButton[] skillButtons;

    [Header("锁链张力条")]
    [SerializeField] private ChainTensionMeter tensionMeter;

    [Header("玩家信息")]
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text playerLevelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private Text goldText;

    [Header("交互提示")]
    [SerializeField] private Text interactionPromptText;
    [SerializeField] private CanvasGroup promptCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectionStatusChanged += UpdateConnectionStatus;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        // 初始化输入管理器引用
        if (MobileInputManager.Instance != null && movementJoystick != null)
        {
            MobileInputManager.Instance.SetJoystick(movementJoystick);
            MobileInputManager.Instance.SetActionButton(actionButton);
        }

        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnConnectionStatusChanged -= UpdateConnectionStatus;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void UpdateConnectionStatus(ConnectionStatus status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status.ToString();
        }

        if (connectionStatusIcon != null)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    connectionStatusIcon.color = connectedColor;
                    break;
                case ConnectionStatus.Connecting:
                    connectionStatusIcon.color = connectingColor;
                    break;
                case ConnectionStatus.Disconnected:
                case ConnectionStatus.Failed:
                    connectionStatusIcon.color = disconnectedColor;
                    break;
            }
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.Playing:
                ShowGameplayUI();
                break;
            case GameState.Paused:
                ShowPauseMenu();
                break;
        }
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            var cg = mainMenuPanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(cg.FadeIn(0.3f));
        }

        // 主菜单隐藏移动端输入 UI
        SetMobileInputVisible(false);
    }

    public void ShowGameplayUI()
    {
        HideAllPanels();
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(true);
            var cg = gameplayPanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(cg.FadeIn(0.2f));
        }

        // 游戏内显示移动端输入 UI
        SetMobileInputVisible(true);
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            var cg = pausePanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(cg.FadeIn(0.2f));
        }

        // 暂停时隐藏移动端输入
        SetMobileInputVisible(false);
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
        {
            var cg = pausePanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(cg.FadeOut(0.2f));
        }

        // 恢复时显示移动端输入
        SetMobileInputVisible(true);
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            var cg = settingsPanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(cg.FadeIn(0.3f));
        }
    }

    private void HideAllPanels()
    {
        mainMenuPanel?.SetActive(false);
        gameplayPanel?.SetActive(false);
        pausePanel?.SetActive(false);
        settingsPanel?.SetActive(false);
    }

    /// <summary>
    /// 显示/隐藏移动端输入 UI
    /// </summary>
    private void SetMobileInputVisible(bool visible)
    {
        if (movementJoystick != null)
            movementJoystick.gameObject.SetActive(visible);

        if (actionButton != null)
            actionButton.gameObject.SetActive(visible);

        foreach (var skill in skillButtons)
        {
            if (skill != null)
                skill.gameObject.SetActive(visible);
        }

        if (tensionMeter != null)
            tensionMeter.SetVisible(visible);

        if (visible)
        {
            MobileInputManager.Instance?.EnableInput();
        }
        else
        {
            MobileInputManager.Instance?.DisableInput();
        }
    }

    /// <summary>
    /// 显示/隐藏交互提示
    /// </summary>
    public void ShowInteractionPrompt(bool show, string text = "")
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = text;
        }

        if (promptCanvasGroup != null)
        {
            if (show)
            {
                promptCanvasGroup.gameObject.SetActive(true);
                StartCoroutine(promptCanvasGroup.FadeIn(0.2f));
            }
            else
            {
                StartCoroutine(promptCanvasGroup.FadeOut(0.2f));
            }
        }
    }

    public void UpdatePlayerInfo(string name, int level, int currentExp, int maxExp, int gold)
    {
        if (playerNameText != null)
            playerNameText.text = name;

        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{level}";

        if (expSlider != null)
        {
            expSlider.maxValue = maxExp;
            expSlider.value = currentExp;
        }

        if (goldText != null)
            goldText.text = gold.ToString();
    }

    public void OnStartGameClicked()
    {
        NetworkManager.Instance?.ConnectToServer();
        GameManager.Instance?.LoadScene("GameScene");
        GameManager.Instance?.ChangeState(GameState.Playing);
    }

    public void OnPauseClicked()
    {
        GameManager.Instance?.PauseGame();
    }

    public void OnResumeClicked()
    {
        GameManager.Instance?.ResumeGame();
        HidePauseMenu();
    }

    public void OnSaveGameClicked()
    {
        var saveData = GameManager.Instance?.CollectSaveData();
        if (saveData != null)
        {
            CloudSaveManager.Instance?.SaveGame(saveData);
        }
    }

    public void OnLoadGameClicked()
    {
        CloudSaveManager.Instance?.LoadGame();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public DynamicJoystick GetJoystick() => movementJoystick;
    public MobileActionButton GetActionButton() => actionButton;
    public ChainTensionMeter GetTensionMeter() => tensionMeter;
}