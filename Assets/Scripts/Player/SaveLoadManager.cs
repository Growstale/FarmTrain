// SaveLoadManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    private string[] _saveFilePaths = new string[4];
    private string _metadataFilePath;

    private GameData _pendingDataToApply = null;

    // �������� ��� ������� � �������� �����. ������ ��� ����� ����������������� � PlayerPrefs.
    public static int CurrentSlotID { get; private set; }

    void Awake()
    {
        Debug.Log($"<color=lime>����� � ������������: {Application.persistentDataPath}</color>");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < 4; i++)
            {
                _saveFilePaths[i] = Path.Combine(Application.persistentDataPath, $"save_slot_{i}.json");
            }
            _metadataFilePath = Path.Combine(Application.persistentDataPath, "metadata.json");

            // ��� ������� ����: ��������� ID ���������� ��������������� ����� �� ������ ����������
            CurrentSlotID = PlayerPrefs.GetInt("LastUsedSlotID", -1); // -1, ���� ��� ������ �� �����������
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- ������� ������ ---

    public void StartNewGame(int slotId)
    {
        SetCurrentSlot(slotId);

        // ������� ������ ��������� ������ ��� ����� ����
        _pendingDataToApply = new GameData();

        // <<< ���������: �������� ApplyPersistentManagerData � ������� ������� (null) >>>
        // ��� �������� ��������� ��������� ������ ����� ���� (������ ��������� ��������, ������ � �.�.)
        // �� �������� _pendingDataToApply, ������� ������ ��� ��� ������, �� ��� ���� (���� playerData) ��� null.
        ApplyPersistentManagerData(_pendingDataToApply);

        // ������� ������ ����, ���� �� ���
        if (File.Exists(_saveFilePaths[slotId]))
        {
            File.Delete(_saveFilePaths[slotId]);
        }
        UpdateSlotMetadata(slotId, false);

        SceneManager.LoadScene("SampleScene");
    }


    public void LoadGame(int slotId)
    {
        if (!File.Exists(_saveFilePaths[slotId]))
        {
            Debug.LogError($"���� ���������� ��� ����� {slotId} �� ������!");
            return;
        }

        SetCurrentSlot(slotId); // ������������� � ���������� ��������� ����

        string json = File.ReadAllText(_saveFilePaths[slotId]);
        _pendingDataToApply = JsonUtility.FromJson<GameData>(json);

        ApplyPersistentManagerData(_pendingDataToApply);
        SceneManager.LoadScene("SampleScene");
    }

    public void SaveGame()
    {
        if (CurrentSlotID < 0 || CurrentSlotID >= 4)
        {
            Debug.LogError("���������� ��������� ����, �� ������ ����!");
            return;
        }

        Debug.Log($"<color=orange>������� ���� ������ ��� ���������� � ���� {CurrentSlotID}...</color>");

        // 1. ������� ������ ��������� ��� ���� ������
        GameData data = new GameData();

        // 2. �������� ��������������� �������� ������ �� ���� ����������

        // --- ������ ��������� (�� Initializer) ---
        data.playerData = GatherPlayerData(); // �������� ������ �� PlayerWallet � ExperienceManager
        data.inventoryItems = InventoryManager.Instance.GetSaveData();
        data.trainUpgradesData = GatherTrainUpgradeData(); // �������� ������ �� TrainUpgradeManager � PlantManager

        // --- ��������� ����� (�� SampleScene, Station, � �.�.) ---

        // �������� ����� QuestManager � ������� ��� ������
        if (QuestManager.Instance != null)
        {
            data.questData = QuestManager.Instance.GetSaveData();
            Debug.Log("<color=green>������ ������� �������.</color>");
        }

        // �������� ����� AchievementManager � ������� ��� ������
        if (AchievementManager.instance != null)
        {
            data.achievementData = AchievementManager.instance.GetSaveData();
            Debug.Log("<color=green>������ ���������� �������.</color>");
        }

        // �������� ����� AnimalPenManager � ������� ��� ������
        if (AnimalPenManager.Instance != null)
        {
            data.animalData = AnimalPenManager.Instance.GetSaveData();
            Debug.Log("<color=green>������ �������� �������.</color>");
        }

        // --- ���� ������ � �������� �� ����� (������ � ��������) ---

        // ������� ��� GridGenerator-� � �������� � ��� ������
        var gridGenerators = FindObjectsOfType<GridGenerator>();
        data.gridsData = new List<GridSaveData>();
        foreach (var grid in gridGenerators)
        {
            data.gridsData.Add(grid.GetSaveData());
        }
        Debug.Log($"<color=green>������� ������ � {gridGenerators.Length} ����� (Grid).</color>");


        // ������� ��� PlantController-� � �������� � ��� ������
        var plants = FindObjectsOfType<PlantController>();
        data.plantsData = new List<PlantSaveData>();
        foreach (var plant in plants)
        {
            PlantSaveData plantData = plant.GetSaveData();
            if (plantData != null)
            {
                data.plantsData.Add(plantData);
            }
        }
        Debug.Log($"<color=green>������� ������ � {plants.Length} ��������.</color>");


        // 3. ������������� ����� ����������
        data.lastSaved = DateTime.Now;

        // 4. ����������� �� � JSON � ���������� � ����
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_saveFilePaths[CurrentSlotID], json);

        // 5. ��������� ���������� ��� ����
        UpdateSlotMetadata(CurrentSlotID, true, data);

        Debug.Log($"<color=orange>���� ������� ��������� � ����: {_saveFilePaths[CurrentSlotID]}</color>");
    }


    // ����� ����� ��� ��������� � ���������� �����
    private void SetCurrentSlot(int slotId)
    {
        CurrentSlotID = slotId;
        // ��������� ����� � ������ ����������, ����� �� �� ��������� ����� �����������
        PlayerPrefs.SetInt("LastUsedSlotID", slotId);
        PlayerPrefs.Save(); // ���������� ���������� �� ����
        Debug.Log($"������� ���� ����������: {CurrentSlotID}");
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene" && _pendingDataToApply != null)
        {
            StartCoroutine(ApplySceneSpecificDataCoroutine());
        }
    }

    private System.Collections.IEnumerator ApplySceneSpecificDataCoroutine()
    {
        yield return null;

        Debug.Log("���������� ������ � ���������� ����� SampleScene...");

        AnimalPenManager.Instance.ApplySaveData(_pendingDataToApply.animalData);
        QuestManager.Instance.ApplySaveData(_pendingDataToApply.questData);
        AchievementManager.instance.ApplySaveData(_pendingDataToApply.achievementData); // <<< ������ ���� ���������

        var gridGenerators = FindObjectsOfType<GridGenerator>().ToDictionary(g => g.identifier, g => g);
        foreach (var gridData in _pendingDataToApply.gridsData)
        {
            if (gridGenerators.TryGetValue(gridData.identifier, out var grid))
            {
                grid.ApplySaveData(gridData);
            }
        }

        foreach (var plant in FindObjectsOfType<PlantController>())
        {
            Destroy(plant.gameObject);
        }

        foreach (var plantData in _pendingDataToApply.plantsData)
        {
            if (gridGenerators.TryGetValue(plantData.gridIdentifier, out var grid))
            {
                grid.SpawnPlantFromSave(plantData);
            }
        }

        PlantManager.instance.ApplySaveData(_pendingDataToApply.trainUpgradesData);
        TrainUpgradeManager.Instance.ApplySaveData(_pendingDataToApply.trainUpgradesData);

        _pendingDataToApply = null;
        Debug.Log("���������� ������ ���������.");
    }

    private void ApplyPersistentManagerData(GameData data)
    {
        PlayerWallet.Instance.ApplySaveData(data.playerData);
        ExperienceManager.Instance.ApplySaveData(data.playerData);
        InventoryManager.Instance.ApplySaveData(data.inventoryItems);
    }

    private PlayerSaveData GatherPlayerData()
    {
        return new PlayerSaveData
        {
            currentMoney = PlayerWallet.Instance.GetCurrentMoney(),
            currentLevel = ExperienceManager.Instance.CurrentLevel,
            currentPhase = ExperienceManager.Instance.CurrentPhase,
            currentXP = ExperienceManager.Instance.CurrentXP
        };
    }

    private TrainUpgradesSaveData GatherTrainUpgradeData()
    {
        return new TrainUpgradesSaveData
        {
            purchasedUpgradeItemNames = TrainUpgradeManager.Instance.GetSaveData(),
            upgradeWatering = PlantManager.instance.UpgradeWatering
        };
    }

    public AllSlotsMetadata GetAllSlotsMetadata()
    {
        if (!File.Exists(_metadataFilePath))
        {
            var emptyMeta = new AllSlotsMetadata();
            for (int i = 0; i < 4; i++) emptyMeta.slots.Add(new SaveSlotMetadata());
            File.WriteAllText(_metadataFilePath, JsonUtility.ToJson(emptyMeta));
            return emptyMeta;
        }
        string json = File.ReadAllText(_metadataFilePath);
        return JsonUtility.FromJson<AllSlotsMetadata>(json);
    }

    private void UpdateSlotMetadata(int slotId, bool isUsed, GameData data = null)
    {
        var allMeta = GetAllSlotsMetadata();
        if (isUsed && data != null)
        {
            allMeta.slots[slotId].isUsed = true;
            allMeta.slots[slotId].saveTime = data.lastSaved;
            allMeta.slots[slotId].playerLevel = data.playerData.currentLevel;
            allMeta.slots[slotId].stationName = $"Level {data.playerData.currentLevel}";
        }
        else
        {
            allMeta.slots[slotId].isUsed = false;
        }

        string json = JsonUtility.ToJson(allMeta, true);
        File.WriteAllText(_metadataFilePath, json);
    }
}