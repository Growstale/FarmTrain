using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]

public class WorldItem : MonoBehaviour
{
    public ItemData itemData;
    private SpriteRenderer spriteRenderer;
    private Collider2D itemCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
        if (itemCollider == null)
        {
            Debug.LogError($"На объекте {gameObject.name} отсутствует Collider2D, хотя он требуется!", this);
        }
    }

    public void InitializeVisuals()
    {
        if (itemData != null)
        {
            spriteRenderer.sprite = itemData.itemIcon;
            gameObject.name = $"{itemData.itemName}"; 

            UpdateCollider();
        }
        else
        {
            Debug.LogWarning($"ItemData не назначен для WorldItem на объекте {gameObject.name}");
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (itemCollider != null) itemCollider.enabled = false;
        }
    }

    // Метод для обновления коллайдера под размер спрайта
    private void UpdateCollider()
    {
        if (itemCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"Не могу обновить коллайдер для {gameObject.name}: коллайдер или спрайт отсутствует.");
            return;
        }

        BoxCollider2D boxCollider = itemCollider as BoxCollider2D;
        if (boxCollider != null)
        {
            // Устанавливаем размер и смещение коллайдера равными границам спрайта
            boxCollider.size = spriteRenderer.sprite.bounds.size;
            boxCollider.offset = spriteRenderer.sprite.bounds.center;
        }
        else
        {
            // Если это не BoxCollider2D, добавить логику для других типов (мб потом)
            Debug.LogWarning($"Коллайдер на {gameObject.name} не является BoxCollider2D. Автоматическое изменение размера не поддерживается для типа {itemCollider.GetType()}.");
        }
    }

    public ItemData GetItemData()
    {
        return itemData;
    }
}