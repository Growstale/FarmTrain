using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Plant", menuName = "Farming/Plant")]
public class PlantData : ScriptableObject
{
    public string plantName = "New Plant";

    [Header("Growth")]
    public List<Sprite> growthStagesSprites; // Список спрайтов для каждого этапа роста 
    public float timePerGrowthStage = 10.0f; // Время в секундах на каждый этап роста
    public float waterNeededInterval = 5.0f; // Как часто нужно поливать 
    public float fertilizerGrowthMultiplier = 1.5f; // На сколько ускоряется рост с удобрением 
    public float Weight = 1.0f; // Вес растения, или сколько грядок занимает растение

    [Header("Harvest")]
    public ItemData harvestedCrop; // Какой предмет (плод) получаем при сборе
    public ItemData seedItem; // Какой предмет (семечко) может выпасть при сборе
    [Range(0f, 1f)] 
    public float seedDropChance = 0.8f; // Вероятность выпадения семечка
}