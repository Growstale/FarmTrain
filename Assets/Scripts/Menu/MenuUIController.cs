using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MenuUIController : MonoBehaviour
{
    [Header("Кнопки главного меню")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeSlotsButton;

    [Header("Панель слотов")]
    [SerializeField] private GameObject saveSlotsPanel;

    // Сюда, как и раньше, перетащите 4 объекта-слота из иерархии
    [SerializeField] private GameObject[] slotContainers;

    private bool isNewGameMode = false;

    void Start()
    {
        newGameButton.onClick.AddListener(() => OpenSlotsMenu(true));
        continueButton.onClick.AddListener(() => OpenSlotsMenu(false));
        closeSlotsButton.onClick.AddListener(() => saveSlotsPanel.SetActive(false));

        saveSlotsPanel.SetActive(false);
        // Небольшая задержка, чтобы SaveLoadManager точно успел загрузиться
        Invoke(nameof(RefreshContinueButtonState), 0.1f);
    }

    private void OpenSlotsMenu(bool isNewGame)
    {
        isNewGameMode = isNewGame;
        saveSlotsPanel.SetActive(true);
        RefreshSlotsUI();
    }

    private void RefreshContinueButtonState()
    {
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("SaveLoadManager не найден! Не могу обновить UI.");
            return;
        }
        var allMetaData = SaveLoadManager.Instance.GetAllSlotsMetadata();
        continueButton.interactable = allMetaData.slots.Any(s => s.isUsed);
    }

    private void RefreshSlotsUI()
    {
        var allMetaData = SaveLoadManager.Instance.GetAllSlotsMetadata();

        for (int i = 0; i < slotContainers.Length; i++)
        {
            // Находим на каждом слоте наш новый скрипт SaveSlotUI
            SaveSlotUI slotUIScript = slotContainers[i].GetComponent<SaveSlotUI>();
            if (slotUIScript != null)
            {
                // Передаем ему всю необходимую информацию для самонастройки
                slotUIScript.Setup(allMetaData.slots[i], i, isNewGameMode, OnSlotClicked);
            }
        }
    }

    private void OnSlotClicked(int slotId)
    {
        saveSlotsPanel.SetActive(false); // Прячем панель

        if (isNewGameMode)
        {
            SaveLoadManager.Instance.StartNewGame(slotId);
        }
        else
        {
            SaveLoadManager.Instance.LoadGame(slotId);
        }
    }
}