using UnityEngine;
using TMPro;
using System.Collections;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public float hideDelay = 0.05f;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        tooltipPanel.SetActive(false);
    }

    public void Show(string message, Vector2 screenPos)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        itemNameText.text = message;
        tooltipPanel.transform.position = screenPos + new Vector2(20, 50);
        tooltipPanel.SetActive(true);
    }

    public void Hide()
    {
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        tooltipPanel.SetActive(false);
        hideCoroutine = null;
    }
}