using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public List<string> GetSaveData()
    {
        return purchasedUpgrades.Select(ud => ud.name).ToList();
    }

    public void ApplySaveData(TrainUpgradesSaveData data)
    {
        purchasedUpgrades.Clear();
        if (data == null || data.purchasedUpgradeItemNames == null) return;

        foreach (var itemName in data.purchasedUpgradeItemNames)
        {
            ItemData upgradeAsset = Resources.Load<ItemData>($"Data/{itemName}");
            if (upgradeAsset != null)
            {
                purchasedUpgrades.Add(upgradeAsset);
            }
        }
    }
}