using UnityEngine;

public class ThoughtBubbleController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer iconRenderer;

    void Awake()
    {
        if (iconRenderer == null)
        {
            Debug.LogError("Icon Renderer не найден или не назначен в ThoughtBubbleController!", gameObject);
        }
        Hide();
    }

    public void Show(ItemData itemToShow)
    {
        if (iconRenderer == null || itemToShow == null)
        {
            Debug.LogError("Не могу показать облачко - iconRenderer или itemToShow не найден!", gameObject);
            gameObject.SetActive(false);
            return;
        }

        if (itemToShow.itemName == "Wool")
        {
            // Уменьшаем масштаб ТОЛЬКО иконки
            iconRenderer.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        else
        {
            // Возвращаем стандартный масштаб для всех остальных иконок
            iconRenderer.transform.localScale = Vector3.one;
        }

        // Устанавливаем спрайт и включаем объект
        iconRenderer.sprite = itemToShow.itemIcon;
        gameObject.SetActive(true);
    }



    public void Hide()
    {
        gameObject.SetActive(false);
        if (iconRenderer != null)
        {
            // Сбрасываем масштаб на случай, если облачко скрыли, пока там была шерсть
            iconRenderer.transform.localScale = Vector3.one;
            iconRenderer.sprite = null;
        }
    }
}