using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// 场景过渡管理器 - 管理场景切换时的过渡动画
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("过渡配置")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float defaultFadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("加载画面")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private UnityEngine.UI.Text loadingText;

    private bool isTransitioning;

    public bool IsTransitioning => isTransitioning;

    public event Action OnTransitionStart;
    public event Action OnTransitionComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void TransitionToScene(string sceneName, float fadeDuration = -1f)
    {
        if (isTransitioning) return;

        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;
        StartCoroutine(TransitionCoroutine(sceneName, duration));
    }

    private IEnumerator TransitionCoroutine(string sceneName, float duration)
    {
        isTransitioning = true;
        OnTransitionStart?.Invoke();

        yield return StartCoroutine(FadeOut(duration * 0.5f));

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            UpdateLoadingProgress(asyncLoad.progress / 0.9f);
            yield return null;
        }

        UpdateLoadingProgress(1f);
        yield return new WaitForSeconds(0.5f);

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        yield return StartCoroutine(FadeIn(duration * 0.5f));

        isTransitioning = false;
        OnTransitionComplete?.Invoke();
    }

    private IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null) yield break;

        fadePanel.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadePanel.alpha = fadeCurve.Evaluate(t);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }

    private IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadePanel.alpha = 1f - fadeCurve.Evaluate(t);
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.blocksRaycasts = false;
    }

    private void UpdateLoadingProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    public IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeCurve.Evaluate(t));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.1f;
        canvasGroup.interactable = targetAlpha > 0.1f;
    }
}
