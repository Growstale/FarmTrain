using UnityEngine;
using System;

public enum GamePhase
{
    Train,
    Station
}

public class ExperienceManager : MonoBehaviour
{
    // ... (��� ���� � Awake/Initialize/AddXP/UpdateXpThreshold �������� ��� ���������) ...
    public static ExperienceManager Instance { get; private set; }

    [System.Serializable]
    public struct XpThreshold { public int trainPhaseXP; public int stationPhaseXP; }
    [SerializeField] private XpThreshold[] xpLevels;

    public int CurrentLevel { get; private set; }
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentXP { get; private set; }
    public int XpForNextPhase { get; private set; }

    public event Action<int, int> OnXPChanged;
    public event Action<int, GamePhase> OnPhaseUnlocked;
    [SerializeField] private AudioClip trainDepartSound;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        CurrentLevel = 1;
        CurrentPhase = GamePhase.Train;
        CurrentXP = 0;
        UpdateXpThreshold();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0 || XpForNextPhase == 0) return;
        CurrentXP += amount;
        Debug.Log($"�������� {amount} XP. �����: {CurrentXP}/{XpForNextPhase}");
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        if (CurrentXP >= XpForNextPhase)
        {
            OnPhaseUnlocked?.Invoke(CurrentLevel, CurrentPhase);
            Debug.Log($"<color=cyan>������� �� ��������� ���� �������������!</color>");
        }
    }

    // <<< ����� �����: ���������� ��� �������� � ������ �� �������
    public void EnterStation()
    {
        if (CurrentPhase != GamePhase.Train) return;

        CurrentPhase = GamePhase.Station;
        CurrentXP = 0; // �������� ���� ��� ������� �� �������
        UpdateXpThreshold();
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        QuestManager.Instance.ActivateQuestsForCurrentPhase();
        Debug.Log($"<color=yellow>���� �� ������� {CurrentLevel}. ����: {CurrentPhase}.</color>");
    }

    // <<< ����� �����: ���������� ��� ����������� �� ������� �� ��������� �����
    public void DepartToNextTrainLevel()
    {
        if (CurrentPhase != GamePhase.Station) return;

        CurrentPhase = GamePhase.Train;
        CurrentLevel++; // ����������� ������� ����
        CurrentXP = 0; // �������� ���� ��� ������� �� ����� ������
        UpdateXpThreshold();
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
        QuestManager.Instance.ActivateQuestsForCurrentPhase();
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.UpdateRadioByLevel(CurrentLevel);
        }

        if (audioSource != null && trainDepartSound != null)
        {
            audioSource.PlayOneShot(trainDepartSound);
        }
        Debug.Log($"<color=yellow>����������� �� ����� {CurrentLevel}. ����: {CurrentPhase}.</color>");
    }

    // <<< ������� ������ ����� AdvanceToNextPhase() >>>
    // public void AdvanceToNextPhase() { ... }

    private void UpdateXpThreshold()
    {
        int levelIndex = CurrentLevel - 1;
        if (levelIndex < 0 || levelIndex >= xpLevels.Length)
        {
            XpForNextPhase = int.MaxValue;
            return;
        }

        if (CurrentPhase == GamePhase.Train)
        {
            XpForNextPhase = xpLevels[levelIndex].trainPhaseXP;
        }
        else
        {
            XpForNextPhase = xpLevels[levelIndex].stationPhaseXP;
        }
    }
    public void ApplySaveData(PlayerSaveData data)
    {
        if (data == null) // ����� ����
        {
            Initialize(); // ������ ���������� �� ��������� ��������
        }
        else
        {
            CurrentLevel = data.currentLevel;
            CurrentPhase = data.currentPhase;
            CurrentXP = data.currentXP;
            UpdateXpThreshold();
        }
        OnXPChanged?.Invoke(CurrentXP, XpForNextPhase);
    }

}