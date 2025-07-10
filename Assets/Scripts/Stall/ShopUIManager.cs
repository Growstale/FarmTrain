// ShopUIManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    // ... (��� ���� [SerializeField] �������� ��� ���������) ...
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
        // �� ������ DontDestroyOnLoad, ��� ��� �� ������ ���� ������ �� ����� �������.

        // <<< �������� ����, ��� �� ������
        OnInstanceReady?.Invoke(this);
    }


    // ... (Start, OpenShop, CloseShop, SetMode, PopulateShopList, OnItemActionClicked, OpenConfirmationPanel - ��� ���������) ...
    // ��� ��� ������ �������� ��������

    private void Start()
    {
        shopPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        closeButton.onClick.AddListener(CloseShop);
        buyTabButton.onClick.AddListener(() => SetMode(true));
        sellTabButton.onClick.AddListener(() => SetMode(false));

        plusButton.onClick.AddListener(IncreaseQuantity);
        minusButton.onClick.AddListener(DecreaseQuantity);
        yesButton.onClick.AddListener(ConfirmTransaction);
        noButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
        cancelButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
    }

    public void OpenShop(ShopInventoryData shopData)
    {
        if (StallCameraController.Instance == null)
        {
            Debug.LogWarning("StallCameraController �� ������. ������� ��������� ��� ���������� �������.");
        }

        currentShopData = shopData;
        shopNameText.text = shopData.shopName;
        SetMode(true);
        shopPanel.SetActive(true);
    }


    public void CloseShop()
    {
        if (!shopPanel.activeSelf) return;

        Debug.Log("�������� ������ ��������.");
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
        Debug.Log($"������ ������ ��� {shopItem.itemData.itemName}");
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
                // int price = currentItemForTransaction.buyPrice; // <<< ������� ��� ������
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
                    Debug.LogError($"� �������� {itemData.name} ���� associatedAnimalData ������!");
                    maxQuantity = 0;
                }
            }
            else // ����� �������
            {
                maxQuantity = AnimalPenManager.Instance.GetAnimalCount(itemData.associatedAnimalData);
            }
        }
        else
        {
            // ������ ��� ������� ��������� (�� ��������)
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
        yesButton.interactable = transactionQuantity > 0;
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
                    Debug.LogError($"� �������� {itemData.itemName} �� ������� ������ �� AnimalData! ���������� ��������.");
                    confirmationPanel.SetActive(false);
                    return;
                }

                // <<< ����������� ������ >>>
                // ��������� ��������, ��������� ����� �����
                int currentAnimalCount = AnimalPenManager.Instance.GetAnimalCount(animalData);
                int maxCapacity = AnimalPenManager.Instance.GetMaxCapacityForAnimal(animalData);

                if (currentAnimalCount + transactionQuantity > maxCapacity)
                {
                    Debug.LogError($"������! ������� ������ {transactionQuantity} ��������, �� � ������ ��� �����. ���������� ��������.");
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
                OnItemPurchased?.Invoke(itemData, transactionQuantity); // ������� ��� ������� �������� ��� ���� �������
            }
            else // ����� �������
            {
                if (AnimalPenManager.Instance.GetAnimalCount(itemData.associatedAnimalData) < transactionQuantity) return;

                // <<< ���������: ���� `if (TrainPenController.Instance != null)` �� ����� ��� �������.
                // ������� - ��� ����� �������������/���������� ��������. �� ������ ������� ������ �� AnimalPenManager.
                // Despawn ��������� ���������� ������������� ��� ��������� �������� ����� � �������,
                // ��� ��� TrainPenController ������ �� ������ ��� ���� ������ � �� ������� GameObject.
                // ��� ������ ������� ����� ��������. ���� ����� ������� �������� �� �������,
                // � ����� ���� ������� �� ������ �� ����� - ������ ��� ����� ����� �������.

                PlayerWallet.Instance.AddMoney(totalPrice);
                ShopDataManager.Instance.IncreaseStock(currentShopData, itemData, transactionQuantity);
                for (int i = 0; i < transactionQuantity; i++)
                {
                    // <<< ���������: �� �������� `SellAnimal` ������ `DespawnAnimal`
                    AnimalPenManager.Instance.SellAnimal(itemData.associatedAnimalData);
                }
            }
        }
        else
        {
            // ������ ��� ������� ��������� (�� ��������)
            if (isBuyMode)
            {
                if (!PlayerWallet.Instance.HasEnoughMoney(totalPrice)) return;

                if (itemData.itemType == ItemType.Upgrade)
                {
                    // ��������� �� �������� ����� � ���������, ��� ��� �������� �� ����� ����������
                    PlayerWallet.Instance.SpendMoney(totalPrice);
                    TrainUpgradeManager.Instance.PurchaseUpgrade(itemData); // ������������ ������� ���������

                    // ������ ����������, ����� ��� ���������
                    if (InventoryManager.Instance.StorageUpgradeData == itemData)
                    {
                        // ��� ��������� ������!
                        // ������� ������ �������� �� ���������, InventoryManager ��� ��������� ���������
                        // ����� TrainUpgradeManager.Instance.HasUpgrade() � ����� Update.
                        Debug.Log($"<color=cyan>������� ������� ��������� ��� ������:</color> {itemData.itemName}");

                    }
                    else if(itemData == PlantManager.instance._UpgradeData)
                    {
                        Debug.Log($"<color=cyan>������� ��������� ��������� ��� ������ ��������:</color> {itemData.itemName}");
                        PlantManager.instance.CompleteWateringUpgrade();
                    }
                    else
                    {
                        // ������������, ��� ��� ��������� ��� ������ ��������
                        Debug.Log($"<color=cyan>������� ��������� ��������� ��� ������:</color> {itemData.itemName}");
                        AnimalPenManager.Instance.ApplyUpgrade(itemData);
                    }
                    GameEvents.TriggerAddedNewUpdgrade(1);
                }
                else // ���� ��� �� ��������� (������� �������)
                {
                    if (!InventoryManager.Instance.CheckForSpace(itemData, transactionQuantity)) return;

                    PlayerWallet.Instance.SpendMoney(totalPrice);
                    InventoryManager.Instance.AddItem(itemData, transactionQuantity);
                }

                ShopDataManager.Instance.DecreaseStock(currentShopData, itemData, transactionQuantity);
                OnItemPurchased?.Invoke(itemData, transactionQuantity); // ������� ��� ������� �������� ��� ���� �������

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
            Debug.Log($"<color=cyan>[SHOP DEBUG]</color> ����� ������� ��������� '{animalData.speciesName}' �� �����: {countAfterPurchase}");
        }

        Debug.Log("���������� �������!");
        confirmationPanel.SetActive(false);
        PopulateShopList();
    }
}