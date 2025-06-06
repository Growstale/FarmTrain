using UnityEngine;
using UnityEngine.UI;

public class TestAnimalSpawnerButton : MonoBehaviour
{
    public ItemSpawner itemSpawner; // �������� ���� ������ �� ��������� �� �����
    public ItemData animalItemToSpawn; // �������� ���� CowItem (ItemData � ����� Animal)
    public Transform spawnPoint; // ������ ������ � �����, ��� ��������

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(SpawnAnimal);
        }
        // �������� �� null ��� itemSpawner, animalItemToSpawn, spawnPoint
        if (itemSpawner == null || animalItemToSpawn == null || spawnPoint == null)
        {
            Debug.LogError("�� ��� ���� ��������� � TestAnimalSpawnerButton!", gameObject);
        }
    }

    public void SpawnAnimal()
    {
        if (itemSpawner != null && animalItemToSpawn != null && spawnPoint != null)
        {
            Debug.Log($"������ ������ ������ {animalItemToSpawn.itemName}");
            // ���������� ������� spawnPoint ��� ������
            itemSpawner.SpawnItem(animalItemToSpawn, spawnPoint.position);
        }
        else
        {
            Debug.LogError("�� ���� ���������� �������� - �� ��� ������ �����������!");
        }
    }
}