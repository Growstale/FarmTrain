using UnityEngine;
using System.Collections.Generic;

// Перечисление для статуса квеста
public enum QuestStatus
{
    NotAccepted,
    Accepted,
    Completed
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    [Header("Info")]
    public string id; // Уникальный идентификатор квеста, например "01_FirstHarvest"
    public string title;
    [TextArea(4, 10)]
    public string description;
    [TextArea(4, 10)]
    public string shortDescription;

    [Header("Progression")]
    [Tooltip("К какому уровню игры относится квест (1, 2, 3...)")]
    public int gameLevel; // Заменяем stationId
    [Tooltip("К какой фазе относится квест (Поезд или Станция)")]
    public GamePhase phase; // Новое поле
    [Tooltip("Квест, который начнется после завершения этого. Оставьте пустым, если это последний в цепочке.")]
    public Quest nextQuest; // Какой квест открывается после этого

    [Header("Goals & Rewards")]
    public List<QuestGoal> goals;
    public int rewardXP; // Награда в опыте

    // Эти поля управляются QuestManager'ом
    [HideInInspector] public QuestStatus status = QuestStatus.NotAccepted;
    [HideInInspector] public bool isPinned = false; // Отслеживается ли квест
    [HideInInspector] public bool hasBeenViewed = false; // Просмотрел ли игрок квест в журнале
}