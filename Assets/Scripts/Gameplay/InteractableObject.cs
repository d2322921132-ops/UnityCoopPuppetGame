using UnityEngine;
using System;

/// <summary>
/// 可交互对象 - 玩家可以与之交互的物体
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("交互配置")]
    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private string interactionPrompt = "点击交互";
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private bool requireBothPlayers = false;
    [SerializeField] private float interactionDuration = 0f;

    [Header("视觉效果")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private ParticleSystem interactionParticles;
    [SerializeField] private AudioClip interactionSound;

    [Header("事件")]
    [SerializeField] private bool destroyOnInteract = false;
    [SerializeField] private GameObject spawnOnInteract;

    public string ObjectId => objectId;
    public string DisplayName => displayName;
    public string InteractionPrompt => interactionPrompt;
    public float InteractionRange => interactionRange;
    public bool RequireBothPlayers => requireBothPlayers;

    public event Action<string> OnInteracted;
    public event Action OnInteractionStarted;
    public event Action OnInteractionCompleted;

    private bool isInRange;
    private bool isInteracting;
    private float interactionTimer;
    private int playersInRange = 0;

    private void Update()
    {
        if (isInteracting && interactionDuration > 0)
        {
            interactionTimer += Time.deltaTime;
            if (interactionTimer >= interactionDuration)
            {
                CompleteInteraction();
            }
        }
    }

    /// <summary>
    /// 玩家进入交互范围
    /// </summary>
    public void OnPlayerEnterRange(string playerId)
    {
        playersInRange++;
        isInRange = true;

        if (highlightEffect != null)
            highlightEffect.SetActive(true);

        // 显示交互提示 UI
        ShowInteractionPrompt(true);
    }

    /// <summary>
    /// 玩家离开交互范围
    /// </summary>
    public void OnPlayerExitRange(string playerId)
    {
        playersInRange = Mathf.Max(0, playersInRange - 1);
        
        if (playersInRange <= 0)
        {
            isInRange = false;
            
            if (highlightEffect != null)
                highlightEffect.SetActive(false);

            ShowInteractionPrompt(false);
            CancelInteraction();
        }
    }

    /// <summary>
    /// 开始交互
    /// </summary>
    public void StartInteraction(string playerId)
    {
        if (!isInRange || isInteracting) return;

        // 如果需要两个玩家，检查是否都有玩家在范围内
        if (requireBothPlayers && playersInRange < 2)
        {
            Debug.Log("[InteractableObject] 需要两名玩家同时交互");
            return;
        }

        isInteracting = true;
        interactionTimer = 0f;

        OnInteractionStarted?.Invoke();

        // 如果是即时交互，直接完成
        if (interactionDuration <= 0)
        {
            CompleteInteraction();
        }
    }

    /// <summary>
    /// 完成交互
    /// </summary>
    private void CompleteInteraction()
    {
        isInteracting = false;

        // 播放效果
        if (interactionParticles != null)
            interactionParticles.Play();

        if (interactionSound != null)
            AudioManager.Instance?.PlaySFX(interactionSound);

        // 触发事件
        OnInteracted?.Invoke(objectId);
        OnInteractionCompleted?.Invoke();

        // 生成物体
        if (spawnOnInteract != null)
        {
            Instantiate(spawnOnInteract, transform.position, Quaternion.identity);
        }

        // 销毁自身
        if (destroyOnInteract)
        {
            Destroy(gameObject);
        }

        Debug.Log($"[InteractableObject] 交互完成: {displayName}");
    }

    /// <summary>
    /// 取消交互
    /// </summary>
    public void CancelInteraction()
    {
        if (!isInteracting) return;

        isInteracting = false;
        interactionTimer = 0f;
    }

    /// <summary>
    /// 显示/隐藏交互提示
    /// </summary>
    private void ShowInteractionPrompt(bool show)
    {
        // 实际实现: 通过 UI 系统显示交互提示
        // UIManager.Instance?.ShowInteractionPrompt(show, interactionPrompt);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
