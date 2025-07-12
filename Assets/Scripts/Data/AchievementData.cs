using UnityEngine;

// Enum остается тем же
public enum TypeOfAchivment
{
    BountifulHarvest,
    Rancher,
    BuddingTycoon,
    TheWholeGangsHere,
    MasterGardener,
    StateoftheArtFarm,
    FarmingLegend
}

[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievements/Achievement")]
public class AchievementData : ScriptableObject
{
    [Header("Основная информация")]
    public int IDArchievment;
    public string Name; 
    [TextArea] public string Description;
    public Sprite Icon;
    public int reward;


    [Header("Условие выполнения")]
    public TypeOfAchivment typeOfAchivment;
    public int goal; // Целевое значение

    
}