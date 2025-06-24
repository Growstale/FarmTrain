using UnityEngine;

[System.Serializable]
public class AnimalStateData
{
    public AnimalData animalData;
    public float feedTimer;
    public float productionTimer;
    public float fertilizerTimer;

    public Vector3 lastPosition;
    public bool hasBeenPlaced = false;

    public AnimalStateData(AnimalData data)
    {
        animalData = data;
        feedTimer = data.feedInterval;
        productionTimer = data.productionInterval;
        fertilizerTimer = data.fertilizerInterval;
    }
}