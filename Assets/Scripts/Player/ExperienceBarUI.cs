using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperienceBarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private GameObject experiencePanel;

    private void Start()
    {
        // Проверяем, что ExperienceManager существует
        if (ExperienceManager.Instance == null)
        {
            Debug.LogError("ExperienceManager не найден! UI опыта не будет работать.");
            gameObject.SetActive(false);
            return;
        }

        // Подписываемся на событие изменения опыта
        ExperienceManager.Instance.OnXPChanged += UpdateXPBar;

        // Обновляем полосу при старте, чтобы показать начальные значения
        UpdateXPBar(ExperienceManager.Instance.CurrentXP, ExperienceManager.Instance.XpForNextPhase);
    }

    private void OnDestroy()
    {
        // Обязательно отписываемся, чтобы избежать ошибок
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnXPChanged -= UpdateXPBar;
        }
    }

    private void UpdateXPBar(int currentXP, int xpForNextPhase)
    {
        if (xpForNextPhase > 0)
        {
            experiencePanel.SetActive(true);
            xpText.text = $"{currentXP} / {xpForNextPhase}";
        }
        else
        {
            experiencePanel.SetActive(false);

        }

    }
}