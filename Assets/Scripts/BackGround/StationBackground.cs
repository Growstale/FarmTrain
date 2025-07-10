// StationBackgroundManager.cs

using UnityEngine;
using System.Collections.Generic;

// Убеждаемся, что на объекте есть SpriteRenderer
[RequireComponent(typeof(SpriteRenderer))]
public class StationBackgroundManager : MonoBehaviour
{
    [Tooltip("Список спрайтов для фона каждой станции. Element 0 - для Станции 1, Element 1 - для Станции 2 и т.д.")]
    [SerializeField] private List<Sprite> stationSprites;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Получаем ссылку на компонент SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("На объекте фона станции отсутствует компонент SpriteRenderer!", gameObject);
            enabled = false; // Отключаем скрипт, чтобы избежать ошибок
        }
    }

    void Start()
    {
        // Проверяем, что ExperienceManager доступен
        if (ExperienceManager.Instance == null)
        {
            Debug.LogError("ExperienceManager не найден! Фон станции не может определить свой уровень.", gameObject);
            return;
        }

        // Получаем текущий уровень игры (который соответствует ID станции)
        int currentLevel = ExperienceManager.Instance.CurrentLevel;

        // Устанавливаем соответствующий фон
        SetBackgroundForLevel(currentLevel);
    }

    /// <summary>
    /// Устанавливает спрайт фона в соответствии с указанным уровнем.
    /// </summary>
    /// <param name="level">Уровень станции (1, 2, 3...)</param>
    public void SetBackgroundForLevel(int level)
    {
        // Уровень 1 соответствует индексу 0 в списке, уровень 2 -> индексу 1, и т.д.
        int spriteIndex = level - 1;

        if (stationSprites == null || stationSprites.Count == 0)
        {
            Debug.LogWarning($"У фона станции ({gameObject.name}) не назначен список спрайтов в инспекторе.", gameObject);
            return;
        }

        // Проверяем, что для данного уровня есть спрайт в списке
        if (spriteIndex >= 0 && spriteIndex < stationSprites.Count)
        {
            // Устанавливаем нужный спрайт
            spriteRenderer.sprite = stationSprites[spriteIndex];
            Debug.Log($"Фон станции изменен на спрайт '{stationSprites[spriteIndex].name}' для уровня {level}.");
        }
        else
        {
            Debug.LogError($"Для фона станции ({gameObject.name}) не найден спрайт для уровня {level} (требуемый индекс: {spriteIndex}). Проверьте список stationSprites.", gameObject);
        }
    }
}