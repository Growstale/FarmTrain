using UnityEngine;

public class EscPanelToggle : MonoBehaviour
{
    public GameObject panelToOpen; // Панель, которую нужно показать
    public AudioClip openSound;
    public AudioSource audioSource;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (audioSource != null && openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }
            // Переключение активности панели
            panelToOpen.SetActive(!panelToOpen.activeSelf);
        }
    }
}
