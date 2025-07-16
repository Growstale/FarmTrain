using UnityEngine;
using UnityEngine.Audio;

public class close : MonoBehaviour
{
    public GameObject panelToHide;
    public AudioClip closeSound;         
    public AudioSource audioSource;

    public void HidePanel()
    {
        if (audioSource != null && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
        panelToHide.SetActive(false);
    }
}
