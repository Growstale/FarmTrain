using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Bed", menuName = "Farming/Bed")]

public class BedData : ScriptableObject
{
    public string speciesName = "New Bed";
    public GameObject bedlPrefab; // Префаб грядок


    [Header("Growth")]
    public Sprite bedSprite; //  спрайт грядки 
    public bool isPlanted; // засажена ли грядка
    
 
}
