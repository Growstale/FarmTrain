using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotScripts : MonoBehaviour
{
    public bool isPlanted = false; // есть ли растение
    public bool ishavebed = false;  // есть ли грядка
    public bool isRaked = false; // обработана ли грядка
    public bool isFertilize = false; // есть ли удобрения
    Color currentColor;
    SpriteRenderer spriteRenderer;
    Transform slot;

    private InventoryManager inventoryManager; // Ссылка на менеджер инвентаря

    [SerializeField] ItemSpawner _itemSpawner;
    [SerializeField] Vector3 _sizeBed;
    [SerializeField] Vector3 _sizePlant;

    [SerializeField] ItemData _dataBed;
    public Vector2Int gridPosition;



    void Start()
    {
        inventoryManager = InventoryManager.Instance; // И поиск синглтона тоже

        if (_itemSpawner == null)
        {
            Debug.Log("itemSpaner not found!");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        currentColor = spriteRenderer.color;
        _sizeBed = new Vector3(1.7f,2.5f,2);
    }

    public void PlantSeeds()
    {
        // Получаем ВЫБРАННЫЙ предмет и ИНДЕКС выбранного слота
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

        if (selectedItem == null)
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
                    _itemSpawner.TestSpawnBed(selectedItem.itemData, transform.position, _sizeBed, gameObject.transform);
                    SFXManager.Instance.PlaySFX(SFXManager.Instance.placeBed);
                    ishavebed = true;
                    InventoryManager.Instance.RemoveItem(selectedIndex);
                }
            }

            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Seed && !isPlanted)
            {
                if (ishavebed)
                {
                    if (isRaked)
                    {
                        if (!isPlanted)
                        {
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
                                        var tourple1 = gridGenerator.CheckFreeSlot(name);
                                        bool isFreeSlot1 = tourple1.Item1;
                                        Vector3 pos1 = tourple1.Item2;
                                        Vector2Int[] idSlots1 = tourple1.Item3;
                                        if (isFreeSlot1)
                                        {
                                            _itemSpawner.SpawnPlant(selectedItem.itemData, pos1, _sizePlant, gameObject.transform.parent, idSlots1);
                                            AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                            SFXManager.Instance.PlaySFX(plantSound);
                                            isPlanted = true;
                                            InventoryManager.Instance.RemoveItem(selectedIndex);
                                        }
                                        break;

                                    case 2:
                                        if (gridGenerator != null)
                                        {
                                            var tourple = gridGenerator.CheckFree2Slot(name);
                                            bool isFreeSlot = tourple.Item1;
                                            Vector3 pos = tourple.Item2;
                                            Vector2Int[] idSlots = tourple.Item3;
                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.SpawnPlant(selectedItem.itemData, pos, _sizePlant, gameObject.transform.parent, idSlots);
                                                AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                                SFXManager.Instance.PlaySFX(plantSound);
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
                                            var tourple = gridGenerator.CheckSquareCells(name);
                                            bool isFreeSlot = tourple.Item1;
                                            Vector3 Plantposition = tourple.Item2;
                                            Vector2Int[] idSlots = tourple.Item3;
                                            if (isFreeSlot)
                                            {
                                                _itemSpawner.SpawnPlant(selectedItem.itemData, Plantposition, _sizePlant, gameObject.transform.parent, idSlots);
                                                AudioClip plantSound = selectedItem.itemData.associatedPlantData.plantingSound;
                                                SFXManager.Instance.PlaySFX(plantSound);
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

            if (!selectedItem.IsEmpty && selectedItem.itemData.itemType == ItemType.Tool)
            {
                if (ishavebed)
                {
                    if (selectedItem.itemData.itemName == "Rake")
                    {
                        GameObject childBed = FindChildWithTag("Bed");
                        if (childBed != null)
                        {
                            BedController bedController = childBed.GetComponent<BedController>();
                            if (bedController != null)
                            {
                                bedController.ChangeStage(BedData.StageGrowthPlant.Raked, 1);
                                isRaked = true;
                                SFXManager.Instance.PlaySFX(SFXManager.Instance.rake);
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
                    if (selectedItem.itemData.itemName == "Shovel" && ishavebed && !isPlanted)
                    {
                        GameObject childBed = FindChildWithTag("Bed");
                        if (childBed != null) {
                            Destroy(childBed);
                            ishavebed = false;
                            _itemSpawner.SpawnItem(_dataBed, new Vector3(transform.position.x - 0.5f,transform.position.y,1),new Vector3(1,2,1));
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
        //Debug.LogWarning($"No child with tag {tag} found.");
        return null;
    }

    public bool ChangeStateBed(BedData.StageGrowthPlant stage, int idx)
    {
        GameObject childBed = FindChildWithTag("Bed");
        if (childBed != null)
        {
            BedController bedController = childBed.GetComponent<BedController>();
            if (bedController != null)
            {
                bedController.ChangeStage(stage, idx);
                return true;
            }
            else
            {
                Debug.LogError("bedController не найден");
                return false;
            }

        }
        return false;
    }


    public SlotSaveData GetSaveData()
    {
        return new SlotSaveData
        {
            gridPosition = this.gridPosition,
            isPlanted = this.isPlanted,
            ishavebed = this.ishavebed,
            isRaked = this.isRaked,
            isFertilize = this.isFertilize
        };
    }
    public void ApplySaveData(SlotSaveData data)
    {

        GameObject childBed = FindChildWithTag("Bed");
        if (childBed != null) {
            UpdateSlotVisuals();
        }
        else
        {
            Debug.Log("[SlotScript] try to load data........");
            

            //Debug.Log("")

            // 1. Восстанавливаем логические переменные
            this.isPlanted = data.isPlanted;
            this.ishavebed = data.ishavebed;
            this.isRaked = data.isRaked;
            this.isFertilize = data.isFertilize;
            _itemSpawner.SpawnLoadedBed(transform.position, _sizeBed, transform,data.isRaked);
            // 2. Обновляем визуальное состояние слота в соответствии с данными
            // Важно! Без этого шага вы не увидите изменений после загрузки.
            UpdateSlotVisuals();
        }
            
    }

    /// <summary>
    /// Вспомогательный метод для обновления визуала грядки после загрузки.
    /// </summary>
    private void UpdateSlotVisuals()
    {
        // Если по данным сохранения грядки нет, а объект грядки есть, удаляем его
        GameObject childBed = FindChildWithTag("Bed");
        if (!ishavebed && childBed != null)
        {
            Destroy(childBed);
            return;
        }

      

    }


    public void DestoythisObject()
    {
        _itemSpawner.SpawnItem(_dataBed, transform.position, new Vector3(1, 2, 1));
        foreach (Transform child in transform) {

            if (child.CompareTag("Bed"))
            {
               Destroy(child.gameObject);
                isPlanted = false;
                ishavebed = false;
                isRaked = false;
                isFertilize = false;
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