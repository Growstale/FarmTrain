using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.Overlays;
public class PlantController : MonoBehaviour
{


    [Header("Growth")]
    // Ссылка на data растения
    public  PlantData plantData;
    [SerializeField] GameObject icon_water;
    [SerializeField] GameObject worldItemPrefab;

    private InventoryManager inventoryManager;

    public bool upgradeWatering;

    //
    SpriteRenderer _spriteRenderer;
    PlantData.StageGrowthPlant Stageplant;

    public Vector2Int[] IdSlots;

    public int currentStage;

    float timePerGrowthStage = 0.0f;
    float timeWaterNeed = 0.0f;

    public bool isNeedWater = false;
    public bool isFertilize = false;

    // таймер для воды 
    public float growthTimer = 0.0f;
    public float waterNeedTimer = 0.0f;


    public bool isInitialized = false;
    private void Awake()
    {
        
        _spriteRenderer = GetComponent<SpriteRenderer>();
        inventoryManager = InventoryManager.Instance; // И поиск синглтона тоже
    }

    void Start()
    {

        if (isInitialized) return;


        //if (!isInitialized)
        //{
        //    if (plantData != null)
        //    {
        //        Stageplant = PlantData.StageGrowthPlant.defaultStage;
        //        currentStage = 0;
        //        timePerGrowthStage = plantData.timePerGrowthStage;
        //        timeWaterNeed = plantData.waterNeededInterval;
        //        _spriteRenderer.sprite = plantData.growthStagesSprites[0];

        //        //if(!upgradeWatering) InvokeRepeating("StartWaterNeededInterval", 0f, timeWaterNeed);
        //        Debug.Log($"Spawning plant {plantData.plantName}");


        //    }
        //    else
        //    {
        //        Debug.LogError("Отсутствует ссылка на данные растения");
        //        Destroy(gameObject);
        //    }
        //}
        //CheckForAchievement(plantData.plantName);
        //float countSlot = plantData.Weight;
        //upgradeWatering = PlantManager.instance.UpgradeWatering;
        Initialize(null);
    }

