// SaveDataStructures.cs
using UnityEngine;
using System.Collections.Generic;
using System; // ��� DateTime

// --- ������� ��������� ��� ���� ������ ���� ---
[System.Serializable]
public class GameData
{
    public DateTime lastSaved;
    public PlayerSaveData playerData;
    public List<InventoryItemSaveData> inventoryItems;
    public List<QuestSaveData> questData;
    public AchievementSaveData achievementData;
    public List<AnimalStateData> animalData; // ��� ����� ��� ��������!
    public List<GridSaveData> gridsData; // ��� ����� ��� ��������!
    public List<PlantSaveData> plantsData; // ��� ����� ��� ��������!
    public TrainUpgradesSaveData trainUpgradesData;
}

// --- ������ ������ ---
[System.Serializable]
public class PlayerSaveData
{
    public int currentMoney;
    public int currentLevel;
    public GamePhase currentPhase;
    public int currentXP;
}

// --- ������ ��������� ---
[System.Serializable]
public class InventoryItemSaveData
{
    public string itemName; // ��������� �� �����, ����� ����� ItemData ��� ��������
    public int quantity;
    public int slotIndex;
}

// --- ������ ������� ---
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

// --- ������ ��������� ������ ---
[System.Serializable]
public class TrainUpgradesSaveData
{
    public List<string> purchasedUpgradeItemNames;
    public bool upgradeWatering; // ��� PlantManager
}

// --- ���������� ��� ������ � ���� ---
[System.Serializable]
public class SaveSlotMetadata
{
    public bool isUsed;
    public DateTime saveTime;
    public int playerLevel;
    public string stationName; // ������ ��� ������� � UI
}

[System.Serializable]
public class AllSlotsMetadata
{
    public List<SaveSlotMetadata> slots = new List<SaveSlotMetadata>();
}