using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    private string persistentManagersScene = "Initializer";

    void Start()
    {
        // Проверяем, не загружена ли уже эта сцена, чтобы избежать дубликатов
        if (!SceneManager.GetSceneByName(persistentManagersScene).isLoaded)
        {
            // Загружаем сцену с менеджерами ВДОБАВОК к текущей сцене Menu
            SceneManager.LoadScene(persistentManagersScene, LoadSceneMode.Additive);
        }
    }
}