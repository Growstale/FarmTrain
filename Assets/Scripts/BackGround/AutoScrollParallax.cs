using UnityEngine;

public class AutoScrollParallax : MonoBehaviour
{
    [Tooltip("—корость, с которой будет двигатьс€ этот слой. ќтрицательные значени€ - движение влево.")] public float scrollSpeed = -2f; // ”величена базова€ скорость дл€ более быстрого движени€

    private float spriteWidth;
    private Vector3 startPosition;

    void Start()
    {
        // —охран€ем начальную позицию объекта
        startPosition = transform.position;

        // ¬ычисл€ем ширину спрайта в мировых координатах
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        // ƒвигаем объект влево с посто€нной скоростью
        // Time.deltaTime делает движение плавным и не завис€щим от FPS
        transform.Translate(Vector3.right * scrollSpeed * Time.deltaTime); // ”двоенный множитель скорости

        // ѕровер€ем, нужно ли "телепортировать" фон дл€ создани€ бесконечности
        // Mathf.Repeat зацикливает позицию в диапазоне от 0 до ширины спрайта
        float newPosition = Mathf.Repeat(Time.time * scrollSpeed * 2f, spriteWidth) - 2; // ”двоенна€ скорость в повторении
        transform.position = startPosition + Vector3.right * newPosition;
    }

}