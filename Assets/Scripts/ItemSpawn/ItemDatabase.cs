// ItemDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;

    private Dictionary<string, ItemData> itemsByName;
    private bool isInitialized = false;

    private void Initialize()
    {
        if (isInitialized) return;

        itemsByName = new Dictionary<string, ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                // --- ГЛАВНОЕ ИЗМЕНЕНИЕ ---
                // "Очищаем" ключ перед добавлением в словарь
                string cleanKey = item.itemName.Trim();

                if (!itemsByName.ContainsKey(cleanKey))
                {
                    itemsByName.Add(cleanKey, item);
                }
                else
                {
                    Debug.LogWarning($"В ItemDatabase найден дубликат после очистки ключа: '{cleanKey}'");
                }
            }
            else
            {
                Debug.LogWarning("В ItemDatabase найден пустой элемент или элемент с пустым именем.");
            }
        }
        isInitialized = true;
    }

    // Публичный метод для получения ItemData по его имени (itemName)
    public ItemData GetItemByName(string name)
    {
        Initialize();

        // --- ГЛАВНОЕ ИЗМЕНЕНИЕ ---
        // "Очищаем" искомое имя перед поиском
        string cleanName = name.Trim();

        itemsByName.TryGetValue(cleanName, out ItemData item);
        return item;
    }

    public ItemData GetSeedByPlantName(string plantName)
    {
        Initialize();

        // Здесь тоже можно добавить очистку, если имена растений могут содержать пробелы
        string cleanPlantName = plantName.Trim();

        return allItems.FirstOrDefault(item =>
            item.itemType == ItemType.Seed &&
            item.associatedPlantData != null &&
            item.associatedPlantData.plantName.Trim() == cleanPlantName
        );
    }
}