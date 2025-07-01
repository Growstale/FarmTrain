using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestLogEntryUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Button pinButton; // Наша кнопка-булавка
    [SerializeField] private Image pinIcon; // Иконка на этой кнопке
    [SerializeField] private Button mainButton; // Основная кнопка, по которой кликаем

    [Header("Pin Sprites")]
    [SerializeField] private Sprite pinnedSprite; // Спрайт закрепленной булавки
    [SerializeField] private Sprite unpinnedSprite; // Спрайт обычной булавки

    private Quest assignedQuest;
    private Action<Quest> onSelectCallback;

    public void Setup(Quest quest, Action<Quest> selectCallback)
    {
        assignedQuest = quest;
        onSelectCallback = selectCallback;

        titleText.text = quest.title;

        // Настраиваем видимость элементов в зависимости от статуса квеста
        bool isCompleted = quest.status == QuestStatus.Completed;
        checkmarkImage.gameObject.SetActive(isCompleted);
        pinButton.gameObject.SetActive(!isCompleted); // Булавку показываем только для активных

        if (!isCompleted)
        {
            pinIcon.sprite = quest.isPinned ? pinnedSprite : unpinnedSprite;
        }

        // Привязываем слушателей событий
        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(HandleSelection);

        pinButton.onClick.RemoveAllListeners();
        pinButton.onClick.AddListener(HandlePinning);
    }

    // Клик по основной части строки - выбрать квест для показа деталей
    private void HandleSelection()
    {
        onSelectCallback?.Invoke(assignedQuest);
    }

    // Клик по булавке - закрепить/открепить
    private void HandlePinning()
    {
        QuestManager.Instance.PinQuest(assignedQuest);
    }
}