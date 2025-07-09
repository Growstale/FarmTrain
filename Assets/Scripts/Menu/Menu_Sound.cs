using UnityEngine;
using UnityEngine.UI;
public class Menu_Sound : MonoBehaviour
{
    public AudioClip clickSound;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = Camera.main.GetComponent<AudioSource>(); 
    }

    public void PlaySound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
