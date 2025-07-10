using UnityEngine;
using UnityEngine.EventSystems;

// Этот скрипт можно повесить на любой UI-элемент с компонентом Graphic (Image, Button, Text),
// чтобы он мог отлавливать события наведения мыши.
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(3, 10)]
    public string tooltipMessage;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(tooltipMessage) && TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Show(tooltipMessage, eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
        {
            TooltipUI.Instance.Hide();
        }
    }

    public void SetTooltip(string message)
    {
        this.tooltipMessage = message;
    }
}