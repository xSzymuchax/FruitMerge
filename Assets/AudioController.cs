using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    public AudioSource audioSource;

    public AudioClip pop1;
    public AudioClip pop2;
    public AudioClip sadHorn;

    private void Start()
    {
        Instance = this;
    }

    public void PlayPop()
    {
        if (Random.value <= 0.5f)
            audioSource.clip = pop1;
        else
            audioSource.clip = pop2;

        audioSource.Play();
    }

    public void PlaySadHorn()
    {
        audioSource.clip = sadHorn;
        audioSource.Play();
    }
}
