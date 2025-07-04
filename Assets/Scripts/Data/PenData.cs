using UnityEngine;

// 1. Конфигурационные данные (хранятся в AnimalPenManager)
// НЕ СОДЕРЖАТ ссылок на объекты сцены.
[System.Serializable]
public class PenConfigData
{
    public AnimalData animalData;
    public int maxCapacity;

    // Имена объектов, которые мы будем искать на сцене поезда.
    // Это надежнее, чем прямые ссылки.
    public string placementAreaName;
    public string animalParentName;
}

// 2. "Живые" данные (используются TrainPenController во время игры)
// СОДЕРЖАТ ссылки на реальные объекты на сцене.
public class PenRuntimeInfo
{
    public PenConfigData config; // Ссылка на исходные данные
    public Collider2D placementArea;
    public Transform animalParent;
}