using UnityEngine;

public class CreateGrid : MonoBehaviour
{

    [SerializeField]GameObject bed;

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
        GenerateGrid();

    }

    void GenerateGrid()
    {
        for (float i = (PosX + gridSizeX /2) - spacingX; i < PosX+((CountBedsX + spacingX) * gridSizeX) + gridSizeX; i+= spacingX + gridSizeX) {

            for (float j = (PosY + gridSizeY / 2) - spacingY; j < PosY + ((CountBedsY + spacingY) * gridSizeY ); j+= spacingY + gridSizeY) {
                if(j  + spacingY == 0) continue; // кастыль для 2 ряда
                Vector3 spawnPosition = new Vector3(i + spacingX, j + spacingY, 0);
                GameObject newCube = Instantiate(bed,spawnPosition,Quaternion.identity);
                BedsScripts.AddBed(newCube);
                Debug.Log(newCube.gameObject.name);
                newCube.transform.localScale =new Vector3(gridSizeX,gridSizeY);
               

            }
        }
    }
}
