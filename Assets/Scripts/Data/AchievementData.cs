using UnityEngine;
using UnityEngine.Events;


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

[CreateAssetMenu(fileName = "Achievement",menuName = "Achievements/Achievement")]
public class AchievementData : ScriptableObject
{
    [Header("Основная информация")]
    public int IDArchievment;
    public string Name;
    [TextArea] public string Description;
    public Sprite Icon;

    [Header("Условие выполнения")]
    public TypeOfAchivment typeOfAchivment;
    public int goal; // целевое назгачение в кол-во
    public bool isReceived = false;


}
