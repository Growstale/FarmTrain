using UnityEngine;

public class ClosePanelButton : MonoBehaviour
{
    [Header("���������")]
    [Tooltip("������, ������� ����� ������� ��� ������� �� ��� ������.")]
    [SerializeField] private GameObject panelToClose;

    public void OnCloseButtonClick()
    {
        if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
        else
        {
            Debug.LogError("������: � ������� 'ClosePanelButton' �� ������� '" + gameObject.name + "' �� ��������� ������ ��� �������� (Panel To Close)!", this);
        }
    }
}