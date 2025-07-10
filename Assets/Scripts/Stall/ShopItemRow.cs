using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class ShopItemRow : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI availableText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    private Action<ShopItem> onActionButtonClicked;
    private ShopItem currentShopItem;


    // ShopItemRow.cs

    public void Setup(ShopItem shopItem, int shopStock, int playerItemCount, bool isBuyMode, Action<ShopItem> buttonCallback)
    {
        this.currentShopItem = shopItem;
        this.onActionButtonClicked = buttonCallback;
        var itemData = shopItem.itemData;

        itemIcon.sprite = itemData.itemIcon;
        descriptionText.text = itemData.description;

        if (isBuyMode)
        {
            buttonText.text = "Buy";
            priceText.text = $"{shopItem.buyPrice} BYN";
            availableText.text = shopItem.isInfiniteStock ? "In stock" : $"{shopStock}";

            // --- НАЧАЛО НОВОЙ ЛОГИКИ ПРОВЕРКИ ---

            // 1. Базовые проверки: хватает ли денег и есть ли товар на складе.
            bool canAfford = PlayerWallet.Instance.HasEnoughMoney(shopItem.buyPrice);
            bool hasStock = shopItem.isInfiniteStock || shopStock > 0;

            // Начинаем с предположения, что купить можно, если базовые условия выполнены.
            bool isPurchaseable = canAfford && hasStock;

            // 2. Если базовые условия прошли, проводим более сложные, специфичные для типа предмета, проверки.
            if (isPurchaseable)
            {
                switch (itemData.itemType)
                {
                    // ПРОВЕРКА ДЛЯ УЛУЧШЕНИЙ
                    case ItemType.Upgrade:

                        // улучшение для грядок 

                       

                        // Это улучшение для склада?
                        if (InventoryManager.Instance.StorageUpgradeData == itemData)
                        {
                            isPurchaseable = !TrainUpgradeManager.Instance.HasUpgrade(itemData);
                            
                        }
                        else if (itemData == PlantManager.instance._UpgradeData)
                        {
                            isPurchaseable = true;
                        }
                        // Иначе, может это улучшение для загона?
                        else
                        {
                            var allConfigs = AnimalPenManager.Instance.GetAllPenConfigs();
                            var animalForThisUpgrade = allConfigs.FirstOrDefault(c => c.upgradeLevels.Any(l => l.requiredUpgradeItem == itemData))?.animalData;

                            if (animalForThisUpgrade != null)
                            {
                                ItemData nextUpgrade = AnimalPenManager.Instance.GetNextAvailableUpgrade(animalForThisUpgrade);
                                isPurchaseable = (nextUpgrade == itemData);
                            }
                            else
                            {
                                isPurchaseable = false; // Неизвестное улучшение
                                Debug.LogWarning($"Не удалось определить назначение улучшения: {itemData.name}");
                            }
                        }
                        break;

                    // ПРОВЕРКА ДЛЯ ЖИВОТНЫХ
                    case ItemType.Animal:
                        var animalData = itemData.associatedAnimalData;
                        if (animalData != null)
                        {
                            int currentCount = AnimalPenManager.Instance.GetAnimalCount(animalData);
                            int maxCapacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData);
                            if (currentCount >= maxCapacity)
                            {
                                isPurchaseable = false; // Нет места в загоне
                            }
                        }
                        else
                        {
                            isPurchaseable = false; // Ошибка в данных
                        }
                        break;

                    // ПРОВЕРКА ДЛЯ ВСЕХ ОСТАЛЬНЫХ ПРЕДМЕТОВ (СЕМЕНА, ИНСТРУМЕНТЫ, ПРОДУКТЫ)
                    default:
                        // Проверяем, есть ли место в инвентаре хотя бы для 1 штуки
                        if (!InventoryManager.Instance.CheckForSpace(itemData, 1))
                        {
                            isPurchaseable = false; // Нет места в инвентаре
                        }
                        break;
                }
            }

            actionButton.interactable = isPurchaseable;

            // --- КОНЕЦ НОВОЙ ЛОГИКИ ПРОВЕРКИ ---
        }
        else // Режим продажи
        {
            buttonText.text = "Sell";
            priceText.text = $"{shopItem.sellPrice} BYN";
            availableText.text = $"You have: {playerItemCount}";
            bool playerHasItem = playerItemCount > 0;
            actionButton.interactable = playerHasItem && shopItem.willBuy;
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        onActionButtonClicked?.Invoke(currentShopItem);
    }
}