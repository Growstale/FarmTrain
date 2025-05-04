using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite itemIcon = null;
    public ItemType itemType = ItemType.General;
    public bool isStackable = true;
    public int maxStackSize = 16;

    [TextArea(3, 5)]
    public string description = "";

    [Header("Farming Links (Optional)")]
    [Tooltip("Данные о растении, если этот предмет является семенем.")]
    public PlantData associatedPlantData; // Ссылка на данные растения

    [Tooltip("Данные о животном, если этот предмет используется для его размещения/создания.")]
    public AnimalData associatedAnimalData; // Ссылка на данные животного

    [Tooltip("Данные о грядке")]
    public BedData associatedBedData; // Ссылка на данные грядки

}

public enum ItemType
{
    General,
    Tool,
    Seed,
    Pot,
    AnimalProduct,
    PlantProduct,
    Fertilizer,
    Animal
}