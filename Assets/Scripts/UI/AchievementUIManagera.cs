using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
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
        public GameObject entryContainer;
        public Sprite defaultSprite; 
        public Sprite completedSprite;
    }

    // В инспекторе вы создадите список и для каждого элемента укажете тип,
    // слайдер и текст. Порядок больше не важен!
    public List<AchievementUIElements> uiElementsList;


    // --- ЛОГИКА ---

    // Вызывается один раз, когда объект становится активным (например, при открытии окна)
    private void OnEnable()
    {

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
                uiElement.progressText.gameObject.SetActive(!isCompleted);
                uiElement.completedOverlay.SetActive(isCompleted);
            }

            Image entryImage = uiElement.entryContainer.GetComponent<Image>();
            if (entryImage != null)
            {
                entryImage.sprite = isCompleted ? uiElement.completedSprite : uiElement.defaultSprite;
            }
        }
        SortUIEntries();
    }
    private void SortUIEntries()
    {
        if (AchievementManager.instance == null) return;

        // Используем LINQ для сортировки нашего списка uiElementsList.
        // OrderBy сортирует коллекцию по указанному ключу.
        // В нашем случае ключ - это булево значение (true/false) от IsCompleted().
        // По умолчанию false (не выполнено) идет раньше, чем true (выполнено),
        // что нам и нужно: невыполненные окажутся в начале списка.
        var sortedList = uiElementsList
            .OrderBy(ui => AchievementManager.instance.IsCompleted(ui.achievementType))
            .ToList();

        // Теперь, когда у нас есть отсортированный C# список,
        // мы должны применить этот порядок к объектам в иерархии Unity.
        foreach (var uiElement in sortedList)
        {
            // Метод SetAsLastSibling() перемещает Transform этого объекта в конец списка
            // дочерних объектов его родителя.
            // Проходя по нашему отсортированному списку и вызывая этот метод для каждого,
            // мы эффективно выстраиваем их в нужном порядке в иерархии.
            uiElement.entryContainer.transform.SetAsLastSibling();
        }
    }
}