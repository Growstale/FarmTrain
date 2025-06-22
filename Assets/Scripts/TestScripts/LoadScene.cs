using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public string sceneToLoad;

    public void Scene()
    {

        Debug.Log("Загружаем сцену: " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
    }
}