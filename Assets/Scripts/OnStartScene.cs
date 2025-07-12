using UnityEngine;

public class OnStartScene : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
        SaveLoadManager.Instance.LoadGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
