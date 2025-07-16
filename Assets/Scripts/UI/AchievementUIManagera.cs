using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
public class AchievementUIManagera : MonoBehaviour
{
    // --- ���� ��� ��������� � ���������� ---

    // ������ ��������� �������� ��� ��������� � �������,
    // �������� �����, ������� ���������� ��� UI �������� ��� ������ ����������.
    // ��� ������� ��������, ��� ���������� �� ������� ��������.
    [System.Serializable]
    public class AchievementUIElements
    {
        // �������, ������ ���� ���������� ������������� ���� UI �������
        public TypeOfAchivment achievementType;
        public Slider progressBar;
        public TextMeshProUGUI progressText;
        public GameObject completedOverlay; // �������������: ������� ��� ���������� ��� ����������
        public GameObject entryContainer;
        public Sprite defaultSprite; 
        public Sprite completedSprite;
    }

    // � ���������� �� ��������� ������ � ��� ������� �������� ������� ���,
    // ������� � �����. ������� ������ �� �����!
    public List<AchievementUIElements> uiElementsList;


    // --- ������ ---

    // ���������� ���� ���, ����� ������ ���������� �������� (��������, ��� �������� ����)
    private void OnEnable()
    {

        RefreshUI();
    }

    /// <summary>
    /// �������� �����, ������� ��������� ��� �������� UI �� ������ ������ �� AchievementManager.
    /// </summary>
    public void RefreshUI()
    {
        // ���������, ��� AchievementManager ��� ����������
        if (AchievementManager.instance == null)
        {
            Debug.LogError("AchievementManager �� ������! UI �� ����� ���� ��������.");
            return;
        }

        // ���������� �� ������ ������ UI ���������
        foreach (var uiElement in uiElementsList)
        {
            // ������� ������ (������������) ��� ����� ���� ����������
            AchievementData data = AchievementManager.instance.AllDataAchievement.Find(a => a.typeOfAchivment == uiElement.achievementType);
            if (data == null)
            {
                Debug.LogWarning($"�� ������� ������ ��� ���������� ���� {uiElement.achievementType}");
                continue; // ���������� ���� UI �������
            }

            // --- �������� ���������� ������ �� AchievementManager ---
            int currentProgress = AchievementManager.instance.GetProgress(uiElement.achievementType);
            bool isCompleted = AchievementManager.instance.IsCompleted(uiElement.achievementType);

            // --- ��������� ��������������� UI ���������� ---

            // ��������� �������
            if (uiElement.progressBar != null)
            {
                uiElement.progressBar.maxValue = data.goal;
                uiElement.progressBar.value = currentProgress;
            }

            // ��������� �����
            if (uiElement.progressText != null)
            {
                uiElement.progressText.text = $"{currentProgress} / {data.goal}";
            }

            // ���������� ��� �������� "�������" � ����������
            if (uiElement.completedOverlay != null)
            {
                uiElement.progressText.gameObject.SetActive(!isCompleted);
                uiElement.completedOverlay.SetActive(isCompleted);
            }

            Image entryImage = uiElement.entryContainer.GetComponent<Image>();
            if (entryImage != null)
            {
                entryImage.sprite = isCompleted ? uiElement.completedSprite : uiElement.defaultSprite;
            }
        }
        SortUIEntries();
    }
    private void SortUIEntries()
    {
        if (AchievementManager.instance == null) return;

        // ���������� LINQ ��� ���������� ������ ������ uiElementsList.
        // OrderBy ��������� ��������� �� ���������� �����.
        // � ����� ������ ���� - ��� ������ �������� (true/false) �� IsCompleted().
        // �� ��������� false (�� ���������) ���� ������, ��� true (���������),
        // ��� ��� � �����: ������������� �������� � ������ ������.
        var sortedList = uiElementsList
            .OrderBy(ui => AchievementManager.instance.IsCompleted(ui.achievementType))
            .ToList();

        // ������, ����� � ��� ���� ��������������� C# ������,
        // �� ������ ��������� ���� ������� � �������� � �������� Unity.
        foreach (var uiElement in sortedList)
        {
            // ����� SetAsLastSibling() ���������� Transform ����� ������� � ����� ������
            // �������� �������� ��� ��������.
            // ������� �� ������ ���������������� ������ � ������� ���� ����� ��� �������,
            // �� ���������� ����������� �� � ������ ������� � ��������.
            uiElement.entryContainer.transform.SetAsLastSibling();
        }
    }
}