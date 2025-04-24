using UnityEngine;
using UnityEngine.UI;

public class TestAnimalSpawnerButton : MonoBehaviour
{
    public ItemSpawner itemSpawner; // Перетащи сюда объект со спавнером из сцены
    public ItemData animalItemToSpawn; // Перетащи сюда CowItem (ItemData с типом Animal)
    public Transform spawnPoint; // Пустой объект в сцене, где спавнить

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(SpawnAnimal);
        }
        // Проверки на null для itemSpawner, animalItemToSpawn, spawnPoint
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
            // Используем позицию spawnPoint для спавна
            itemSpawner.SpawnItem(animalItemToSpawn, spawnPoint.position);
        }
        else
        {
            Debug.LogError("Не могу заспавнить животное - не все ссылки установлены!");
        }
    }
}