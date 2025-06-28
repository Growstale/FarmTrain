using UnityEngine;

public class WheatController : MonoBehaviour
{


    [Header("Growth")]
    // ссылка на data растения 
    [SerializeField] PlantData plantData;

    // 
    SpriteRenderer _spriteRenderer;
    PlantData.StageGrowthPlant Stageplant;


    float timePerGrowthStage = 0.0f;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if(plantData != null)
        {
            Stageplant = PlantData.StageGrowthPlant.defaultStage;
            timePerGrowthStage = plantData.timePerGrowthStage;
            _spriteRenderer.sprite = plantData.growthStagesSprites[2];
            InvokeRepeating("StartPlantGrowth", 0f, timePerGrowthStage);
        }
        else
        {
            Debug.LogError("Отстутсвует ссылка на данные растения");
        }
    }



    void StartPlantGrowth()
    {
        switch (Stageplant) {
            case PlantData.StageGrowthPlant.defaultStage:
                _spriteRenderer.sprite = plantData.growthStagesSprites[0];
                Stageplant = PlantData.StageGrowthPlant.SecondStage;
                break;
            case PlantData.StageGrowthPlant.SecondStage:
                _spriteRenderer.sprite = plantData.growthStagesSprites[1];
                Stageplant = PlantData.StageGrowthPlant.ThirdStage;
                break;
            case PlantData.StageGrowthPlant.ThirdStage:
                _spriteRenderer.sprite = plantData.growthStagesSprites[2];
                Stageplant = PlantData.StageGrowthPlant.FourthStage;
                break;
            case PlantData.StageGrowthPlant.FourthStage:
                _spriteRenderer.sprite = plantData.growthStagesSprites[3];
                Stageplant = PlantData.StageGrowthPlant.FifthStage;
                break;
            case PlantData.StageGrowthPlant.FifthStage:
                _spriteRenderer.sprite = plantData.growthStagesSprites[4];
                CancelInvoke("StartPlantGrowth");
                break;
        }
        
    }
}
