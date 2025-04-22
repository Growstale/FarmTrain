using UnityEngine;
using UnityEngine.UI;

public class SpawnButtonHandler : MonoBehaviour
{
    [Tooltip("Ссылка на главный спаунер в сцене")]
    public ItemSpawner mainSpawner; 

    [Header("Что спавнить этой кнопкой")]
    public ItemData itemToSpawn;
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnScale = Vector3.one;

    public void HandleSpawnClick()
    {
        if (mainSpawner == null)
        {
            Debug.LogError("Main Spawner не назначен!");
            return;
        }
        if (itemToSpawn == null)
        {
            Debug.LogError("ItemData для спавна не назначен на этой кнопке!");
            return;
        }

        GameObject spawnedObject = mainSpawner.SpawnItem(itemToSpawn, spawnPosition, spawnScale);

        if (spawnedObject != null)
        {
            Debug.Log($"Кнопка успешно заспавнила {itemToSpawn.itemName}");
        }
        else
        {
            Debug.LogWarning($"Кнопке не удалось заспавнить {itemToSpawn.itemName}");
        }
    }
}