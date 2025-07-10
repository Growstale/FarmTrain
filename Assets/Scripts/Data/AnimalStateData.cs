using UnityEngine;

[System.Serializable]
public class AnimalStateData
{
    public AnimalData animalData;

    // --- NEW FIELDS ---
    // Replaces the three old timers (feedTimer, productionTimer, fertilizerTimer)
    public AnimalProductionState productionState;
    public float cycleTimer;
    // --- END NEW FIELDS ---

    public Vector3 lastPosition;
    public bool hasBeenPlaced = false;

    // The constructor now initializes the new sequential state.
    public AnimalStateData(AnimalData data)
    {
        animalData = data;
        // The animal always starts by needing to be fed.
        productionState = AnimalProductionState.WaitingForFeed;
        cycleTimer = data.feedInterval;
    }
}