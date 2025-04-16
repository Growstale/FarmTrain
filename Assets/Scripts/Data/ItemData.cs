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
}

// Перечисление для типов предметов
public enum ItemType
{
    General,
    Tool,
    Seed,
    Pot,
    AnimalProduct,
    Crop,
    Fertilizer // Добавим на будущее
}