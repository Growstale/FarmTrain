using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string secondSceneToLoad = "Initializer";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadGame()
    {
        Debug.Log("Loading scene: SampleScene");
        SceneManager.LoadScene(secondSceneToLoad);
    }
    
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void OnSettingsCloseButtonClick()
    {
        updated_sound sound = FindObjectOfType<updated_sound>();
        if (sound != null)
        {
            sound.PlaySound();
        }

        Menu menu = FindObjectOfType<Menu>();
        if (menu != null)
        {
            menu.HideSettings();
        }
    }

    public void HideSettings()
    {
        Debug.Log("Loading scene: HideSettings");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
             Application.Quit();
        #endif
    }
}