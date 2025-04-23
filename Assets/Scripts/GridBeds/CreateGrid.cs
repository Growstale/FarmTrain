using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    

[SerializeField]GameObject slot;
    [SerializeField] GameObject bedprefab;
    [SerializeField] GameObject BedManager;
    BedsManagerScript bedsManagerScript;

    [Header("Position")]
    public float PosX; // позиция сетки по X
    public float PosY; // позиция сетки по Y

    [Header("Size 1 slot")]
    public float gridSizeX; // Размер слота по x
    public float gridSizeY; // размер слота по y

    [Header("gridSize")]
    public float spacingX; // отступ между ячейками
    public float spacingY; // отступ между ячейками
    public int CountBedsX; // Размер сетки X
    public int CountBedsY; // Размер сетки Y

    void Start()
    {
        bedsManagerScript = BedManager.GetComponent<BedsManagerScript>();
        GenerateGrid();

    }

    void GenerateGrid()
    {
        int col = 2;
        int count = 0;
        for (float i = (PosX + gridSizeX /2) - spacingX; i < PosX+((CountBedsX + spacingX) * gridSizeX) + gridSizeX; i+= spacingX + gridSizeX) {
            for (float j = (PosY + gridSizeY / 2) - spacingY; j < PosY + ((CountBedsY + spacingY) * gridSizeY ); j+= spacingY + gridSizeY) {
                count++;
                if (count == col) { 
                    col += 3;
                    continue; } 
                Vector3 spawnPosition = new Vector3(i + spacingX, j + spacingY, 0);
                GameObject newCube = Instantiate(slot, spawnPosition,Quaternion.identity);
                GenerateBed(bedprefab, newCube);
                newCube.transform.localScale =new Vector3(gridSizeX,gridSizeY);
            }
        }
    }
    void GenerateBed(GameObject smallSquarePrefab, GameObject largeSquare)
    {
        
        float smallSquareOffset = 0.2f; // Расстояние между маленькими квадратами
        
        // Верхний левый угол
        GameObject smallSquare1 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(-smallSquareOffset, smallSquareOffset, 0), Quaternion.identity);
        
        smallSquare1.transform.parent = largeSquare.transform;
        bedsManagerScript.AddBed(smallSquare1);

        // Верхний правый угол
        GameObject smallSquare2 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(smallSquareOffset, smallSquareOffset, 0), Quaternion.identity);
        smallSquare2.transform.parent = largeSquare.transform;
        bedsManagerScript.AddBed(smallSquare2);
        // Нижний левый угол
        GameObject smallSquare3 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(-smallSquareOffset  , -smallSquareOffset , 0), Quaternion.identity);
        smallSquare3.transform.parent = largeSquare.transform;
        bedsManagerScript.AddBed(smallSquare3);
        // Нижний правый угол
        GameObject smallSquare4 = Instantiate(smallSquarePrefab, largeSquare.transform.position + new Vector3(smallSquareOffset , -smallSquareOffset , 0), Quaternion.identity);
        smallSquare4.transform.parent = largeSquare.transform;
        bedsManagerScript.AddBed(smallSquare4);
    }

}


