using UnityEngine;

public class StallInteraction : MonoBehaviour
{
    [Header("Shop Data")]
    public ShopInventoryData shopInventoryData;

    private void Start()
    {
        if (shopInventoryData != null && ShopDataManager.Instance != null)
        {
            ShopDataManager.Instance.InitializeShop(shopInventoryData);
        }
    }

    public void OpenShopUI()
    {
        if (shopInventoryData == null)
        {
            Debug.LogError($"У ларька {gameObject.name} не назначены данные магазина (ShopInventoryData)!");
            return;
        }

        if (ShopUIManager.Instance == null)
        {
            Debug.LogError("ShopUIManager не найден на сцене! Невозможно открыть магазин.");
            return;
        }

        Debug.Log($"Открываем UI для магазина: {shopInventoryData.name}");
        ShopUIManager.Instance.OpenShop(shopInventoryData);
    }
}