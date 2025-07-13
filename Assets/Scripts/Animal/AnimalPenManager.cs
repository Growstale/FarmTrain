using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnimalPenManager : MonoBehaviour
{
    public static AnimalPenManager Instance { get; private set; }
    private AudioSource audioSource;
    [Header("������������ �������")]
    [SerializeField] private List<PenConfigData> penConfigurations;

    [System.Serializable]
    public struct StartingAnimal
    {
        public AnimalData animalData;
        public int count;
    }

    [Header("��������� �����")]
    [SerializeField] private List<StartingAnimal> startingAnimals;

    private List<AnimalStateData> allAnimals = new List<AnimalStateData>();
    private Dictionary<AnimalData, int> penUpgradeLevels = new Dictionary<AnimalData, int>();

    private bool hasInitialized = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // �� �������� ������������� �����, ����� ���� ������ ���������� ���� ����������.
        // �� ������� �� ���� ��� ��� ������ �������������.
    }

    // ���� ����� ������ ����� ���������� �� ������ ��������,
    // ����� �������������, ��� �� �������� ���� �� ���.
    private void EnsureInitialized()
    {
        if (hasInitialized) return;

        Debug.Log("<color=yellow>[AnimalPenManager]</color> ������ �������������...");

        penUpgradeLevels.Clear();
        foreach (var config in penConfigurations)
        {
            penUpgradeLevels[config.animalData] = 0;
        }

        allAnimals.Clear();
        foreach (var startInfo in startingAnimals)
        {
            if (startInfo.animalData != null && startInfo.count > 0)
            {
                for (int i = 0; i < startInfo.count; i++)
                {
                    // �� �� �������� AddAnimal, � ������� ������ ��������.
                    // AddAnimal ������������ ��� ���������� ����� �������� � �������� ����.
                    AnimalStateData newState = new AnimalStateData(startInfo.animalData);
                    allAnimals.Add(newState);
                    Debug.Log($"��������� ��������� ������ ���: {startInfo.animalData.speciesName}");
                }
            }
        }

        hasInitialized = true;
    }

    public void ApplyUpgrade(ItemData upgradeItem)
    {
        if (upgradeItem == null) return;

        Debug.Log($"������� ��������� ���������: {upgradeItem.name}");

        foreach (var config in penConfigurations)
        {
            for (int i = 0; i < config.upgradeLevels.Count; i++)
            {
                var levelData = config.upgradeLevels[i];
                if (levelData.requiredUpgradeItem == upgradeItem)
                {
                    Debug.Log($"������� ���� ��� ���������: ����� {config.animalData.speciesName}, ������� {i}");

                    int currentLevel = GetCurrentPenLevel(config.animalData);
                    Debug.Log($"������� ������� ������: {currentLevel}. ��������� ���������� �������: {i - 1}");

                    if (currentLevel == i - 1)
                    {
                        penUpgradeLevels[config.animalData] = i;
                        Debug.Log($"<color=cyan>������� ������ {config.animalData.speciesName} ������� �� {i}</color>");

                        if (TrainPenController.Instance != null)
                        {
                            TrainPenController.Instance.UpdatePenVisuals(config.animalData);
                        }
                        if (audioSource != null && levelData.upgradeApplySound != null)
                        {
                            audioSource.PlayOneShot(levelData.upgradeApplySound);
                        }
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("������� �� ������ �� ���������! ��������� �� ���������.");
                    }
                }
            }
        }
        Debug.LogError($"�� ������� ���������, ��������������� �������� {upgradeItem.name} � ������������� �������!");
    }

    public int GetCurrentPenLevel(AnimalData animalData)
    {
        EnsureInitialized();
        if (penUpgradeLevels.TryGetValue(animalData, out int level))
        {
            return level;
        }
        return -1;
    }

    public PenConfigData GetPenConfigForAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        return penConfigurations.FirstOrDefault(p => p.animalData == animalData);
    }

    public List<PenConfigData> GetAllPenConfigs()
    {
        EnsureInitialized();
        return penConfigurations;
    }

    public void AddAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        AnimalStateData newState = new AnimalStateData(animalData);
        allAnimals.Add(newState);
        Debug.Log($"<color=green>[AnimalPenManager]</color> �������/��������� ����� ��������: {animalData.speciesName}.");
    }

    public bool SellAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        AnimalStateData animalToRemove = allAnimals.FirstOrDefault(a => a.animalData == animalData);
        if (animalToRemove != null)
        {
            allAnimals.Remove(animalToRemove);
            return true;
        }
        return false;
    }

    public int GetAnimalCount(AnimalData animalData)
    {
        EnsureInitialized();
        return allAnimals.Count(a => a.animalData == animalData);
    }

    public List<AnimalStateData> GetStatesForAnimalType(AnimalData animalData)
    {
        EnsureInitialized();
        return allAnimals.Where(a => a.animalData == animalData).ToList();
    }

    public int GetMaxCapacityForAnimal(AnimalData animalData)
    {
        EnsureInitialized();
        var config = GetPenConfigForAnimal(animalData);
        if (config == null) return 0;

        int currentLevel = GetCurrentPenLevel(animalData);
        if (currentLevel >= 0 && currentLevel < config.upgradeLevels.Count)
        {
            return config.upgradeLevels[currentLevel].capacity;
        }

        return 0;
    }
    public bool HasAutoFeeder(AnimalData animalData)
    {
        EnsureInitialized();

        // ������� ������������ ��� ����� ���������
        var config = GetPenConfigForAnimal(animalData);
        if (config == null) return false;

        // �������� ��� ������� ������� ���������
        int currentLevel = GetCurrentPenLevel(animalData);
        if (currentLevel < 0 || currentLevel >= config.upgradeLevels.Count) return false;

        // ��������� ������ �������� ������
        return config.upgradeLevels[currentLevel].providesAutoFeeding;
    }
    public ItemData GetNextAvailableUpgrade(AnimalData animalData)
    {
        EnsureInitialized();
        var config = GetPenConfigForAnimal(animalData);
        if (config == null) return null;

        int currentLevel = GetCurrentPenLevel(animalData);

        // ���������, ���� �� ��������� ������� � ������ ���������
        int nextLevelIndex = currentLevel + 1;
        if (nextLevelIndex < config.upgradeLevels.Count)
        {
            // ���������� �������, ����������� ��� �������� �� ��������� �������
            return config.upgradeLevels[nextLevelIndex].requiredUpgradeItem;
        }

        // ���� ���������� ������ ���, ������ ��� ��� �������
        return null;
    }

    public List<AnimalStateData> GetSaveData()
    {
        // ��� ����� ���������, ��� "�����" �������� �� ����� ��������� ���� ��������� ���������
        foreach (var animalController in FindObjectsOfType<AnimalController>())
        {
            animalController.SaveState();
        }
        return allAnimals;
    }

    public void ApplySaveData(List<AnimalStateData> data)
    {
        EnsureInitialized(); // ��������, ��� �������� �����

        if (data == null || data.Count == 0) // ����� ����
        {
            // ������ ��� ��������� �������� ��� � EnsureInitialized,
            // ������ ������� ������ �� ������ ������ � ������� ��.
            allAnimals.Clear();
            hasInitialized = false; // ������� ����, ����� EnsureInitialized �������� ������
            EnsureInitialized();
        }
        else
        {
            allAnimals = new List<AnimalStateData>(data);
        }

        // ������������ �������� �� ����� ���������� � TrainPenController
        // ����� �� ������� ��� ����������� ������.
    }

}