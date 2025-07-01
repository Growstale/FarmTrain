using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class QuestNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject notificationIcon; // Сюда перетащим нашу иконку "!"

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager не найден! Уведомления о квестах не будут работать.");
            gameObject.SetActive(false);
            return;
        }

        // Подписываемся на общее обновление лога
        QuestManager.Instance.OnQuestLogUpdated += CheckForNewQuests;

        // Скрываем иконку при старте
        notificationIcon.SetActive(false);

        // Первая проверка на случай, если игра начинается с непросмотренным квестом
        CheckForNewQuests();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestLogUpdated -= CheckForNewQuests;
        }
    }

    private void CheckForNewQuests()
    {
        // Проверяем, есть ли ХОТЯ БЫ ОДИН активный квест, который еще не был просмотрен
        bool hasUnreadQuests = QuestManager.Instance.ActiveQuests.Any(quest => !quest.hasBeenViewed);

        notificationIcon.SetActive(hasUnreadQuests);
    }
}