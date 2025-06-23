using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string firstSceneToLoad = "SampleScene";

    void Start()
    {
        SceneManager.LoadScene(firstSceneToLoad);
    }
}