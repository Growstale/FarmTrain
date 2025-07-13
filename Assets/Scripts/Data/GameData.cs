// SaveDataStructures.cs
using UnityEngine;
using System.Collections.Generic;
using System; // Для DateTime

// --- Главный контейнер для всех данных игры ---
[System.Serializable]
public class GameData
{
    public DateTime lastSaved;
    public PlayerSaveData playerData;
    public List<InventoryItemSaveData> inventoryItems;
    public List<QuestSaveData> questData;
    public AchievementSaveData achievementData;
    public List<AnimalStateData> animalData; // Ваш класс уже подходит!
    public List<GridSaveData> gridsData; // Ваш класс уже подходит!
    public List<PlantSaveData> plantsData; // Ваш класс уже подходит!
    public TrainUpgradesSaveData trainUpgradesData;
}

// --- Данные игрока ---
[System.Serializable]
public class PlayerSaveData
{
    public int currentMoney;
    public int currentLevel;
    public GamePhase currentPhase;
    public int currentXP;
}

// --- Данные инвентаря ---
[System.Serializable]
public class InventoryItemSaveData
{
    public string itemName; // Сохраняем по имени, чтобы найти ItemData при загрузке
    public int quantity;
    public int slotIndex;
}

// --- Данные квестов ---
[System.Serializable]
public class QuestSaveData
{
    public string questId;
    public QuestStatus status;
    public bool isPinned;
    public bool hasBeenViewed;
    public List<QuestGoalSaveData> goals;
}

[System.Serializable]
public class QuestGoalSaveData
{
    public int currentAmount;
}

[Serializable]
public class AchievementSaveData
{
    public List<AchievementProgressEntry> progressList = new List<AchievementProgressEntry>();
    public List<TypeOfAchivment> completedList = new List<TypeOfAchivment>();
}

[Serializable]
public struct AchievementProgressEntry
{
    public TypeOfAchivment type;
    public int value;
}

// --- Данные улучшений поезда ---
[System.Serializable]
public class TrainUpgradesSaveData
{
    public List<string> purchasedUpgradeItemNames;
    public bool upgradeWatering; // Для PlantManager
}

// --- Метаданные для слотов в меню ---
[System.Serializable]
public class SaveSlotMetadata
{
    public bool isUsed;
    public DateTime saveTime;
    public int playerLevel;
    public string stationName; // Просто для красоты в UI
}

[System.Serializable]
public class AllSlotsMetadata
{
    public List<SaveSlotMetadata> slots = new List<SaveSlotMetadata>();
}