using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false; // есть ли растение
    public bool ishavebed = false;  // есть ли грядка
    public bool isRaked = false; // обработана ли грядка
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
            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Pot)
            {

               

                if (ishavebed)
                {

                    Debug.Log("Тут уже занято, куда??");
                }
                else
                {
                    _itemSpawner.TestSpawnBed(selectedItem.itemData, transform.position, new Vector3(0.25f,0.25f,0.25f), gameObject.transform);
                    ishavebed = true;
                    InventoryManager.Instance.RemoveItem(selectedIndex);

                }

            }

            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && !isPlanted)
            {
                if (ishavebed) {
                    if (isRaked) {
                        if (!isPlanted) {
                            Transform parentSlot = transform.parent;

                            BedSlotController bedSlotController = parentSlot.GetComponent<BedSlotController>();
                            GridGenerator gridGenerator = parentSlot.GetComponent<GridGenerator>();



                            if (parentSlot != null)
                            {
                                float weightSeed = selectedItem.itemData.associatedPlantData.Weight;

                                // проверка веса растения 

                                switch (weightSeed)
                                {
                                    case 1:
                                        _itemSpawner.TestSpawnPlant(selectedItem.itemData, transform.position, new Vector3(0.5f, 0.5f, 0.5f),gameObject.transform);
                                        isPlanted = true;
                                        InventoryManager.Instance.RemoveItem(selectedIndex);
                                        break;

                                    case 2:

                                        if (gridGenerator != null)
                                        {

                                            bool isFreeSlot = gridGenerator.CheckFree2Slot(name);

                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.TestSpawnPlant(selectedItem.itemData, transform.position, new Vector3(0.5f, 0.5f, 0.5f),gameObject.transform.parent);
                                                InventoryManager.Instance.RemoveItem(selectedIndex);
                                            }
                                            else
                                            {
                                                Debug.Log("Не хватает грядок, надо купить еще");

                                            }

                                        }
                                        else
                                        {
                                            Debug.Log("Ошибка заполнения, отсутствует скрипт gridGenerator у родительского Slot");
                                        }
                                        break;
                                    case 4:

                                        if (gridGenerator != null)
                                        {

                                            bool isFreeSlot = gridGenerator.CheckSquareCells(name).Item1;
                                            Vector3 Plantposition = gridGenerator.CheckSquareCells(name).Item2;

                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.TestSpawnPlant(selectedItem.itemData, Plantposition, new Vector3(0.5f, 0.5f, 0.5f),gameObject.transform.parent);
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
                            Debug.Log("Тут уже занято, куда??");
                        }
                    }
                    else
                    {
                        Debug.Log("Сначала надо обработать грядку, а потом уже садить растение");

                    }

                }
                else
                {
                    Debug.Log("Сначала надо поставить грядку!");
                }

                
               
            }
         

            if(!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Tool)
            {
                if (ishavebed)
                {
                    if (selectedItem.itemData.itemName == "Rake")
                    {
                        GameObject childBed = FindChildWithTag("Bed");
                        if (childBed != null)
                        {
                            BedController bedController = childBed.GetComponent<BedController>();
                            if (bedController != null) {

                                bedController.ChangeStage(BedData.StageGrowthPlant.Raked, 1);
                                isRaked = true;
                            }
                            else
                            {
                                Debug.LogError("bedController не найден");
                            }
                        }
                        else
                        {
                            Debug.LogError("Грядка не является дочерней для слота, ошибка");
                        }
                    }
                    if(selectedItem.itemData.itemName == "Shovel")
                    {
                        if (isPlanted)
                        {
                            GameObject plant = FindChildWithTag("Plant");
                            Destroy(plant);
                            isPlanted = false;
                        }
                        else
                        {
                            Debug.Log("Здесь нет растения, нечего выкапывать ");
                        }
                    }
                }
                else
                {
                    Debug.Log("Обрабатывать можно только посаженные грядки!");
                }
            }

        }
        
    }

    private GameObject FindChildWithTag(string tag)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
            {
                return child.gameObject;
            }
        }
        Debug.LogWarning($"No child with tag {tag} found.");
        return null;
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
