using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Audio Sources")]
    public AudioSource audioSource;

    [Header("SFX Clips")]
    public AudioClip placeBed;
    public AudioClip rake;
    public AudioClip plant;
    public AudioClip fertilize;
    public AudioClip slotClickSound;
    public AudioClip pickupSound;
    public AudioClip wateringSound;
    public AudioClip shovelSound;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
