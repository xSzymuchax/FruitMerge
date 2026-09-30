using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    public AudioSource audioSource;

    public AudioClip pop1;
    public AudioClip pop2;
    public AudioClip sadHorn;

    void Awake()
    {
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        AudioListener extraListener = GetComponent<AudioListener>();
        if (extraListener != null && Camera.main != null && Camera.main.GetComponent<AudioListener>() != null)
            extraListener.enabled = false;
    }

    public void PlayPop(float pitch = 1f)
    {
        if (audioSource == null)
            return;

        AudioClip clip = Random.value <= 0.5f ? pop1 : pop2;
        if (clip == null)
            clip = pop1 != null ? pop1 : pop2;
        if (clip == null)
            return;

        audioSource.pitch = Mathf.Clamp(pitch, 0.75f, 1.35f);
        audioSource.PlayOneShot(clip);
    }

    public void PlaySadHorn()
    {
        if (audioSource == null || sadHorn == null)
            return;

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(sadHorn);
    }
}
