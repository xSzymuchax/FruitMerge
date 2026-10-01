using TMPro;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    public AudioSource audioSource;
    public AudioSource musicSource;
    public AudioClip backgroundMusic;
    public TextMeshProUGUI musicLabel;
    public TextMeshProUGUI effectsLabel;

    public AudioClip pop1;
    public AudioClip pop2;
    public AudioClip sadHorn;
    public AudioClip newRecord;

    const string MusicKey = "FruitMerge_Music";
    const string EffectsKey = "FruitMerge_Effects";

    public bool MusicOn { get; private set; }
    public bool EffectsOn { get; private set; }

    void Awake()
    {
        Instance = this;
        MusicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        EffectsOn = PlayerPrefs.GetInt(EffectsKey, 1) == 1;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        AudioListener extraListener = GetComponent<AudioListener>();
        if (extraListener != null && Camera.main != null && Camera.main.GetComponent<AudioListener>() != null)
            extraListener.enabled = false;
    }

    void Start()
    {
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            if (backgroundMusic != null)
                musicSource.clip = backgroundMusic;
            ApplyMusic();
        }

        RefreshLabels();
    }

    public void ToggleMusic()
    {
        MusicOn = !MusicOn;
        PlayerPrefs.SetInt(MusicKey, MusicOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusic();
        RefreshLabels();
    }

    public void ToggleEffects()
    {
        EffectsOn = !EffectsOn;
        PlayerPrefs.SetInt(EffectsKey, EffectsOn ? 1 : 0);
        PlayerPrefs.Save();
        RefreshLabels();
    }

    public void RefreshLabels()
    {
        if (musicLabel != null)
            musicLabel.text = MusicOn ? "Muzyka: włączona" : "Muzyka: wyłączona";
        if (effectsLabel != null)
            effectsLabel.text = EffectsOn ? "Efekty: włączone" : "Efekty: wyłączone";
    }

    public void PlayPop(float pitch = 1f)
    {
        AudioClip clip = Random.value <= 0.5f ? pop1 : pop2;
        if (clip == null)
            clip = pop1 != null ? pop1 : pop2;

        PlayEffect(clip, Mathf.Clamp(pitch, 0.75f, 1.35f));
    }

    public void PlaySadHorn()
    {
        PlayEffect(sadHorn, 1f);
    }

    public void PlayNewRecord()
    {
        PlayEffect(newRecord, 1f);
    }

    void PlayEffect(AudioClip clip, float pitch)
    {
        if (!EffectsOn || audioSource == null || clip == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
    }

    void ApplyMusic()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        if (MusicOn)
        {
            if (!musicSource.isPlaying)
                musicSource.UnPause();
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            musicSource.Pause();
        }
    }
}
