using UnityEngine;

public class InfiniteScroller : MonoBehaviour
{
    [Tooltip("Скорость движения слоя. Отрицательные значения - влево, положительные - вправо.")]
    public float scrollSpeed = -5f;

    private float spriteWidth;
    private Vector2 startPosition;

    void Start()
    {
        // Получаем ширину спрайта
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
        startPosition = transform.position;
    }

    void Update()
    {
        // Двигаем объект
        transform.Translate(Vector3.right * scrollSpeed * Time.deltaTime);

        // Проверяем, ушел ли объект полностью за экран влево
        // Мы проверяем, когда он отодвинулся от своего НАЧАЛЬНОГО положения на свою полную ширину
        if (transform.position.x < startPosition.x - spriteWidth)
        {
            // Вычисляем, на сколько нужно "перепрыгнуть" вперед
            // Мы двигаем его вперед на ДВЕ ширины спрайта от текущей позиции, чтобы он оказался справа от своего "напарника"
            transform.position += new Vector3(spriteWidth * 2f, 0, 0);
        }
    }
}