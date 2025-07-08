// PenRelatedData.cs
using UnityEngine;
using System.Collections.Generic;

// Данные для одного конкретного уровня улучшения загона
[System.Serializable]
public class PenLevelData
{
    [Tooltip("Визуальное представление загона на этом уровне (сломанный, обычный, улучшенный и т.д.).")]
    public Sprite penSprite;
    [Tooltip("Максимальная вместимость на этом уровне.")]
    public int capacity;
    [Tooltip("Предмет-улучшение, который нужно купить в магазине для перехода на ЭТОТ уровень.")]
    public ItemData requiredUpgradeItem; // null для стартового уровня
    [Tooltip("Дает ли этот уровень улучшения автоматическое кормление?")]
    public bool providesAutoFeeding;
    public AudioClip upgradeApplySound;

}

// Конфигурация для одного типа животных (например, для куриц)
// Хранится в AnimalPenManager
[System.Serializable]
public class PenConfigData
{
    [Tooltip("Для какого типа животных этот загон.")]
    public AnimalData animalData;

    // Имена объектов на сцене для поиска
    [Tooltip("Имя объекта SpriteRenderer, который отображает сам загон.")]
    public string penSpriteRendererName;
    [Tooltip("Имя объекта, куда будут спавниться животные.")]
    public string animalParentName;
    [Tooltip("Имя коллайдера, который ограничивает передвижение животных.")]
    public string placementAreaName;

    [Tooltip("Список всех возможных уровней улучшения для этого загона.")]
    public List<PenLevelData> upgradeLevels;
}

// "Живые" данные для TrainPenController
public class PenRuntimeInfo
{
    public PenConfigData config;
    public SpriteRenderer penSpriteRenderer; // <<< Теперь храним SpriteRenderer, а не Transform
    public Transform animalParent;
    public Collider2D placementArea;

    [HideInInspector] public int currentLevel = 0; // Текущий уровень улучшения (индекс в списке upgradeLevels)
}