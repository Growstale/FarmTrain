using UnityEngine;

public class ClickToObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnMouseDown()
    {
        gameObject.SetActive(false);
    }
    void Update()
    {
        
    }
}
