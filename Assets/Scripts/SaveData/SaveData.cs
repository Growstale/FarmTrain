// SaveData.cs
using UnityEngine;
using System.Collections.Generic;

// Обязательно для сериализации в JSON
[System.Serializable]
public class PlantSaveData
{
    public string plantDataName; // Имя ScriptableObject'а растения для его загрузки
    public string gridIdentifier; // К какому GridGenerator'у относится ("GridGeneratorUp" или "GridGeneratorDown")
    public Vector2Int[] idSlots; // Какие слоты занимает

    public int currentStage; // Текущая стадия роста (int)
    public float growthTimer;
    public float waterNeedTimer;
    public bool isNeedWater;
    public bool isFertilize;
    public Vector3 currentposition;
}

[System.Serializable]
public class SlotSaveData
{
    public Vector2Int gridPosition;
    public bool isPlanted;
    public bool ishavebed;
    public bool isRaked;
    public bool isFertilize;
}

[System.Serializable]
public class GridSaveData
{
    public string identifier; // "GridGeneratorUp" или "GridGeneratorDown"
    public List<SlotSaveData> slotsData = new List<SlotSaveData>();
}

[System.Serializable]
public class GameSaveData
{
    // Состояние менеджера
    public bool upgradeWatering;

    // Состояния всех сеток и растений
    public List<GridSaveData> gridsData = new List<GridSaveData>();
    public List<PlantSaveData> plantsData = new List<PlantSaveData>();
}