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

      
        // »нициализаци€ словар€ (если нет сохранени€, он будет пустым)
        positionBed = new Dictionary<int, List<Vector2Int>>();
        positionBed.Add(0, new List<Vector2Int>());
        positionBed.Add(1, new List<Vector2Int>());
        // CkeckValue(); // Ётот метод можно убрать или оставить дл€ отладки
        SaveLoadManager.Instance.SaveGame();
        
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



    public int GetPlacedBedsCount()
    {
        int placedBeds = 0;
        GridGenerator[] allGrids = FindObjectsOfType<GridGenerator>();

        // ѕроходимс€ по каждой сетке
        foreach (GridGenerator grid in allGrids)
        {
            // ѕроходимс€ по всем слотам внутри сетки
            foreach (GameObject slotObject in grid.gridObjects.Values)
            {
                SlotScripts slot = slotObject.GetComponent<SlotScripts>();
                // ≈сли у слота есть скрипт и в нем установлена гр€дка (ishavebed == true)
                if (slot != null && slot.ishavebed)
                {
                    placedBeds++;
                }
            }
        }
        return placedBeds;
    }
}
