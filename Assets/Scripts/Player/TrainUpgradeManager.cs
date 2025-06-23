using UnityEngine;
using System.Collections.Generic;

public class TrainUpgradeManager : MonoBehaviour
{
    public static TrainUpgradeManager Instance { get; private set; }

    private HashSet<ItemData> purchasedUpgrades = new HashSet<ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public bool HasUpgrade(ItemData upgradeData)
    {
        return purchasedUpgrades.Contains(upgradeData);
    }

    public void PurchaseUpgrade(ItemData upgradeData)
    {
        if (!HasUpgrade(upgradeData))
        {
            purchasedUpgrades.Add(upgradeData);
            Debug.Log($"Куплено улучшение: {upgradeData.itemName}");
        }
    }
}