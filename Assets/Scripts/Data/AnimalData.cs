using UnityEngine;

[CreateAssetMenu(fileName = "New Animal Type", menuName = "Farming/Animal Type")]
public class AnimalData : ScriptableObject
{
    public string speciesName = "New Animal";
    public AudioClip[] animalSounds; // массив звуков

    public GameObject animalPrefab; // Префаб животного с его моделью/спрайтом и базовым скриптом

    [Tooltip("Ссылка на ItemData, представляющий это животное в магазинах и инвентаре")]
    public ItemData correspondingItemData;

    [Header("Appearance")]
    [Tooltip("Основной спрайт животного")]
    public Sprite defaultSprite;
    [Tooltip("Спрайт животного, когда продукт готов")]
    public Sprite productReadySprite;

    [Header("Needs")]
    public ItemData requiredFood; // Какой ItemData нужен для кормления
    public float feedInterval = 60.0f; // Как часто нужно кормить

    [Header("Production")]
    public ItemData productProduced; // Какой ItemData производит
    public float productionInterval = 120.0f; // Как часто производит продукт
    public int productAmount = 1; // Сколько продукта за раз
    [Tooltip("Инструмент, необходимый для сбора продукта")]
    public ItemData requiredToolForHarvest;

    [Header("Fertilizer")] // Удобрение
    public ItemData fertilizerProduced;
    public float fertilizerInterval = 180.0f;
    public int fertilizerAmount = 1; 

}