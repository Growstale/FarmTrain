using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false;
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;

    Transform seed;


    private InventoryManager inventoryManager; // Ссылка на менеджер инвентаря

    [SerializeField] GameObject _itemSpawnManager;

     ItemSpawner _itemSpawner;
    

    void Start()
    {

        inventoryManager = InventoryManager.Instance; // И поиск синглтона тоже
        _itemSpawner = _itemSpawnManager.GetComponent<ItemSpawner>();
        if(_itemSpawner == null)
        {
            Debug.Log("itemSpaner not found!");
        }

        slot = transform.Find("Square");
        seed = transform.Find("Seed");
        spriteRenderer = slot.GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
        if (slot == null)
        {
            Debug.Log("not find");
        }
        else
        {
            Debug.Log("<< find");
        }
      
    }

    public void PlantSeeds()
    {
        
        // Получаем ВЫБРАННЫЙ предмет и ИНДЕКС выбранного слота
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

        // Проверяем, есть ли выбранный предмет и является ли он семенами
        if (selectedItem != null && !selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && !isPlanted)
        {

                _itemSpawner.SpawnItem(selectedItem.itemData, transform.position);
                isPlanted = true;
                InventoryManager.Instance.RemoveItem(selectedIndex);
        }
        else
        {
            if (selectedItem != null)
            {

                if (isPlanted)
                {

                    Debug.Log("Тут уже занято, куда??");
                }
                if (selectedItem.itemData.itemType != ItemType.Seed)
                {
                    Debug.Log("Садить можно только семена ;)");
                }

                
            }
            else
            {
                Debug.Log($"Выбери предмет из инвентаря");
            }
        }
    }
    public void ChangeColor()
    {

        slot.GetComponent<SpriteRenderer>().color = new Color(0, 255f, 0, 0.1f);

    }
    public void UnChangeColor()
    {
        slot.GetComponent<SpriteRenderer>().color = currentColor;
    }
}
