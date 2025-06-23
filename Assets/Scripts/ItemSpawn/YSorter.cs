using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private const int sortingOrderMultiplier = -100;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer == null) return;

        float sortY;

        sortY = transform.position.y;

        spriteRenderer.sortingOrder = Mathf.RoundToInt(sortY * sortingOrderMultiplier);
    }
}