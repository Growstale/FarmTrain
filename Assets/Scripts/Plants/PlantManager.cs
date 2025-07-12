using System.Collections.Generic;
using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    public bool UpgradeWatering;
    public ItemData _UpgradeData;

    public Dictionary<int, List<Vector2Int>> positionBed;

    public static GameSaveData SessionData { get; private set; }

    

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

      
        // Инициализация словаря (если нет сохранения, он будет пустым)
        positionBed = new Dictionary<int, List<Vector2Int>>();
        positionBed.Add(0, new List<Vector2Int>());
        positionBed.Add(1, new List<Vector2Int>());
        // CkeckValue(); // Этот метод можно убрать или оставить для отладки
        SaveLoadManager.Instance.SaveGame();
        Debug.Log(">>>>>>>>>>>>>>>>>>>Save Game");
    }

    private void Start()
    {
       
    }

    public bool CompleteWateringUpgrade()
    {
        Debug.Log("Complete upgrade");
       UpgradeWatering = true;
        
        return true;
    }
    public void AddNewPositionBed(int level, Vector2Int posBed)
    {

       
        if (!positionBed.ContainsKey(level))
        {
           positionBed.Add(level, new List<Vector2Int>());
           
        }
        positionBed[level].Add(posBed);
    }
    
    public List<Vector2Int> GetValueFromDictionary(int key)
    {
        if(positionBed.TryGetValue(key, out List<Vector2Int> value))
          return value;  
        else  return null;
    }




}
