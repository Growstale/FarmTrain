using UnityEngine;

public class TestBackGround : MonoBehaviour
{
    public GameObject BackGrounds;


    public float startX;
    public float startY;

    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector3 position = new Vector3(-38.57891f, startY, 0);

        // Просто создаем объект, не перезаписывая переменную BackGrounds
        Instantiate(BackGrounds, position, Quaternion.identity);
    }

}
