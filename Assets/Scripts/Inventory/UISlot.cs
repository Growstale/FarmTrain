using UnityEngine;
using UnityEngine.UI; 
using TMPro;          

public class UISlot : MonoBehaviour
{
    [SerializeField] private Image slotBackground; 
    [SerializeField] private Image itemIcon;       // Иконка предмета
    [SerializeField] private TextMeshProUGUI quantityText; // Текст количества

    public void UpdateSlot(InventorySlot slotData)
    {
        if (slotData != null && slotData.itemData != null && slotData.quantity > 0)
        {
            itemIcon.enabled = true; 
            itemIcon.sprite = slotData.itemData.itemIcon; 

            // Показываем количество, если предмет стакается
            if (slotData.itemData.isStackable)
            {
                quantityText.enabled = true; 
                quantityText.text = slotData.quantity.ToString(); 
            }
            else
            {
                quantityText.enabled = false; 
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        itemIcon.enabled = false;    
        itemIcon.sprite = null;      
        quantityText.enabled = false;
        quantityText.text = "";      
    }

    public void Highlight(bool showHighlight)
    {
        if (slotBackground != null)
        {
            slotBackground.color = showHighlight ? Color.yellow : Color.white; 
        }
    }

    void Awake()
    {
        if (itemIcon == null)
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>(); 
        if (quantityText == null && itemIcon != null)
            quantityText = itemIcon.transform.Find("QuantityText")?.GetComponent<TextMeshProUGUI>();
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        ClearSlot();
    }
}