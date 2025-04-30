using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))] // Убедимся, что SpriteRenderer есть
public class YSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    // Если используешь Вариант Б (дочерний объект для опорной точки):
    // Раскомментируй и перетащи объект "FeetPosition" сюда в инспекторе префаба
    // [SerializeField] private Transform sortPointTransform;

    // Множитель для преобразования Y в sortingOrder.
    // Отрицательный, т.к. чем НИЖЕ Y, тем БОЛЬШЕ должен быть sortingOrder.
    // Значение 100 обычно достаточно, чтобы избежать конфликтов с целыми числами Y.
    private const int sortingOrderMultiplier = -100;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate() // Используем LateUpdate, чтобы позиция объекта уже обновилась
    {
        if (spriteRenderer == null) return;

        float sortY;

        // Определяем Y-координату для сортировки
        // ---- Вариант А (Используем Pivot спрайта / позицию объекта) ----
        sortY = transform.position.y;

        // ---- Вариант Б (Используем дочерний объект sortPointTransform) ----
        /*
        if (sortPointTransform != null)
        {
            sortY = sortPointTransform.position.y;
        }
        else
        {
            // Запасной вариант, если sortPointTransform не назначен
            sortY = transform.position.y;
            // Можно добавить предупреждение в Awake, если он не назначен
            // Debug.LogWarning($"SortPointTransform не назначен для {gameObject.name}, используется позиция объекта.", gameObject);
        }
        */ // Закомментируй/раскомментируй нужный вариант

        // Рассчитываем и устанавливаем sortingOrder
        // Умножаем на -100 и приводим к int.
        // Чем меньше Y, тем больше будет итоговый sortingOrder.
        spriteRenderer.sortingOrder = Mathf.RoundToInt(sortY * sortingOrderMultiplier);

        // Опционально: Логирование для отладки
        // Debug.Log($"{gameObject.name} - Y: {sortY}, SortingOrder: {spriteRenderer.sortingOrder}");
    }
}