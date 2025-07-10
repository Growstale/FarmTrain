using UnityEngine;

public class PlantManager : MonoBehaviour
{
    public static PlantManager instance;

    public bool UpgradeWatering;
    public ItemData _UpgradeData;
    void Start()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CompleteWateringUpgrade()
    {
        Debug.Log("Complete upgrade");
       UpgradeWatering = true;
        return true;
    }

}
