using UnityEngine;

public class BubbleYSorter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer iconRenderer;

    private Transform ownerTransform;

    private const int sortingOrderMultiplier = -100;

    private const int backgroundOrderOffset = 0;
    private const int iconOrderOffset = 1;

    void Awake()
    {
        if (backgroundRenderer == null)
        {
            Debug.LogError("Background Renderer не назначен в BubbleYSorter!", gameObject);
        }
        if (iconRenderer == null)
        {
            Debug.LogError("Icon Renderer не назначен в BubbleYSorter!", gameObject);
        }
    }

    public void SetOwner(Transform owner)
    {
        ownerTransform = owner;
        UpdateSortOrder();
    }

    void LateUpdate()
    {
        UpdateSortOrder();
    }

    void UpdateSortOrder()
    {
        if (ownerTransform == null || backgroundRenderer == null || iconRenderer == null)
        {
            return;
        }

        float ownerY = ownerTransform.position.y;

        int baseSortingOrder = Mathf.RoundToInt(ownerY * sortingOrderMultiplier);

        backgroundRenderer.sortingOrder = baseSortingOrder + backgroundOrderOffset;
        iconRenderer.sortingOrder = baseSortingiority + iconOrderOffset;
    }
}