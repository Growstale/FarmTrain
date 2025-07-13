using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    private string persistentManagersScene = "Initializer";

    void Start()
    {
        // ���������, �� ��������� �� ��� ��� �����, ����� �������� ����������
        if (!SceneManager.GetSceneByName(persistentManagersScene).isLoaded)
        {
            // ��������� ����� � ����������� �������� � ������� ����� Menu
            SceneManager.LoadScene(persistentManagersScene, LoadSceneMode.Additive);
        }
    }
}