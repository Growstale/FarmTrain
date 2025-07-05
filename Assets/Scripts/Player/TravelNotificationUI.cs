using UnityEngine;
using System.Collections;

public class TravelNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private float displayDuration = 4.0f;

    private Coroutine displayCoroutine;

    private void Start()
    {
        if (ExperienceManager.Instance == null)
        {
            Debug.LogError("ExperienceManager не найден!");
            gameObject.SetActive(false);
            return;
        }

        // ѕодписываемс€ на событие разблокировки перехода
        ExperienceManager.Instance.OnPhaseUnlocked += ShowNotification;
        notificationPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked -= ShowNotification;
        }
    }

    private void ShowNotification(int level, GamePhase phase)
    {
        // ѕоказываем уведомление только при переходе с поезда на станцию
        if (phase == GamePhase.Train)
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            displayCoroutine = StartCoroutine(ShowAndHidePanel());
        }
    }

    private IEnumerator ShowAndHidePanel()
    {
        notificationPanel.SetActive(true);
        yield return new WaitForSeconds(displayDuration);
        notificationPanel.SetActive(false);
        displayCoroutine = null;
    }
}