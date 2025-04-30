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
        Hide(); // Скрываем по умолчанию
    }

    // Показывает облачко с указанной иконкой
    public void Show(Sprite iconToShow)
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = iconToShow;
            gameObject.SetActive(true); // Делаем весь объект видимым
        }
        else
        {
            Debug.LogError("Не могу показать облачко - iconRenderer не найден!", gameObject);
        }
    }

    // Скрывает облачко
    public void Hide()
    {
        gameObject.SetActive(false); // Делаем весь объект невидимым
        if (iconRenderer != null)
        {
            iconRenderer.sprite = null; // Убираем иконку на всякий случай
        }
    }
}