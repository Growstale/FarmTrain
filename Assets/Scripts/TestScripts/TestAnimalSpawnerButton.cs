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
            if (animalItemToSpawn.itemType != ItemType.Animal || animalItemToSpawn.associatedAnimalData == null)
            {
                Debug.LogError($"Предмет {animalItemToSpawn.itemName} не является животным или для него не указаны данные (AnimalData). Учет не будет обновлен.");
                return; 
            }

            Debug.Log($"Нажата кнопка спавна {animalItemToSpawn.itemName}");

            AnimalPenManager.Instance.AddAnimal(animalItemToSpawn.associatedAnimalData);

            Debug.Log($"Данные обновлены. Теперь в AnimalPenManager есть новое животное типа {animalItemToSpawn.associatedAnimalData.speciesName}");
        }
        else
        {
            Debug.LogError("Не могу заспавнить животное - не все ссылки установлены!");
        }
    }

}