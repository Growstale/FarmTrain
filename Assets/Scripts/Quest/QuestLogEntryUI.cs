using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class QuestLogEntryUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image checkmarkImage;
    [SerializeField] private Button pinButton; // ���� ������-�������
    [SerializeField] private Image pinIcon; // ������ �� ���� ������
    [SerializeField] private Button mainButton; // �������� ������, �� ������� �������

    [Header("Pin Sprites")]
    [SerializeField] private Sprite pinnedSprite; // ������ ������������ �������
    [SerializeField] private Sprite unpinnedSprite; // ������ ������� �������
    [SerializeField] private Sprite pinHighlightedSprite;

    [Header("Text Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material disabledMaterial;

    private Quest assignedQuest;
    private Action<Quest> onSelectCallback;
    private bool isPointerInside;
    private bool isPointerDown;

    private void Awake()
    {
        if (titleText == null)
            titleText = GetComponentInChildren<TextMeshProUGUI>();

        if (titleText != null)
            titleText.raycastTarget = false;
    }

    public void Setup(Quest quest, Action<Quest> selectCallback)
    {
        assignedQuest = quest;
        onSelectCallback = selectCallback;

        titleText.text = quest.title;
        titleText.fontMaterial = normalMaterial;

        // ����������� ��������� ��������� � ����������� �� ������� ������
        bool isCompleted = quest.status == QuestStatus.Completed;
        checkmarkImage.gameObject.SetActive(isCompleted);
        pinButton.gameObject.SetActive(!isCompleted); // ������� ���������� ������ ��� ��������

        if (isCompleted)
        {
            titleText.fontMaterial = disabledMaterial;
            mainButton.interactable = false;
        }
        else
        {
            titleText.fontMaterial = normalMaterial;
            mainButton.interactable = true;
            pinIcon.sprite = quest.isPinned ? pinnedSprite : unpinnedSprite;
        }

        // ����������� ���������� �������
        mainButton.onClick.RemoveAllListeners();
        mainButton.onClick.AddListener(HandleSelection);

        pinButton.onClick.RemoveAllListeners();
        pinButton.onClick.AddListener(HandlePinning);
    }

    // ���� �� �������� ����� ������ - ������� ����� ��� ������ �������
    private void HandleSelection()
    {
        onSelectCallback?.Invoke(assignedQuest);
    }

    // ���� �� ������� - ���������/���������
    private void HandlePinning()
    {
       bool newPinnedState = !assignedQuest.isPinned;
        QuestManager.Instance.PinQuest(assignedQuest);
        UpdatePinIcon(newPinnedState);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainButton.interactable)
        {
            titleText.fontMaterial = selectedMaterial;
            pinIcon.sprite = pinnedSprite;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mainButton.interactable)
        {
            titleText.fontMaterial = isPointerInside ? highlightedMaterial : normalMaterial;
            UpdatePinIcon(assignedQuest.isPinned);
        }
    }

    private void UpdatePinIcon(bool isPinned)
    {
        if (isPointerDown)
        {
            pinIcon.sprite = pinnedSprite;
        }
        else if (isPointerInside)
        {
            pinIcon.sprite = pinHighlightedSprite;
        }
        else
        {
            pinIcon.sprite = isPinned ? pinnedSprite : unpinnedSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        if (mainButton.interactable)
        {
            titleText.fontMaterial = highlightedMaterial;
            UpdatePinIcon(assignedQuest.isPinned);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        if (mainButton.interactable)
        {
            titleText.fontMaterial = normalMaterial;
            UpdatePinIcon(assignedQuest.isPinned);
        }
    }
}