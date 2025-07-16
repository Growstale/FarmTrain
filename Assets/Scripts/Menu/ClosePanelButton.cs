using UnityEngine;

public class ClosePanelButton : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Панель, которую нужно закрыть при нажатии на эту кнопку.")]
    [SerializeField] private GameObject panelToClose;

    public void OnCloseButtonClick()
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
        else
        {
            Debug.LogError("Ошибка: В скрипте 'ClosePanelButton' на объекте '" + gameObject.name + "' не назначена панель для закрытия (Panel To Close)!", this);
        }
    }
}