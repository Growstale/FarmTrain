// ShopUIManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    // ... (все поля [SerializeField] остаются без изменений) ...
    [Header("Main Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;

    [Header("Item Display")]
    [SerializeField] private RectTransform itemsContentArea;
    [SerializeField] private GameObject shopItemRowPrefab;

    [Header("Confirmation Panel")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI totalPriceText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private ShopInventoryData currentShopData;
    private ShopItem currentItemForTransaction;
    private bool isBuyMode = true;
    private int transactionQuantity = 1;

    private List<GameObject> spawnedRows = new List<GameObject>();
    public event Action<ItemData, int> OnItemPurchased;
    public static event Action<ShopUIManager> OnInstanceReady;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // НЕ делаем DontDestroyOnLoad, так как он должен жить только на сцене станции.

        // <<< СООБЩАЕМ ВСЕМ, ЧТО МЫ ГОТОВЫ
        OnInstanceReady?.Invoke(this);
    }


    // ... (Start, OpenShop, CloseShop, SetMode, PopulateShopList, OnItemActionClicked, OpenConfirmationPanel - без изменений) ...
    // Все эти методы остаются прежними

    private void Start()
    {
        shopPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        closeButton.onClick.AddListener(CloseShop);
        buyTabButton.onClick.AddListener(() => SetMode(true));
        sellTabButton.onClick.AddListener(() => SetMode(false));

        plusButton.onClick.AddListener(IncreaseQuantity);
        minusButton.onClick.AddListener(DecreaseQuantity);
        confirmButton.onClick.AddListener(ConfirmTransaction);
        cancelButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
    }

    public void OpenShop(ShopInventoryData shopData)
    {
        if (StallCameraController.Instance == null)
        {
            Debug.LogWarning("StallCameraController не найден. Магазин откроется без управления камерой.");
        }

        currentShopData = shopData;
        shopNameText.text = shopData.shopName;
        SetMode(true);
        shopPanel.SetActive(true);
    }


    public void CloseShop()
    {
        if (!shopPanel.activeSelf) return;

        Debug.Log("Закрытие панели магазина.");
        shopPanel.SetActive(false);
        currentShopData = null;

        if (StallCameraController.Instance != null)
        {
            StallCameraController.Instance.EnterOverviewMode();
        }
    }


    private void SetMode(bool buy)
    {
        isBuyMode = buy;
        buyTabButton.interactable = !isBuyMode;
        sellTabButton.interactable = isBuyMode;

        PopulateShopList();
    }

    private void PopulateShopList()
    {
        foreach (var row in spawnedRows)
        {
            Destroy(row);
        }
        spawnedRows.Clear();

        foreach (var shopItem in currentShopData.shopItems)
        {
            if (isBuyMode && !shopItem.forSale) continue;
            if (!isBuyMode && !shopItem.willBuy) continue;

            GameObject rowGO = Instantiate(shopItemRowPrefab, itemsContentArea);
            ShopItemRow rowScript = rowGO.GetComponent<ShopItemRow>();

            int shopStock = ShopDataManager.Instance.GetCurrentStock(currentShopData, shopItem.itemData);

            int playerItemCount = 0;
            if (shopItem.itemData.itemType == ItemType.Animal)
            {
                playerItemCount = AnimalPenManager.Instance.GetAnimalCount(shopItem.itemData.associatedAnimalData);
            }
            else
            {
                playerItemCount = InventoryManager.Instance.GetTotalItemQuantity(shopItem.itemData);
            }

            rowScript.Setup(shopItem, shopStock, playerItemCount, isBuyMode, OnItemActionClicked);
            spawnedRows.Add(rowGO);
        }

    }

    private void OnItemActionClicked(ShopItem shopItem)
    {
        Debug.Log($"Нажата кнопка для {shopItem.itemData.itemName}");
        currentItemForTransaction = shopItem;
        transactionQuantity = 1;
        OpenConfirmationPanel();
    }

    private void OpenConfirmationPanel()
    {
        UpdateConfirmationPanel();
        confirmationPanel.SetActive(true);
    }


    private void UpdateConfirmationPanel()
    {
        if (currentItemForTransaction == null) return;

        var itemData = currentItemForTransaction.itemData;
        int price = isBuyMode ? currentItemForTransaction.buyPrice : currentItemForTransaction.sellPrice;
        int maxQuantity = int.MaxValue;

        if (itemData.itemType == ItemType.Animal)
        {
            if (isBuyMode)
            {
                int stock = ShopDataManager.Instance.GetCurrentStock(currentShopData, itemData);
                // int price = currentItemForTransaction.buyPrice; // <<< УДАЛЯЕМ ЭТУ СТРОКУ
                int affordable = (price > 0) ? PlayerWallet.Instance.GetCurrentMoney() / price : int.MaxValue;
                maxQuantity = Mathf.Min(stock, affordable);

                var animalData = itemData.associatedAnimalData;
                if (animalData != null)
                {
                    int currentAnimalCount = AnimalPenManager.Instance.GetAnimalCount(animalData);
                    int maxCapacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData);

                    int availableSpace = maxCapacity - currentAnimalCount;
                    maxQuantity = Mathf.Min(maxQuantity, availableSpace);
                }
                else
                {
                    Debug.LogError($"У предмета {itemData.name} поле associatedAnimalData ПУСТОЕ!");
                    maxQuantity = 0;
                }
            }
            else // Режим продажи
            {
                maxQuantity = AnimalPenManager.Instance.GetAnimalCount(itemData.associatedAnimalData);
            }
        }
        else
        {
            // Логика для обычных предметов (не меняется)
            if (isBuyMode)
            {
                int stock = ShopDataManager.Instance.GetCurrentStock(currentShopData, itemData);
                int affordable = (price > 0) ? PlayerWallet.Instance.GetCurrentMoney() / price : int.MaxValue;
                maxQuantity = Mathf.Min(stock, affordable);
            }
            else
            {
                maxQuantity = InventoryManager.Instance.GetTotalItemQuantity(itemData);
            }
        }

        transactionQuantity = Mathf.Clamp(transactionQuantity, 1, maxQuantity);
        if (maxQuantity <= 0) transactionQuantity = 0;

        quantityText.text = transactionQuantity.ToString();
        totalPriceText.text = $"{transactionQuantity * price}";
        confirmButton.interactable = transactionQuantity > 0;
    }


    private void IncreaseQuantity()
    {
        transactionQuantity++;
        UpdateConfirmationPanel();
    }

    private void DecreaseQuantity()
    {
        transactionQuantity--;
        if (transactionQuantity < 1) transactionQuantity = 1;
        UpdateConfirmationPanel();
    }

    private void ConfirmTransaction()
    {
        if (transactionQuantity <= 0) return;

        var itemData = currentItemForTransaction.itemData;
        int totalPrice = (isBuyMode ? currentItemForTransaction.buyPrice : currentItemForTransaction.sellPrice) * transactionQuantity;

        if (itemData.itemType == ItemType.Animal)
        {
            if (isBuyMode)
            {
                var animalData = itemData.associatedAnimalData;
                if (animalData == null)
                {
                    Debug.LogError($"У предмета {itemData.itemName} не указана ссылка на AnimalData! Транзакция отменена.");
                    confirmationPanel.SetActive(false);
                    return;
                }

                // <<< ИСПРАВЛЕНИЕ ОШИБКИ >>>
                // Повторяем проверку, используя новый метод
                int currentAnimalCount = AnimalPenManager.Instance.GetAnimalCount(animalData);
                int maxCapacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData);

                if (currentAnimalCount + transactionQuantity > maxCapacity)
                {
                    Debug.LogError($"Ошибка! Попытка купить {transactionQuantity} животных, но в загоне нет места. Транзакция отменена.");
                    confirmationPanel.SetActive(false);
                    return;
                }

                if (!PlayerWallet.Instance.HasEnoughMoney(totalPrice)) return;

                PlayerWallet.Instance.SpendMoney(totalPrice);
                ShopDataManager.Instance.DecreaseStock(currentShopData, itemData, transactionQuantity);
                for (int i = 0; i < transactionQuantity; i++)
                {
                    AnimalPenManager.Instance.AddAnimal(itemData.associatedAnimalData);
                }
                OnItemPurchased?.Invoke(itemData, transactionQuantity); // Событие для квестов вызываем для ВСЕХ покупок
            }
            else // Режим продажи
            {
                if (AnimalPenManager.Instance.GetAnimalCount(itemData.associatedAnimalData) < transactionQuantity) return;

                // <<< ИЗМЕНЕНИЕ: Блок `if (TrainPenController.Instance != null)` НЕ НУЖЕН при продаже.
                // Продажа - это чисто экономическая/логическая операция. Мы просто удаляем данные из AnimalPenManager.
                // Despawn животного произойдет автоматически при следующей загрузке сцены с поездом,
                // так как TrainPenController просто не найдет для него данных и не создаст GameObject.
                // Это делает систему более надежной. Если игрок продаст животное на станции,
                // а потом игра вылетит до захода на поезд - данные все равно будут верными.

                PlayerWallet.Instance.AddMoney(totalPrice);
                ShopDataManager.Instance.IncreaseStock(currentShopData, itemData, transactionQuantity);
                for (int i = 0; i < transactionQuantity; i++)
                {
                    // <<< ИЗМЕНЕНИЕ: Мы вызываем `SellAnimal` вместо `DespawnAnimal`
                    AnimalPenManager.Instance.SellAnimal(itemData.associatedAnimalData);
                }
            }
        }
        else
        {
            // Логика для обычных предметов (не меняется)
            if (isBuyMode)
            {
                if (!PlayerWallet.Instance.HasEnoughMoney(totalPrice)) return;

                if (itemData.itemType == ItemType.Upgrade)
                {
                    // Улучшения не занимают места в инвентаре, так что проверку на место пропускаем
                    PlayerWallet.Instance.SpendMoney(totalPrice);
                    TrainUpgradeManager.Instance.PurchaseUpgrade(itemData); // Регистрируем покупку улучшения

                    // Теперь определяем, КАКОЕ это улучшение
                    if (InventoryManager.Instance.StorageUpgradeData == itemData)
                    {
                        // Это улучшение склада!
                        // Никаких других действий не требуется, InventoryManager сам подхватит изменение
                        // через TrainUpgradeManager.Instance.HasUpgrade() в своем Update.
                        Debug.Log($"<color=cyan>Успешно куплено улучшение для склада:</color> {itemData.itemName}");
                    }
                    else
                    {
                        // Предполагаем, что это улучшение для загона животных
                        Debug.Log($"<color=cyan>Попытка применить улучшение для загона:</color> {itemData.itemName}");
                        AnimalPenManager.Instance.ApplyUpgrade(itemData);
                    }
                }
                else // Если это НЕ улучшение (обычный предмет)
                {
                    if (!InventoryManager.Instance.CheckForSpace(itemData, transactionQuantity)) return;

                    PlayerWallet.Instance.SpendMoney(totalPrice);
                    InventoryManager.Instance.AddItem(itemData, transactionQuantity);
                }

                ShopDataManager.Instance.DecreaseStock(currentShopData, itemData, transactionQuantity);
                OnItemPurchased?.Invoke(itemData, transactionQuantity); // Событие для квестов вызываем для ВСЕХ покупок

            }
            else
            {
                if (InventoryManager.Instance.GetTotalItemQuantity(itemData) < transactionQuantity) return;

                PlayerWallet.Instance.AddMoney(totalPrice);
                InventoryManager.Instance.RemoveItemByType(itemData, transactionQuantity);
                OnItemPurchased?.Invoke(itemData, transactionQuantity);
                ShopDataManager.Instance.IncreaseStock(currentShopData, itemData, transactionQuantity);
            }
        }

        if (itemData.itemType == ItemType.Animal && isBuyMode)
        {
            var animalData = itemData.associatedAnimalData;
            int countAfterPurchase = AnimalPenManager.Instance.GetAnimalCount(animalData);
            Debug.Log($"<color=cyan>[SHOP DEBUG]</color> После покупки животного '{animalData.speciesName}' их стало: {countAfterPurchase}");
        }

        Debug.Log("Транзакция успешна!");
        confirmationPanel.SetActive(false);
        PopulateShopList();
    }
}