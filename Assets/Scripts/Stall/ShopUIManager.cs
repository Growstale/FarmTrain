using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;

[System.Serializable]
public struct TabButtonSprites
{
    public Sprite activeSprite;
    public Sprite inactiveSprite;
}

public class ShopUIManager : MonoBehaviour, IUIManageable
{
    public static ShopUIManager Instance { get; private set; }

    private TooltipTrigger plusButtonTooltip;
    private TooltipTrigger minusButtonTooltip;

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
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button cancelButton;

    [Header("Tab Sprites")] 
    [SerializeField] private TabButtonSprites buyButtonSprites;
    [SerializeField] private TabButtonSprites sellButtonSprites;

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



    private void Start()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Register(this);
        }

        shopPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        closeButton.onClick.AddListener(CloseShop);
        buyTabButton.onClick.AddListener(() => SetMode(true));
        sellTabButton.onClick.AddListener(() => SetMode(false));

        plusButton.onClick.AddListener(IncreaseQuantity);
        minusButton.onClick.AddListener(DecreaseQuantity);
        yesButton.onClick.AddListener(ConfirmTransaction);
        cancelButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
        noButton.onClick.AddListener(() => confirmationPanel.SetActive(false));

        plusButtonTooltip = plusButton.GetComponent<TooltipTrigger>();
        minusButtonTooltip = minusButton.GetComponent<TooltipTrigger>();

    }

    public void OpenShop(ShopInventoryData shopData)
    {
        ExclusiveUIManager.Instance.NotifyPanelOpening(this);

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
        confirmationPanel.SetActive(false);

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


        // Получаем компоненты Image у наших кнопок
        Image buyButtonImage = buyTabButton.GetComponent<Image>();
        Image sellButtonImage = sellTabButton.GetComponent<Image>();

        // Проверяем, что все 4 спрайта назначены, чтобы избежать ошибок
        if (buyButtonSprites.activeSprite == null || buyButtonSprites.inactiveSprite == null ||
            sellButtonSprites.activeSprite == null || sellButtonSprites.inactiveSprite == null)
        {
            Debug.LogWarning("Не все спрайты для вкладок назначены в ShopUIManager!");
            // Выходим, чтобы не было ошибки при попытке присвоить пустой спрайт
            PopulateShopList();
            return;
        }

        // Устанавливаем спрайты в зависимости от режима
        if (isBuyMode)
        {
            // Режим покупки: кнопка "Buy" активна, "Sell" - неактивна
            buyButtonImage.sprite = buyButtonSprites.activeSprite;
            sellButtonImage.sprite = sellButtonSprites.inactiveSprite;
        }
        else // Режим продажи
        {
            // Режим продажи: кнопка "Buy" неактивна, "Sell" - активна
            buyButtonImage.sprite = buyButtonSprites.inactiveSprite;
            sellButtonImage.sprite = sellButtonSprites.activeSprite;
        }


        PopulateShopList();
    }

    private void PopulateShopList()
    {
        foreach (var row in spawnedRows)
        {
            Destroy(row);
        }
        spawnedRows.Clear();

        List<ShopItem> itemsToDisplay = new List<ShopItem>();

        if (isBuyMode)
        {
            // Фильтруем и СОРТИРУЕМ товары для покупки
            itemsToDisplay = currentShopData.shopItems
                .Where(item => item.forSale)
                .OrderByDescending(item => IsItemPurchaseable(item, out _)) // Сортируем: сначала доступные (true), потом недоступные (false)
                .ToList();
        }
        else
        {
            // Для продажи просто фильтруем
            itemsToDisplay = currentShopData.shopItems.Where(item => item.willBuy).ToList();
        }

        foreach (var shopItem in itemsToDisplay)
        {
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

            // Передаем управление в ShopItemRow. Он сам разберется с кнопкой и подсказкой.
            rowScript.Setup(shopItem, shopStock, playerItemCount, isBuyMode, OnItemActionClicked);
            spawnedRows.Add(rowGO);
        }
    }

    public bool IsItemPurchaseable(ShopItem shopItem, out string reason)
    {
        reason = "";
        var itemData = shopItem.itemData;
        int shopStock = ShopDataManager.Instance.GetCurrentStock(currentShopData, itemData);
        if (!shopItem.isInfiniteStock && shopStock <= 0)
        {
            reason = "Out of stock";
            return false;
        }
        if (!PlayerWallet.Instance.HasEnoughMoney(shopItem.buyPrice))
        {
            reason = "Not enough money";
            return false;
        }

        switch (itemData.itemType)
        {
            case ItemType.Upgrade:
                if (InventoryManager.Instance.StorageUpgradeData == itemData)
                {
                    if (TrainUpgradeManager.Instance.HasUpgrade(itemData))
                    {
                        reason = "The upgrade has already been purchased";
                        return false;
                    }
                }
                else if (itemData == PlantManager.instance._UpgradeData)
                {
                    if (PlantManager.instance.UpgradeWatering)
                    {
                        reason = "The upgrade has already been purchased";
                        return false;
                    }
                }
                else
                {
                    var allConfigs = AnimalPenManager.Instance.GetAllPenConfigs();
                    var animalForThisUpgrade = allConfigs.FirstOrDefault(c => c.upgradeLevels.Any(l => l.requiredUpgradeItem == itemData))?.animalData;
                    if (animalForThisUpgrade != null)
                    {
                        if (AnimalPenManager.Instance.GetNextAvailableUpgrade(animalForThisUpgrade) != itemData)
                        {
                            reason = "Previous improvement is required";
                            return false;
                        }
                    }
                }
                break;

            case ItemType.Animal:
                var animalData = itemData.associatedAnimalData;
                if (animalData != null)
                {
                    int currentCount = AnimalPenManager.Instance.GetAnimalCount(animalData);
                    int maxCapacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData);
                    if (currentCount >= maxCapacity)
                    {
                        reason = "The pen is full";
                        return false;
                    }
                }
                break;

            default:
                if (!InventoryManager.Instance.CheckForSpace(itemData, 1))
                {
                    reason = "Inventory is full";
                    return false;
                }
                break;
        }

        return true;
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
        string plusDisabledReason = "";

        if (isBuyMode)
        {
            // Логика для покупки (остается без изменений)
            int stock = ShopDataManager.Instance.GetCurrentStock(currentShopData, itemData);
            if (!currentItemForTransaction.isInfiniteStock && stock < maxQuantity)
            {
                maxQuantity = stock;
                plusDisabledReason = "Out of stock";
            }

            int affordable = (price > 0) ? PlayerWallet.Instance.GetCurrentMoney() / price : int.MaxValue;
            if (affordable < maxQuantity)
            {
                maxQuantity = affordable;
                plusDisabledReason = "Not enough money";
            }

            if (itemData.itemType == ItemType.Animal)
            {
                var animalData = itemData.associatedAnimalData;
                if (animalData != null)
                {
                    int availableSpace = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData) - AnimalPenManager.Instance.GetAnimalCount(animalData);
                    if (availableSpace < maxQuantity)
                    {
                        maxQuantity = availableSpace;
                        plusDisabledReason = "The pen is full";
                    }
                }
            }
            else if (itemData.itemType != ItemType.Upgrade)
            {
                if (!InventoryManager.Instance.CheckForSpace(itemData, transactionQuantity + 1))
                {
                    if (transactionQuantity < maxQuantity)
                    {
                        maxQuantity = transactionQuantity;
                        plusDisabledReason = "Inventory is full";
                    }
                }
            }
        }
        else // Режим продажи
        {
            // --- НАЧАЛО ИСПРАВЛЕНИЯ ---
            if (itemData.itemType == ItemType.Animal)
            {
                // Получаем общее количество животных этого типа
                int totalPlayerAnimals = AnimalPenManager.Instance.GetAnimalCount(itemData.associatedAnimalData);
                // Максимальное количество для продажи - это все, КРОМЕ ОДНОГО.
                maxQuantity = totalPlayerAnimals - 1;
                plusDisabledReason = "Can't sell the last";
            }
            else // Для обычных предметов
            {
                maxQuantity = InventoryManager.Instance.GetTotalItemQuantity(itemData);
                plusDisabledReason = "You don't have any more";
            }
            // --- КОНЕЦ ИСПРАВЛЕНИЯ ---
        }

        // Защита от отрицательных значений, если у игрока 0 или 1 животное
        if (maxQuantity < 0)
        {
            maxQuantity = 0;
        }

        transactionQuantity = Mathf.Clamp(transactionQuantity, 1, maxQuantity);
        if (maxQuantity <= 0) transactionQuantity = 0;

        quantityText.text = transactionQuantity.ToString();
        totalPriceText.text = $"{transactionQuantity * price}";
        yesButton.interactable = transactionQuantity > 0;

        // Обновляем состояние кнопок +/- и их подсказок
        plusButton.interactable = (maxQuantity > 0 && transactionQuantity < maxQuantity);
        minusButton.interactable = transactionQuantity > 1;

        if (plusButtonTooltip != null)
        {
            plusButtonTooltip.SetTooltip(plusButton.interactable ? "" : plusDisabledReason);
        }
        if (minusButtonTooltip != null)
        {
            minusButtonTooltip.SetTooltip(minusButton.interactable ? "" : "You cannot select less than 1");
        }
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
                    else if(itemData == PlantManager.instance._UpgradeData)
                    {
                        Debug.Log($"<color=cyan>Попытка применить улучшение для полива растений:</color> {itemData.itemName}");
                        PlantManager.instance.CompleteWateringUpgrade();
                    }
                    else
                    {
                        // Предполагаем, что это улучшение для загона животных
                        Debug.Log($"<color=cyan>Попытка применить улучшение для загона:</color> {itemData.itemName}");
                        AnimalPenManager.Instance.ApplyUpgrade(itemData);
                    }
                    GameEvents.TriggerAddedNewUpdgrade(1);
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
                GameEvents.TriggerHarvestCrop(transactionQuantity);
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

    public void CloseUI()
    {
        CloseShop();
    }

    public bool IsOpen()
    {
        return shopPanel.activeSelf;
    }

    private void OnDestroy()
    {
        if (ExclusiveUIManager.Instance != null)
        {
            ExclusiveUIManager.Instance.Deregister(this);
        }
    }
}