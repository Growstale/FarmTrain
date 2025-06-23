using UnityEngine;
using UnityEngine.UI;

public class TestAnimalSpawnerButton : MonoBehaviour
{
    public ItemSpawner itemSpawner;
    public ItemData animalItemToSpawn; 
    public Transform spawnPoint;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(SpawnAnimal);
        }
        if (itemSpawner == null || animalItemToSpawn == null || spawnPoint == null)
        {
            Debug.LogError("Не все поля назначены в TestAnimalSpawnerButton!", gameObject);
        }
    }

    public void SpawnAnimal()
    {
        if (itemSpawner != null && animalItemToSpawn != null && spawnPoint != null)
        {
            Debug.Log($"Нажата кнопка спавна {animalItemToSpawn.itemName}");
            itemSpawner.SpawnItem(animalItemToSpawn, spawnPoint.position);
        }
        else
        {
            Debug.LogError("Не могу заспавнить животное - не все ссылки установлены!");
        }
    }
}