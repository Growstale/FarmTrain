using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false;
    bool ishavebed = false; // есть ли грядка 
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;

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
        spriteRenderer = slot.GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
        if (slot == null)
        {
            Debug.Log("not find");
        }
        else
        {
            // Debug.Log("<< find"); закоментила так как спамило всю консоль
        }
      
    }

    public void PlantSeeds()
    {
        
        // Получаем ВЫБРАННЫЙ предмет и ИНДЕКС выбранного слота
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

        // Проверяем, есть ли выбранный предмет и является ли он семенами
        if (selectedItem != null && !selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Pot && !isPlanted)
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
                if (selectedItem.itemData.itemType != ItemType.Pot)
                {
                    Debug.Log("Сначала нужна земелька ;)");
                }

                
            }
            else
            {
                Debug.Log($"Выбери предмет из инвентаря");
            }
        }


        if(selectedItem != null && !selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && !ishavebed && isPlanted)
        {
            _itemSpawner.SpawnItem(selectedItem.itemData, transform.position);
            ishavebed = true;
            InventoryManager.Instance.RemoveItem(selectedIndex);
        }
        else
        {
            if (selectedItem != null)
            {

                if (ishavebed)
                {

                    Debug.Log("Тут уже занято, куда??");
                }
                if (selectedItem.itemData.itemType != ItemType.Seed)
                {
                    Debug.Log("Тут только семена ;)");
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
