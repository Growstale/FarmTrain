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

    public void Show(Sprite iconToShow)
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = iconToShow;
            gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Не могу показать облачко - iconRenderer не найден!", gameObject);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (iconRenderer != null)
        {
            iconRenderer.sprite = null;
        }
    }
}