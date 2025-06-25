using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    

[SerializeField]GameObject slot;
    [SerializeField] GameObject bedprefab;
    [SerializeField] GameObject SpawnManager;
    SlotManagerScript _slotManagerScript;
    
    [SerializeField]ItemData _bedData;
    [SerializeField] GameObject _Wagon;


    [Header("Position")]
    public float GridPosX; // позиция сетки по X
    public float GridPosY; // позиция сетки по Y


    [Header("gridSize")]
    //public float spacingX; // отступ между ячейками
    public float spacingY; // отступ между ячейками
    public int CountBedsX; // Размер сетки X
    public int CountBedsY; // Размер сетки Y

    private float slotPosX;
    private float slotPosY;
    GameObject newSlot;
    void Awake()
    {
        slotPosX = GridPosX;
        slotPosY = GridPosY;
       
        GenerateGrid();

    }

    void GenerateGrid()
    {
   
        for (float i = 1; i < CountBedsX; i++) {
           
            for (int j = 0; j < CountBedsY; j++) {
                Vector3 spawnPosition = new Vector3(slotPosX, slotPosY, 0);
                newSlot = Instantiate(slot,spawnPosition,Quaternion.identity);

                newSlot.transform.position = spawnPosition;
                if (newSlot == null)
                {
                    Debug.Log("newslot null");
                }
                if (bedprefab == null)
                {
                    Debug.Log("bedprefab null");
                }
                GenerateBed(bedprefab, newSlot);
                slotPosY += newSlot.transform.localScale.y + spacingY;
                newSlot.transform.parent = _Wagon.transform;
            }
            slotPosX += newSlot.transform.localScale.x * 2.85f;
            slotPosY = GridPosY;
        }
    }
    void GenerateBed(GameObject smallSquarePrefab, GameObject largeSquare)
    {
        

        float lengthSlot = largeSquare.transform.localScale.x;
        float widthSlot = largeSquare.transform.localScale.y;
        Vector3 sizeBed = new Vector3(lengthSlot / 2, widthSlot / 2);

        // Верхний левый угол
        GameObject smallSquare1 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(-lengthSlot/4,  widthSlot/4, 0), Quaternion.identity);
        smallSquare1.name = "bedSlot1";
        smallSquare1.transform.localScale = sizeBed;
        smallSquare1.transform.parent = largeSquare.transform;
        

        // Верхний правый угол
        GameObject smallSquare2 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(-lengthSlot / 4, -widthSlot / 4, 0), Quaternion.identity);
        smallSquare2.name = "bedSlot2";
        smallSquare2.transform.localScale = sizeBed;
        smallSquare2.transform.parent = largeSquare.transform;


        // Нижний левый угол
        GameObject smallSquare3 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(lengthSlot / 4, -widthSlot / 4, 0), Quaternion.identity);
        smallSquare3.name = "bedSlot3";
        smallSquare3.transform.localScale = sizeBed;
        smallSquare3.transform.parent = largeSquare.transform;


        // Нижний правый угол
        GameObject smallSquare4 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(lengthSlot / 4, widthSlot / 4, 0), Quaternion.identity);
        smallSquare4.name = "bedSlot4";
        smallSquare4.transform.localScale = sizeBed;
        smallSquare4.transform.parent = largeSquare.transform;

       // _slotManagerScript.AddBed(largeSquare,new GameObject[] { smallSquare1 , smallSquare2, smallSquare3, smallSquare4});

    }

  
}


