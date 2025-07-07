// AutoScrollParallax.cs

using UnityEngine;
using System.Collections.Generic; // <<< Добавляем для List

public class AutoScrollParallax : MonoBehaviour
{
    [Tooltip("Скорость, с которой будет двигаться этот слой.")]
    public float scrollSpeed = -2f;

    // --- НОВЫЕ ПОЛЯ ---
    [Tooltip("Список спрайтов для каждого уровня игры. Element 0 - для уровня 1, Element 1 - для уровня 2 и т.д.")]
    [SerializeField] private List<Sprite> levelSprites;

    private SpriteRenderer spriteRenderer;
    // --- КОНЕЦ НОВЫХ ПОЛЕЙ ---

    private float spriteWidth;
    private Vector3 startPosition;

    void Awake() // <<< Меняем Start на Awake
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("На слое параллакса отсутствует SpriteRenderer!", gameObject);
            enabled = false;
            return;
        }

        // --- НОВАЯ ЛОГИКА ИНИЦИАЛИЗАЦИИ ---
        // Устанавливаем спрайт для текущего уровня при старте игры
        if (ExperienceManager.Instance != null)
        {
            SetSpriteForLevel(ExperienceManager.Instance.CurrentLevel);
        }
        else
        {
            // Если ExperienceManager еще не готов, используем спрайт для первого уровня
            SetSpriteForLevel(1);
        }

        // Сохраняем начальную позицию объекта
        startPosition = transform.position;
        // Вычисляем ширину спрайта в мировых координатах
        RecalculateBounds();
    }

    void Update()
    {
        if (spriteRenderer.sprite == null) return; // Не двигаемся, если нет спрайта

        // Двигаем объект влево
        transform.Translate(Vector3.right * scrollSpeed * Time.deltaTime);

        // Проверяем, нужно ли "телепортировать" фон
        // Mathf.Repeat теперь использует ширину текущего спрайта
        float newPositionX = startPosition.x + Mathf.Repeat(Time.time * scrollSpeed, spriteWidth);
        transform.position = new Vector3(newPositionX, startPosition.y, startPosition.z);
    }

    // --- НОВЫЕ МЕТОДЫ ---

    // Этот метод будет вызываться извне для смены уровня
    public void SetSpriteForLevel(int level)
    {
        int spriteIndex = level - 1; // Уровень 1 -> индекс 0, Уровень 2 -> индекс 1

        if (levelSprites == null || levelSprites.Count == 0)
        {
            Debug.LogWarning($"У слоя параллакса {gameObject.name} не назначен список спрайтов.", gameObject);
            return;
        }

        if (spriteIndex >= 0 && spriteIndex < levelSprites.Count)
        {
            if (spriteRenderer.sprite != levelSprites[spriteIndex])
            {
                spriteRenderer.sprite = levelSprites[spriteIndex];
                RecalculateBounds(); // Важно пересчитать ширину после смены спрайта!
                Debug.Log($"Слой {gameObject.name} сменил спрайт на {spriteRenderer.sprite.name} для уровня {level}.");
            }
        }
        else
        {
            Debug.LogError($"Для слоя {gameObject.name} не найден спрайт для уровня {level} (индекс {spriteIndex}).", gameObject);
        }
    }

    // Вспомогательный метод для пересчета ширины
    private void RecalculateBounds()
    {
        if (spriteRenderer.sprite != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
        }
    }
}