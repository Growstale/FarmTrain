using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementUIManagera : MonoBehaviour
{
    // --- ПОЛЯ ДЛЯ НАСТРОЙКИ В ИНСПЕКТОРЕ ---

    // Вместо отдельных массивов для слайдеров и текстов,
    // создадим класс, который объединяет все UI элементы для одного достижения.
    // Это гораздо надежнее, чем полагаться на индексы массивов.
    [System.Serializable]
    public class AchievementUIElements
    {
        // Укажите, какому типу достижения соответствует этот UI элемент
        public TypeOfAchivment achievementType;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public GameObject completedOverlay; // Необязательно: галочка или затемнение при выполнении
    }

    // В инспекторе вы создадите список и для каждого элемента укажете тип,
    // слайдер и текст. Порядок больше не важен!
    public List<AchievementUIElements> uiElementsList;


    // --- ЛОГИКА ---

    // Вызывается один раз, когда объект становится активным (например, при открытии окна)
    private void OnEnable()
    {
        // Не нужно подписываться на десятки событий.
        // Просто обновляем UI один раз, когда окно открывается.
        RefreshUI();
    }

    /// <summary>
    /// Основной метод, который обновляет все элементы UI на основе данных из AchievementManager.
    /// </summary>
    public void RefreshUI()
    {
        // Проверяем, что AchievementManager уже существует
        if (AchievementManager.instance == null)
        {
            Debug.LogError("AchievementManager не найден! UI не может быть обновлен.");
            return;
        }

        // Проходимся по нашему списку UI элементов
        foreach (var uiElement in uiElementsList)
        {
            // Находим данные (конфигурацию) для этого типа достижения
            AchievementData data = AchievementManager.instance.AllDataAchievement.Find(a => a.typeOfAchivment == uiElement.achievementType);
            if (data == null)
            {
                Debug.LogWarning($"Не найдены данные для достижения типа {uiElement.achievementType}");
                continue; // Пропускаем этот UI элемент
            }

            // --- Получаем АКТУАЛЬНЫЕ данные от AchievementManager ---
            int currentProgress = AchievementManager.instance.GetProgress(uiElement.achievementType);
            bool isCompleted = AchievementManager.instance.IsCompleted(uiElement.achievementType);

            // --- Обновляем соответствующие UI компоненты ---

            // Обновляем слайдер
            if (uiElement.progressBar != null)
            {
                uiElement.progressBar.maxValue = data.goal;
                uiElement.progressBar.value = currentProgress;
            }

            // Обновляем текст
            if (uiElement.progressText != null)
            {
                uiElement.progressText.text = $"{currentProgress} / {data.goal}";
            }

            // Показываем или скрываем "галочку" о выполнении
            if (uiElement.completedOverlay != null)
            {
                uiElement.completedOverlay.SetActive(isCompleted);
            }
        }
    }
}