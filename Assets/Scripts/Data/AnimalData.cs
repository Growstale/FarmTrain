using UnityEngine;

[CreateAssetMenu(fileName = "New Animal Type", menuName = "Farming/Animal Type")]
public class AnimalData : ScriptableObject
{
    public string speciesName = "New Animal";
    public Sprite icon;
    // public GameObject prefab; // Префаб животного с его моделью/спрайтом и базовым скриптом

    [Header("Needs")]
    public ItemData requiredFood; // Какой ItemSO нужен для кормления
    public float feedInterval = 60.0f; // Как часто нужно кормить

    [Header("Production")]
    public ItemData productProduced; // Какой ItemSO производит
    public float productionInterval = 120.0f; // Как часто производит продукт
    public int productAmount = 1; // Сколько продукта за раз
}