    public void Initialize(PlantSaveData savedata)
    {
        if (isInitialized) return;

        if (plantData == null)
        {
            Debug.LogError("Ошибка инициализации: PlantData не установлен. Растение будет уничтожено.", this);
            Destroy(gameObject);
            return;
        }

        // --- Загрузка из сохранения или установка по умолчанию ---
        if (savedata != null) // Если есть данные сохранения
        {
            IdSlots = savedata.idSlots;
            currentStage = savedata.currentStage;
            isFertilize = savedata.isFertilize;
            isNeedWater = savedata.isNeedWater;
            growthTimer = savedata.growthTimer;
            waterNeedTimer = savedata.waterNeedTimer;
            Debug.Log($"Растение '{plantData.plantName}' загружено со стадии {currentStage}");
        }
        else // Если это новое растение
        {
            currentStage = 0;
            // Все остальные поля (isNeedWater, growthTimer и т.д.) уже 0/false по умолчанию
            CheckForAchievement(plantData.plantName);
            Debug.Log($"Spawning new plant {plantData.plantName}");
        }

        // --- Общая настройка, которая нужна в обоих случаях ---
        upgradeWatering = PlantManager.instance.UpgradeWatering;
        timePerGrowthStage = plantData.timePerGrowthStage;
        timeWaterNeed = plantData.waterNeededInterval;

        // Применяем эффект удобрения, если он был
        if (isFertilize)
        {
            timePerGrowthStage /= plantData.fertilizerGrowthMultiplier;
        }

        UpdateStageAndVisuals(); // Обновляем спрайт и состояние

        isInitialized = true;
    }
    private void UpdateStageAndVisuals()
    {
        // Проверка на выход за пределы массива
        if (currentStage >= plantData.growthStagesSprites.Count)
        {
            Debug.LogWarning($"currentStage ({currentStage}) выходит за пределы массива спрайтов для {plantData.name}. Устанавливаем последнюю стадию.");
            currentStage = plantData.growthStagesSprites.Count - 1;
        }

        Stageplant = (PlantData.StageGrowthPlant)currentStage;
        _spriteRenderer.sprite = plantData.growthStagesSprites[currentStage];

        // Логика иконки воды
        Transform icon = transform.Find("icon_water");
        if (isNeedWater && Stageplant != PlantData.StageGrowthPlant.FourthStage)
        {
            if (icon == null) TriggerNeedWater(false); // false - чтобы не дублировать лог
        }
        else
        {
            if (icon != null) Destroy(icon.gameObject);
        }
    }
    void Update()
    {
        if (!isInitialized || Stageplant == PlantData.StageGrowthPlant.FourthStage) return;

        if (!isNeedWater)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= timePerGrowthStage)
            {
                AdvancePlantGrowth();
                growthTimer = 0f;
            }
            if (!upgradeWatering)
            {
                waterNeedTimer += Time.deltaTime;
                if (waterNeedTimer >= timeWaterNeed)
                {
                    TriggerNeedWater(true);
                }
            }
        }
    }

    void AdvancePlantGrowth()
    {
        if (Stageplant == PlantData.StageGrowthPlant.FourthStage) return;

        currentStage++;
        UpdateStageAndVisuals();
    }

    void TriggerNeedWater(bool logMessage)
    {
        isNeedWater = true;

        GameObject iconWater = Instantiate(icon_water, new Vector3(transform.position.x + 0.3f, transform.position.y + 0.4f, transform.position.z), Quaternion.identity);
        iconWater.transform.parent = transform;
        iconWater.name = "icon_water";
        if (logMessage) Debug.Log($"Plant {name} need Water!");
    }

    void CheckForAchievement(string namePlant)
    {
        if (AchievementManager.allTpyesPlant.Contains(namePlant))
        {
            Debug.Log($"Type plant {namePlant} planted");
            if (AchievementManager.allTpyesPlant.Remove(namePlant))
                GameEvents.TriggerOnCollectAllPlants(1);
            else
            {
                Debug.LogWarning("This type of plant is undefind");
            }
        }
    }

    void WateringPlants()
    {
        isNeedWater = false;
        waterNeedTimer = 0f;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.gameObject.name == "icon_water")
            {
                Destroy(child.gameObject);
                Debug.Log("Вы успешно полили растение!");
                if (SFXManager.Instance != null && SFXManager.Instance.wateringSound != null)
                {
                    SFXManager.Instance.PlaySFX(SFXManager.Instance.wateringSound);
                }
                break;
            }

        }
    }

    public void FillVectorInts(Vector2Int[] Posarray)
    {
        IdSlots = Posarray;
        //foreach (var slot in IdSlots)
        //{
        //    Debug.Log($"<<< Current IdSlot for Plant: {slot}");
        //}
    }

    private void FertilizePlant(Vector2Int[] idSlots)
    {
        float fertilizerGrowthMultiplie = plantData.fertilizerGrowthMultiplier;
        GameObject parent = transform.parent.gameObject;

        if (parent != null)
        {
            GridGenerator generator = parent.GetComponent<GridGenerator>();
            if (generator != null)
            {
                generator.FertilizerSlot(idSlots);
                timePerGrowthStage /= fertilizerGrowthMultiplie;
                isFertilize = true;
            }
            else
            {
                Debug.Log($"Для parent {parent.name} generator is null");
            }
        }
        else
        {
            Debug.Log($"Для растения {name} Parent is null");
        }
    }

    public void ClickHandler()
    {
        InventoryItem selectedItem = inventoryManager.GetSelectedItem();
        int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

        if ((selectedItem == null || (selectedItem.itemData.itemType != ItemType.Tool || selectedItem.itemData.itemType != ItemType.Fertilizer)) && Stageplant == PlantData.StageGrowthPlant.FourthStage)
        {

            GameObject parent = transform.parent.gameObject;

            if (parent != null)
            {
                GridGenerator gridGenerator = parent.GetComponent<GridGenerator>();
                if (gridGenerator != null)
                {
                    if (gridGenerator.FreeSlot(IdSlots))
                    {
                        if (SFXManager.Instance != null && SFXManager.Instance.shovelSound != null)
                        {
                            SFXManager.Instance.PlaySFX(SFXManager.Instance.shovelSound);
                        }

                        if (TryGetSeeds(plantData.seedDropChance))
                        {
                            GameObject seed = GetHarvest(transform.position, plantData.seedItem);
                        }

                        GameObject harvestedCrop = GetHarvest(transform.position, plantData.harvestedCrop);
                        if (harvestedCrop != null)
                        {
                            Debug.Log("Урожай собран!");
                        }
                        else
                        {
                            Debug.Log("Урожай не собран!");
                        }
                        Destroy(gameObject);
                    }
                    else
                    {
                        Debug.Log("Ошибка удаления растения");
                    }
                }
                else
                {
                    Debug.Log($"У {gameObject.name} нет родителя GridGenerator и контроллера gridGenerator");
                }
            }
            else
            {
                Debug.Log($"У {gameObject.name} нет родителя GridGenerator");
            }
        }
        else if (selectedItem == null)
        {
            Debug.Log("Выбери предмет из инвенторя");
        }
        else
        {
            if (!selectedItem.IsEmpty)
            {
                if (selectedItem.itemData.itemType == ItemType.Tool)
                {
                    if (selectedItem.itemData.itemName == "Shovel")
                    {
                        GameObject parent = transform.parent.gameObject;
                        if (parent != null)
                        {
                            GridGenerator gridGenerator = parent.GetComponent<GridGenerator>();
                            if (gridGenerator != null)
                            {
                                if (gridGenerator.FreeSlot(IdSlots))
                                    Destroy(gameObject);
                                else
                                {
                                    Debug.Log("Ошибка удаления растения");
                                }
                            }
                            else
                            {
                                Debug.Log($"У {gameObject.name} нет родителя GridGenerator и контроллера gridGenerator");
                            }
                        }
                        else
                        {
                            Debug.Log($"У {gameObject.name} нет родителя GridGenerator");
                        }
                    }
                    if (selectedItem.itemData.itemName == "watering_can")
                    {
                        if (isNeedWater)
                        {
                            WateringPlants();
                        }
                        else
                        {
                            Debug.Log($"У {gameObject.name} нет нужды в поливке");
                        }
                    }
                }
                else if (selectedItem.itemData.itemType == ItemType.Fertilizer)
                {
                    if (!isFertilize)
                    {
                        ItemData usedFertilizer = selectedItem.itemData;

                        FertilizePlant(IdSlots);
                        InventoryManager.Instance.RemoveItem(selectedIndex);

                        if (QuestManager.Instance != null)
                        {
                            QuestManager.Instance.AddQuestProgress(GoalType.Use, usedFertilizer.name, 1);
                            Debug.Log($"[Quest Event] Отправлен прогресс для Use: {usedFertilizer.name}");
                        }

                    }
                    else
                    {
                        Debug.Log("Растение уже удобрено!");
                    }
                }
            }
        }
    }

    // получить урожай (дублирование кода, но что поделать)
    public GameObject GetHarvest(Vector3 spawnPosition, ItemData itemTospawn)
    {
        float randomValue = UnityEngine.Random.Range(-0.25f, 0.25f);
        Vector3 spawnScale = Vector3.one;
        ItemData dataToSpawn = itemTospawn;
        if (worldItemPrefab == null)
        {
            Debug.LogError($"World Item Prefab не назначен в ItemSpawner! Невозможно заспавнить предмет '{dataToSpawn.itemName}'.");
            return null;
        }

        GameObject newItemObject = Instantiate(worldItemPrefab, new Vector3(spawnPosition.x + randomValue, spawnPosition.y + randomValue, spawnPosition.z), Quaternion.identity);

        newItemObject.transform.localScale = spawnScale;
        newItemObject.transform.parent = transform.parent;

        WorldItem worldItemComponent = newItemObject.GetComponent<WorldItem>();
        if (worldItemComponent != null)
        {
            worldItemComponent.itemData = dataToSpawn;
            worldItemComponent.InitializeVisuals();
            Debug.Log($"Заспавнен ПРЕДМЕТ: {dataToSpawn.itemName} в позиции {spawnPosition}");
            return newItemObject;
        }
        else
        {
            Debug.LogError($"На префабе '{worldItemPrefab.name}' отсутствует компонент WorldItem! Предмет '{dataToSpawn.itemName}' уничтожен.");
            Destroy(newItemObject);
            return null;
        }
    }



    private bool TryGetSeeds(double successRate)
    {
        if (successRate < 0 || successRate > 1)
        {
            throw new ArgumentException("Success rate must be between 0 and 1");
        }
        System.Random random = new System.Random();
        return random.NextDouble() < successRate;
    }

    public PlantSaveData GetSaveData()
    {
        GameObject parent = transform.parent.gameObject;
        if (parent == null) return null;

        return new PlantSaveData()
        {
            plantDataName = this.plantData.name, // Сохраняем имя ScriptableObject, а не display name
            gridIdentifier = parent.name, // Берем идентификатор напрямую из GridGenerator
            idSlots = this.IdSlots,
            currentStage = this.currentStage,
            growthTimer = this.growthTimer,
            waterNeedTimer = this.waterNeedTimer,
            isNeedWater = this.isNeedWater,
            isFertilize = this.isFertilize,
            currentposition = this.transform.position,
        };
    }


}