using UnityEngine;

public class BackGroundScript : MonoBehaviour
{
    public float speed;
    [SerializeField] float PositionDissapear; // Позиция где проподает объект

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(transform.position.x + speed * Time.deltaTime, transform.position.y, 0); // двигаем объект меняя его позицию каждый фрейм
        if (transform.position.x > PositionDissapear)
        {
            Destroy(gameObject); // удаляем объект
        }
    }
}
