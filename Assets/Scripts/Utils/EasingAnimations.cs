using UnityEngine;
using System.Collections;

/// <summary>
/// 缓动动画工具类
/// </summary>
public static class EasingAnimations
{
    public enum EaseType
    {
        Linear,
        EaseInQuad, EaseOutQuad, EaseInOutQuad,
        EaseInCubic, EaseOutCubic, EaseInOutCubic,
        EaseInSine, EaseOutSine, EaseInOutSine,
        EaseInBack, EaseOutBack, EaseInOutBack,
        EaseInElastic, EaseOutElastic,
        EaseInBounce, EaseOutBounce
    }

    public static float ApplyEase(float t, EaseType easeType)
    {
        switch (easeType)
        {
            case EaseType.Linear: return t;
            case EaseType.EaseInQuad: return t * t;
            case EaseType.EaseOutQuad: return 1 - (1 - t) * (1 - t);
            case EaseType.EaseInOutQuad: return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
            case EaseType.EaseInCubic: return t * t * t;
            case EaseType.EaseOutCubic: return 1 - Mathf.Pow(1 - t, 3);
            case EaseType.EaseInOutCubic: return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            case EaseType.EaseInSine: return 1 - Mathf.Cos(t * Mathf.PI / 2);
            case EaseType.EaseOutSine: return Mathf.Sin(t * Mathf.PI / 2);
            case EaseType.EaseInOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
            case EaseType.EaseInBack: return 2.70158f * t * t * t - 1.70158f * t * t;
            case EaseType.EaseOutBack: return 1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
            case EaseType.EaseInElastic:
                return t == 0 ? 0 : t == 1 ? 1 : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * ((2 * Mathf.PI) / 3));
            case EaseType.EaseOutElastic:
                return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * ((2 * Mathf.PI) / 3)) + 1;
            case EaseType.EaseInBounce: return 1 - EaseOutBounce(1 - t);
            case EaseType.EaseOutBounce: return EaseOutBounce(t);
            default: return t;
        }
    }

    private static float EaseOutBounce(float t)
    {
        if (t < 1 / 2.75f) return 7.5625f * t * t;
        else if (t < 2 / 2.75f) return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        else if (t < 2.5f / 2.75f) return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        else return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
    }
}

public static class AnimationExtensions
{
    public static IEnumerator ScaleTo(this Transform transform, Vector3 targetScale, float duration, EasingAnimations.EaseType easeType = EasingAnimations.EaseType.EaseOutBack)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EasingAnimations.ApplyEase(t, easeType);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    public static IEnumerator MoveTo(this Transform transform, Vector3 targetPosition, float duration, EasingAnimations.EaseType easeType = EasingAnimations.EaseType.EaseOutQuad)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EasingAnimations.ApplyEase(t, easeType);
            transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }
        transform.position = targetPosition;
    }

    public static IEnumerator FadeIn(this CanvasGroup canvasGroup, float duration, EasingAnimations.EaseType easeType = EasingAnimations.EaseType.EaseOutQuad)
    {
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = EasingAnimations.ApplyEase(Mathf.Clamp01(elapsed / duration), easeType);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public static IEnumerator FadeOut(this CanvasGroup canvasGroup, float duration, EasingAnimations.EaseType easeType = EasingAnimations.EaseType.EaseInQuad)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, EasingAnimations.ApplyEase(Mathf.Clamp01(elapsed / duration), easeType));
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    public static IEnumerator Bounce(this Transform transform, float intensity = 0.3f, int bounces = 3, float duration = 0.5f)
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * bounces)) * (1 - t) * intensity;
            transform.localScale = originalScale * (1 + bounce);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
