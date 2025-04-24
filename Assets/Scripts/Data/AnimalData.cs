using UnityEngine;

[CreateAssetMenu(fileName = "New Animal Type", menuName = "Farming/Animal Type")]
public class AnimalData : ScriptableObject
{
    public string speciesName = "New Animal";
    public GameObject animalPrefab; // Префаб животного с его моделью/спрайтом и базовым скриптом

    [Header("Needs")]
    public ItemData requiredFood; // Какой ItemData нужен для кормления
    public float feedInterval = 60.0f; // Как часто нужно кормить

    [Header("Production")]
    public ItemData productProduced; // Какой ItemData производит
    public float productionInterval = 120.0f; // Как часто производит продукт
    public int productAmount = 1; // Сколько продукта за раз

    [Header("Fertilizer")] // Удобрение
    public ItemData fertilizerProduced;
    public float fertilizerInterval = 180.0f;
    public int fertilizerAmount = 1; 

}