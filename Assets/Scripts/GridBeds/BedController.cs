using UnityEngine;

public class BedController : MonoBehaviour
{

    [Header("Growth")]
    // ссылка на data растения 
    [SerializeField] BedData bedData;
   public  bool isRaked = false;
    public bool isFertilize = false;    
    // 
    SpriteRenderer _spriteRenderer;
    BedData.StageGrowthPlant Stagebed;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
           
            if (bedData != null)
            {
               _spriteRenderer.sprite = bedData.bedSprites[0];
                Stagebed = BedData.StageGrowthPlant.DrySoil;

            }
            else
            {
                Debug.LogError("Отсутсвтует ссылка на bedData , объект удален");
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogError("Отсутсвтует ссылка на _spriteRenderer , объект удален");
            Destroy(gameObject);
        }
       if(isRaked)
        {
           
            ChangeStage(BedData.StageGrowthPlant.Raked, 1);
           
        }
        if (isFertilize)
        {
            ChangeStage(BedData.StageGrowthPlant.WithFertilizers, 3);
        }
        if (!isRaked && !isFertilize) { 
        
             ChangeStage(BedData.StageGrowthPlant.DrySoil, 0);
        }
    }

    public void ChangeStage(BedData.StageGrowthPlant stage, int idx)
    {
        // --- НАЧАЛО ИСПРАВЛЕНИЯ ---
        // Если _spriteRenderer еще не был получен (например, потому что Awake еще не вызвался),
        // то получаем его прямо сейчас.
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---

        // Теперь можно быть уверенным, что _spriteRenderer не null (если компонент вообще есть на объекте)
        if (bedData != null && bedData.bedSprites.Count > idx)
        {
            // Это ваша строка 44
            _spriteRenderer.sprite = bedData.bedSprites[idx];
            Debug.Log($"Спрайт грядки изменен на стадию {stage}");
        }
        else
        {
            Debug.LogError("Не удалось изменить спрайт грядки: bedData не назначен или индекс спрайта выходит за пределы массива.");
        }
    }
}
