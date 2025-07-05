using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StallCameraController : MonoBehaviour
{
    public static StallCameraController Instance { get; private set; }

    [Header("Stall Setup")]
    public List<Transform> stalls;
    public int startingStallIndex = 0;

    [Header("Camera Settings")]
    public float transitionSpeed = 5f;
    public float zoomInSize = 5f;
    public float zoomOutSize = 10f;

    [Header("Panning Settings (Overview)")]
    public float panSpeed = 50f;
    public float minPanX = 10f;
    public float maxPanX = 70f;

    private Camera mainCamera;
    private Vector3 targetPosition;
    private float targetOrthographicSize;
    private int currentStallIndex = -1;
    private int lastStallIndex = 0;
    private bool isOverview = true;
    private bool isPanning = false;
    private Vector3 panOrigin;

    private UnityEngine.EventSystems.EventSystem eventSystem;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();

        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("StallCameraController требует ортографическую камеру!");
            enabled = false;
            return;
        }

        if (stalls == null || stalls.Count == 0)
        {
            Debug.LogError("Список ларьков (stalls) пуст!");
            enabled = false;
            return;
        }

        stalls = stalls.OrderBy(s => s.position.x).ToList();

        if (startingStallIndex < 0 || startingStallIndex >= stalls.Count)
        {
            startingStallIndex = 0;
        }

        lastStallIndex = startingStallIndex;
        EnterOverviewMode(true);
        transform.position = GetTargetPositionForStall(startingStallIndex);
        mainCamera.orthographicSize = targetOrthographicSize;
        CalculatePanLimits();
        ConfigureStallsForCurrentLevel();
    }

    void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        HandleInput();
        SmoothCameraMovement();
    }

    void HandleInput()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f && !isOverview)
        {
            EnterOverviewMode();
        }
        else if (scroll > 0f && isOverview)
        {
            if (lastStallIndex >= 0 && lastStallIndex < stalls.Count)
            {
                FocusOnStall(lastStallIndex);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        HandlePanning();
    }

    void HandleLeftClick()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

        if (hit.collider == null)
        {
            if (!isOverview)
            {
                EnterOverviewMode();
            }
            return;
        }

        StallInteraction clickedStall = hit.collider.GetComponentInParent<StallInteraction>();

        if (clickedStall != null)
        {
            int clickedIndex = stalls.IndexOf(clickedStall.transform);
            if (clickedIndex != -1 && currentStallIndex != clickedIndex)
            {
                FocusOnStall(clickedIndex);
            }
        }
        else
        {
            if (!isOverview)
            {
                EnterOverviewMode();
            }
        }
    }

    void FocusOnStall(int index)
    {
        isOverview = false;
        isPanning = false;
        currentStallIndex = index;
        lastStallIndex = index;

        targetPosition = GetTargetPositionForStall(currentStallIndex);
        targetOrthographicSize = zoomInSize;

        Debug.Log($"Фокусируемся на ларьке {stalls[currentStallIndex].name}");

        stalls[currentStallIndex].GetComponent<StallInteraction>()?.OpenShopUI();
    }

    public void EnterOverviewMode(bool isFirstLoad = false)
    {
        if (isOverview && !isFirstLoad) return;

        Debug.Log("Переход в режим обзора");

        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.CloseShop();
        }

        isOverview = true;
        currentStallIndex = -1;
        targetOrthographicSize = zoomOutSize;

        targetPosition = new Vector3(transform.position.x, GetBaseYPosition(), transform.position.z);
    }

    void HandlePanning()
    {
        if (!isOverview)
        {
            isPanning = false;
            return;
        }
        if (Input.GetMouseButtonDown(1))
        {
            isPanning = true;
            panOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(1) && isPanning)
        {
            Vector3 difference = panOrigin - mainCamera.ScreenToWorldPoint(Input.mousePosition);
            float clampedX = Mathf.Clamp(transform.position.x + difference.x, minPanX, maxPanX);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            targetPosition = transform.position;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
    }

    void SmoothCameraMovement()
    {
        if (!isPanning)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, transitionSpeed * Time.deltaTime);
        }
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, transitionSpeed * Time.deltaTime);
    }

    Vector3 GetTargetPositionForStall(int index)
    {
        if (index >= 0 && index < stalls.Count)
        {
            return new Vector3(stalls[index].position.x, GetBaseYPosition(), transform.position.z);
        }
        return transform.position;
    }

    float GetBaseYPosition()
    {
        return stalls.Count > 0 ? stalls[0].position.y : 0f;
    }

    void CalculatePanLimits()
    {
        if (stalls.Count == 0) return;
        minPanX = stalls[0].position.x;
        maxPanX = stalls[stalls.Count - 1].position.x;
    }
    private void ConfigureStallsForCurrentLevel()
    {
        if (ExperienceManager.Instance == null || StationDatabase.Instance == null)
        {
            Debug.LogError("ExperienceManager или StationDatabase не найдены!");
            return;
        }

        int currentLevel = ExperienceManager.Instance.CurrentLevel;
        StationData stationData = StationDatabase.Instance.GetStationDataById(currentLevel);

        if (stationData == null)
        {
            Debug.LogError($"Не найдены данные для станции уровня: {currentLevel}!");
            return;
        }

        if (stalls.Count != stationData.stallInventories.Count)
        {
            Debug.LogWarning($"Количество ларьков на сцене ({stalls.Count}) не совпадает с количеством инвентарей в StationData ({stationData.stallInventories.Count})!");
        }

        for (int i = 0; i < stalls.Count; i++)
        {
            if (i < stationData.stallInventories.Count)
            {
                var stallInteraction = stalls[i].GetComponent<StallInteraction>();
                if (stallInteraction != null)
                {
                    stallInteraction.shopInventoryData = stationData.stallInventories[i];
                    // Важно! Нужно переинициализировать магазин в менеджере, так как данные могли поменяться
                    ShopDataManager.Instance.InitializeShop(stallInteraction.shopInventoryData);
                }
            }
        }

        Debug.Log($"Станция настроена для '{stationData.stationName}' (Уровень {currentLevel})");
    }

}