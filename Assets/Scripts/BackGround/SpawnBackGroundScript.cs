using UnityEngine;

public class SpawnBackGroundScript : MonoBehaviour
{
    public GameObject Object; // префаб фона
    [SerializeField] float timeInterval; // интервал времени
    [SerializeField] float startTimeInterval; // первое значение времени
   // [SerializeField] Sprite NewFonts;
     [SerializeField] Sprite OldFonts; // фон для для картиник (понадобиться при смене фона)




    [System.Serializable]
    public struct PositionSpawn
    {
        public float x;
        public float y;
        public float z;
    }
    public PositionSpawn spawnPoint; // позиция спавна объекта




    void Start()
    {
        Object.GetComponent<SpriteRenderer>().sprite = OldFonts;

        InvokeRepeating("CreateObjects", startTimeInterval, timeInterval); // Вызывает функцию каждые timeInterval функцию CreateObject


    }

    void CreateObjects()
    {
        GameObject newFon = Instantiate(Object, new Vector3(spawnPoint.x, spawnPoint.y, spawnPoint.z), Quaternion.identity); // создание объекта в координатах


    }
    //void ChangeTextureBackground()
    //{
    //    SpriteRenderer spriteRenderer = Object.GetComponent<SpriteRenderer>();
    //    spriteRenderer.sprite = NewFonts;
    //}
}
