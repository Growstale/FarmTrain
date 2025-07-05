using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Station Data", menuName = "Game/Station Data")]
public class StationData : ScriptableObject
{
    public int stationId;
    public string stationName;
    // Сюда мы будем добавлять данные для каждого ларька на этой станции.
    // Используем список, так как ларьков может быть несколько.
    public List<ShopInventoryData> stallInventories;
}