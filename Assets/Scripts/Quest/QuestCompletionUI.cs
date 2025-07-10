using UnityEngine;
using TMPro;
using System.Collections;

public class QuestCompletionUI : MonoBehaviour
{
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private float displayDuration = 4.0f; // Сколько секунд показывать окно

    [Header("Audio")]  
    [SerializeField] private AudioClip completionSound;
    private AudioSource audioSource;

    private Coroutine currentDisplayCoroutine;

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager не найден! Окно завершения квестов не будет работать.");
            gameObject.SetActive(false);
            return;
        }

        // Подписываемся на событие завершения квеста
        QuestManager.Instance.OnQuestCompleted += ShowCompletionPopup;

        audioSource = GetComponent<AudioSource>();

        completionPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= ShowCompletionPopup;
        }
    }

    private void ShowCompletionPopup(Quest completedQuest)
    {
        Debug.Log($"<color=cyan>[QuestCompletionUI]</color> Получен сигнал о завершении квеста '{completedQuest.title}'. Пытаюсь показать панель.");

        // Если предыдущее окно еще показывается, останавливаем его
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        // Заполняем тексты
        questTitleText.text = completedQuest.title;
        rewardText.text = $"+{completedQuest.rewardXP} XP";

        // Показываем панель и запускаем таймер на скрытие
        completionPanel.SetActive(true);

        if (audioSource != null && completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
        }

        currentDisplayCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        // Ждем указанное количество секунд
        yield return new WaitForSeconds(displayDuration);

        // Скрываем панель
        completionPanel.SetActive(false);
        currentDisplayCoroutine = null;
    }
}