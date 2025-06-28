using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false; // есть ли растение
    public bool ishavebed = false;  // есть ли грядка
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;



    private InventoryManager inventoryManager; // Ссылка на менеджер инвентаря

   

    [SerializeField] ItemSpawner _itemSpawner;
    

    void Start()
    {

        inventoryManager = InventoryManager.Instance; // И поиск синглтона тоже
       
        if(_itemSpawner == null)
        {
            Debug.Log("itemSpaner not found!");
        }

        //slot = transform.Find("Square");
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
        //if (slot == null)
        //{
        //    Debug.Log("not find");
        //}
        //else
        //{
        //    // Debug.Log("<< find"); закоментила так как спамило всю консоль 
        //    spriteRenderer = slot.GetComponent<SpriteRenderer>();
        //}
      
    }

    public void PlantSeeds()
    {
        Debug.Log(">>>>> Try plant new seeds ");

        // Получаем ВЫБРАННЫЙ предмет и ИНДЕКС выбранного слота
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

       
        if(selectedItem == null)
        {
            Debug.Log("Выбери предмет из инвенторя");
        }

        else
        {
            // Проверяем, есть ли выбранный предмет и является ли он горшком или грядкой
            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Pot && !isPlanted)
            {

               

                if (ishavebed)
                {

                    Debug.Log("Тут уже занято, куда??");
                }
                else
                {
                    _itemSpawner.TestSpawnBed(selectedItem.itemData, transform.position, new Vector3(0.25f,0.25f,0.25f));
                    ishavebed = true;
                    InventoryManager.Instance.RemoveItem(selectedIndex);

                }

            }

            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && ishavebed && !isPlanted)
            {

                Transform parentSlot = transform.parent;

                BedSlotController bedSlotController = parentSlot.GetComponent<BedSlotController>();
                if (parentSlot != null)
                {
                    float weightSeed = selectedItem.itemData.associatedPlantData.Weight;

                    // проверка веса растения 
                 
                    switch (weightSeed)
                    {
                        case 1:
                            _itemSpawner.TestSpawnPlant(selectedItem.itemData, transform.position, new Vector3(0.5f, 0.5f, 0.5f));
                            isPlanted = true;
                            InventoryManager.Instance.RemoveItem(selectedIndex);
                            break;

                        case 2:
                           
                            if (bedSlotController != null)
                            {

                                bool isFreeSlot = bedSlotController.CheckFreeSlot(2);

                                if (isFreeSlot)
                                {
                                    _itemSpawner.TestSpawnPlant(selectedItem.itemData, bedSlotController.Plant2Slot(gameObject.name), new Vector3(0.5f, 0.5f, 0.5f));
                                    InventoryManager.Instance.RemoveItem(selectedIndex);
                                }
                                else
                                {
                                    Debug.Log("Не хватает грядок, надо купить еще");

                                }

                            }
                            else
                            {
                                Debug.Log("Ошибка заполнения, отсутствует скрипт BedSlotController у родительского Slot");
                            }
                            break;
                        case 4:
                          
                            if (bedSlotController != null)
                            {

                                bool isFreeSlot = bedSlotController.CheckFreeSlot(4);

                                if (isFreeSlot)
                                {
                                    _itemSpawner.TestSpawnPlant(selectedItem.itemData, bedSlotController.Plant4Slot(), new Vector3(0.5f, 0.5f, 0.5f));
                                    InventoryManager.Instance.RemoveItem(selectedIndex);
                                }
                                else
                                {
                                    Debug.Log("Не хватает грядок, надо купить еще");

                                }

                            }
                            else
                            {
                                Debug.Log("Ошибка заполнения, отсутствует скрипт BedSlotController у родительского Slot");
                            }
                                break;

                    }

                }
                else
                {
                    Debug.LogError("Не найден родительский слот! Ошибка");
                }
               
            }
            else
            {
                    if (isPlanted)
                    {

                        Debug.Log("Тут уже занято, куда??");
                    }
                    if (selectedItem.itemData.itemType != ItemType.Seed)
                    {
                        Debug.Log("Тут только семена ;)");
                    }
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
