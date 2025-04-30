using UnityEngine;

// Этот скрипт вешается на КОРНЕВОЙ объект префаба облачка
public class BubbleYSorter : MonoBehaviour
{
    // Ссылка на SpriteRenderer фона облачка
    [SerializeField] private SpriteRenderer backgroundRenderer;
    // Ссылка на SpriteRenderer иконки внутри облачка
    [SerializeField] private SpriteRenderer iconRenderer;

    // Transform животного-владельца. Будет установлен из AnimalController.
    private Transform ownerTransform;

    // Множитель, как в YSorter для животных
    private const int sortingOrderMultiplier = -100;

    // Базовый порядок для фона внутри облачка (чтобы он был "глубже")
    private const int backgroundOrderOffset = 0;
    // Смещение для иконки, чтобы она была поверх фона
    private const int iconOrderOffset = 1;

    void Awake()
    {
        // Проверки, что рендереры назначены в инспекторе
        if (backgroundRenderer == null)
        {
            Debug.LogError("Background Renderer не назначен в BubbleYSorter!", gameObject);
        }
        if (iconRenderer == null)
        {
            Debug.LogError("Icon Renderer не назначен в BubbleYSorter!", gameObject);
        }
    }

    // Метод для установки владельца извне (из AnimalController)
    public void SetOwner(Transform owner)
    {
        ownerTransform = owner;
        // Можно сразу обновить порядок при установке владельца
        UpdateSortOrder();
    }

    // Используем LateUpdate, чтобы позиция владельца была актуальной
    void LateUpdate()
    {
        UpdateSortOrder();
    }

    void UpdateSortOrder()
    {
        if (ownerTransform == null || backgroundRenderer == null || iconRenderer == null)
        {
            // Если владелец еще не установлен или рендереры пропали, ничего не делаем
            // Можно скрыть рендереры или установить им дефолтный низкий порядок
            // backgroundRenderer.sortingOrder = -10000;
            // iconRenderer.sortingOrder = -10000 + 1;
            return;
        }

        // Получаем Y-координату владельца (используем его ноги/основание, если есть YSorter)
        // Простой вариант: просто Y позиция владельца
        float ownerY = ownerTransform.position.y;

        // Рассчитываем БАЗОВЫЙ sortingOrder для всего облачка
        int baseSortingOrder = Mathf.RoundToInt(ownerY * sortingOrderMultiplier);

        // Устанавливаем sortingOrder для фона и иконки ОТНОСИТЕЛЬНО базового
        backgroundRenderer.sortingOrder = baseSortingOrder + backgroundOrderOffset; // Фон
        iconRenderer.sortingOrder = baseSortingOrder + iconOrderOffset;       // Иконка (будет +1 относительно фона)

        // Опционально: Логирование
        // Debug.Log($"Bubble for {ownerTransform.name} - OwnerY: {ownerY}, BaseOrder: {baseSortingOrder}, BgOrder: {backgroundRenderer.sortingOrder}, IconOrder: {iconRenderer.sortingOrder}");
    }
}