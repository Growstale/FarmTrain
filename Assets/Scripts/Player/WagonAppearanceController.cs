using UnityEngine;

// Убедимся, что на объекте есть SpriteRenderer, чтобы избежать ошибок.
[RequireComponent(typeof(SpriteRenderer))]
public class WagonAppearanceController : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("Спрайт вагона до улучшения склада.")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("Спрайт вагона ПОСЛЕ улучшения склада.")]
    [SerializeField] private Sprite upgradedSprite;

    [Header("Upgrade Data")]
    [Tooltip("Ссылка на ItemData, который представляет собой улучшение для склада.")]
    [SerializeField] private ItemData storageUpgradeItem;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // Получаем ссылку на компонент, который будем менять.
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Проверяем, что все ссылки на месте, чтобы избежать ошибок.
        if (storageUpgradeItem == null || defaultSprite == null || upgradedSprite == null)
        {
            Debug.LogError($"На WagonAppearanceController ({gameObject.name}) не назначены все необходимые поля (спрайты или данные улучшения)!", this);
            return;
        }

        // Подписываемся на событие покупки в магазине.
        // Это нужно, если игрок купит улучшение, находясь на сцене с поездом (если это возможно).
        // Если улучшение покупается только на станции, эта подписка сработает при возвращении на сцену.
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnItemPurchased += OnItemPurchased;
        }

        // Сразу проверяем состояние и обновляем вид вагона.
        UpdateWagonAppearance();
    }

    private void OnDestroy()
    {
        // Очень важно отписаться от события, чтобы избежать утечек памяти и ошибок.
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnItemPurchased -= OnItemPurchased;
        }
    }

    /// <summary>
    /// Этот метод вызывается, когда игрок что-то покупает в магазине.
    /// </summary>
    private void OnItemPurchased(ItemData purchasedItem, int quantity)
    {
        // Нас интересует только тот случай, когда купленный предмет - это НАШЕ улучшение.
        if (purchasedItem == storageUpgradeItem)
        {
            Debug.Log($"<color=cyan>[WagonAppearance]</color> Получено событие о покупке улучшения склада! Обновляю внешний вид.");
            UpdateWagonAppearance();
        }
    }

    /// <summary>
    /// Главный метод, который проверяет, есть ли улучшение, и меняет спрайт.
    /// </summary>
    private void UpdateWagonAppearance()
    {
        // Проверяем, существует ли менеджер улучшений.
        if (TrainUpgradeManager.Instance == null)
        {
            Debug.LogError("TrainUpgradeManager не найден! Невозможно проверить улучшения.", this);
            // На всякий случай ставим спрайт по умолчанию.
            spriteRenderer.sprite = defaultSprite;
            return;
        }

        // Спрашиваем у менеджера, куплено ли улучшение.
        bool isUpgraded = TrainUpgradeManager.Instance.HasUpgrade(storageUpgradeItem);

        // В зависимости от ответа, устанавливаем нужный спрайт.
        if (isUpgraded)
        {
            spriteRenderer.sprite = upgradedSprite;
            Debug.Log($"[WagonAppearance] Установлен УЛУЧШЕННЫЙ спрайт для вагона {gameObject.name}.");
        }
        else
        {
            spriteRenderer.sprite = defaultSprite;
            Debug.Log($"[WagonAppearance] Установлен ОБЫЧНЫЙ спрайт для вагона {gameObject.name}.");
        }
    }
}