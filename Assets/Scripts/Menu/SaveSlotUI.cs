using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Для Action

public class SaveSlotUI : MonoBehaviour
{
    // Перетащите сюда дочерние объекты из вашего префаба в инспекторе
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    // Метод, который будет вызывать MenuUIController
    public void Setup(SaveSlotMetadata slotInfo, int slotId, bool isNewGameMode, Action<int> onSlotClickedCallback)
    {
        if (slotInfo.isUsed)
        {
            // Слот ЗАНЯТ
            infoText.text = $"Слот {slotId + 1}\n{slotInfo.saveTime:dd.MM.yyyy HH:mm}";
            buttonText.text = isNewGameMode ? "Перезаписать" : "Загрузить";
            actionButton.interactable = true;
        }
        else
        {
            // Слот ПУСТ
            infoText.text = $"Слот {slotId + 1}\nПусто";
            buttonText.text = isNewGameMode ? "Создать" : "Пусто";
            actionButton.interactable = isNewGameMode;
        }

        // Назначаем действие на клик
        actionButton.onClick.RemoveAllListeners();
        // Если кнопка должна быть активна, привязываем к ней метод OnSlotClicked из MenuUIController
        if (actionButton.interactable)
        {
            actionButton.onClick.AddListener(() => onSlotClickedCallback(slotId));
        }
    }
}