using UnityEngine;
using TMPro;
using System.Collections;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    private TooltipTrigger actionButtonTooltip;

    public GameObject tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public float hideDelay = 0.05f;
    private Coroutine hideCoroutine;
    private bool isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
            isInitialized = true;
        }
    }

    public void Show(string message, Vector2 screenPos)
    {
        if (!isInitialized || itemNameText == null) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        itemNameText.text = message;
        tooltipPanel.transform.position = screenPos + new Vector2(20, 50);
        tooltipPanel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (!isInitialized || !tooltipPanel.gameObject.activeSelf) return;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        tooltipPanel.gameObject.SetActive(false);
        hideCoroutine = null;
    }
}