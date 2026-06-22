using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 音频管理器 - 管理动态背景音乐和环境音效
/// 根据场景变化自动切换 BGM，支持混音器控制
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频混音器")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource bgmSource2;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM 配置")]
    [SerializeField] private List<BGMConfig> bgmConfigs = new List<BGMConfig>();
    [SerializeField] private float crossFadeDuration = 2f;
    [SerializeField] private float bgmVolume = 0.7f;

    [Header("环境音效")]
    [SerializeField] private List<AmbientConfig> ambientConfigs = new List<AmbientConfig>();

    private AudioSource currentBGMSource;
    private AudioSource nextBGMSource;
    private Coroutine crossFadeCoroutine;
    private string currentSceneName;
    private bool isCrossFading;

    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        if (bgmSource2 == null)
        {
            bgmSource2 = gameObject.AddComponent<AudioSource>();
            bgmSource2.playOnAwake = false;
            bgmSource2.loop = true;
        }

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        if (audioMixer != null)
        {
            var bgmGroup = audioMixer.FindMatchingGroups("BGM");
            if (bgmGroup.Length > 0)
            {
                bgmSource.outputAudioMixerGroup = bgmGroup[0];
                bgmSource2.outputAudioMixerGroup = bgmGroup[0];
            }
            var ambientGroup = audioMixer.FindMatchingGroups("Ambient");
            if (ambientGroup.Length > 0)
                ambientSource.outputAudioMixerGroup = ambientGroup[0];
            var sfxGroup = audioMixer.FindMatchingGroups("SFX");
            if (sfxGroup.Length > 0)
                sfxSource.outputAudioMixerGroup = sfxGroup[0];
        }

        currentBGMSource = bgmSource;
        nextBGMSource = bgmSource2;
    }

    public void SwitchBGMForScene(string sceneName)
    {
        if (currentSceneName == sceneName) return;
        currentSceneName = sceneName;

        BGMConfig config = bgmConfigs.Find(bgm => bgm.sceneName == sceneName);
        if (config != null && config.clip != null)
        {
            CrossFadeBGM(config.clip, config.volume);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到场景 '{sceneName}' 对应的 BGM 配置");
        }

        SwitchAmbientForScene(sceneName);
    }

    private void CrossFadeBGM(AudioClip newClip, float targetVolume)
    {
        if (isCrossFading && crossFadeCoroutine != null)
        {
            StopCoroutine(crossFadeCoroutine);
        }

        crossFadeCoroutine = StartCoroutine(CrossFadeCoroutine(newClip, targetVolume));
    }

    private IEnumerator CrossFadeCoroutine(AudioClip newClip, float targetVolume)
    {
        isCrossFading = true;

        nextBGMSource.clip = newClip;
        nextBGMSource.volume = 0f;
        nextBGMSource.Play();

        float elapsed = 0f;
        float startVolume = currentBGMSource.volume;

        while (elapsed < crossFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossFadeDuration;

            currentBGMSource.volume = Mathf.Lerp(startVolume, 0f, t);
            nextBGMSource.volume = Mathf.Lerp(0f, targetVolume * bgmVolume, t);

            yield return null;
        }

        currentBGMSource.volume = 0f;
        currentBGMSource.Stop();

        nextBGMSource.volume = targetVolume * bgmVolume;

        AudioSource temp = currentBGMSource;
        currentBGMSource = nextBGMSource;
        nextBGMSource = temp;

        isCrossFading = false;
    }

    private void SwitchAmbientForScene(string sceneName)
    {
        AmbientConfig config = ambientConfigs.Find(ambient => ambient.sceneName == sceneName);
        if (config != null && config.clip != null)
        {
            if (ambientSource.clip != config.clip)
            {
                ambientSource.clip = config.clip;
                ambientSource.volume = config.volume;
                ambientSource.Play();
            }
        }
        else
        {
            ambientSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxName}");
        if (clip != null)
        {
            PlaySFX(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] 未找到音效: {sfxName}");
        }
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    private void UpdateBGMVolume()
    {
        if (!isCrossFading && currentBGMSource != null)
        {
            currentBGMSource.volume = bgmVolume;
        }
    }

    public void SetMixerParameter(string parameterName, float value)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(parameterName, value);
        }
    }

    public void PauseAll()
    {
        currentBGMSource?.Pause();
        ambientSource?.Pause();
        sfxSource?.Pause();
    }

    public void ResumeAll()
    {
        currentBGMSource?.UnPause();
        ambientSource?.UnPause();
        sfxSource?.UnPause();
    }

    public void StopAll()
    {
        currentBGMSource?.Stop();
        ambientSource?.Stop();
        sfxSource?.Stop();
    }
}

[Serializable]
public class BGMConfig
{
    public string sceneName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.7f;
}

[Serializable]
public class AmbientConfig
{
    public string sceneName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.5f;
}
