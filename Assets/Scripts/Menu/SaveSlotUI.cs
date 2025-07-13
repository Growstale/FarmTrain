using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // ��� Action

public class SaveSlotUI : MonoBehaviour
{
    // ���������� ���� �������� ������� �� ������ ������� � ����������
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    // �����, ������� ����� �������� MenuUIController
    public void Setup(SaveSlotMetadata slotInfo, int slotId, bool isNewGameMode, Action<int> onSlotClickedCallback)
    {
        if (slotInfo.isUsed)
        {
            // ���� �����
            infoText.text = $"���� {slotId + 1}\n{slotInfo.saveTime:dd.MM.yyyy HH:mm}";
            buttonText.text = isNewGameMode ? "������������" : "���������";
            actionButton.interactable = true;
        }
        else
        {
            // ���� ����
            infoText.text = $"���� {slotId + 1}\n�����";
            buttonText.text = isNewGameMode ? "�������" : "�����";
            actionButton.interactable = isNewGameMode;
        }

        // ��������� �������� �� ����
        actionButton.onClick.RemoveAllListeners();
        // ���� ������ ������ ���� �������, ����������� � ��� ����� OnSlotClicked �� MenuUIController
        if (actionButton.interactable)
        {
            actionButton.onClick.AddListener(() => onSlotClickedCallback(slotId));
        }
    }
}