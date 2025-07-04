using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(WorldItem))] 
public class ItemPickup : MonoBehaviour
{
    public int quantity = 1;

    [Header("Настройки подбора по клику")]
    [Tooltip("Тэг, который должен быть у ЭТОГО объекта, чтобы его можно было подобрать кликом.")]
    public string requiredTagForPickup = "CanBePickedUp";

    private WorldItem worldItem; 

    void Awake()
    {
        worldItem = GetComponent<WorldItem>();
        if (worldItem == null)
        {
            Debug.LogError($"На объекте {gameObject.name} отсутствует компонент WorldItem, необходимый для ItemPickup!", this);
        }
    }


    private void OnMouseDown()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        if (gameObject.CompareTag(requiredTagForPickup))
        {
            AttemptPickup();
        }
    }

    public void AttemptPickup()
    {
        if (worldItem == null)
        {
            Debug.LogError("Отсутствует компонент WorldItem, не могу получить данные предмета!", this);
            return;
        }

        ItemData dataToPickup = worldItem.GetItemData();

        if (dataToPickup == null)
        {
            Debug.LogError("Компонент WorldItem не содержит данных (itemData is null)!", this);
            return;
        }

        bool added = InventoryManager.Instance.AddItem(dataToPickup, quantity);

        if (added)
        {
            Debug.Log($"Подобран предмет: {dataToPickup.itemName} (тэг объекта: {gameObject.tag})");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"Не удалось подобрать {dataToPickup.itemName} - инвентарь полон?");
        }
    }
}