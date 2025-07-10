using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;


public class TrainCameraController : MonoBehaviour
{
    [Header("Wagon Setup")]
    public List<Transform> wagons;
    public int startingWagonIndex = 1;

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
    private int currentWagonIndex = -1;
    private int lastWagonIndex = 1;
    private bool isOverview = false;

    private bool isPanning = false;
    private Vector3 panOrigin;

    [Header("Dependencies")]
    [SerializeField] private LocomotiveController locomotiveController;
    [Header("Audio")]
    [SerializeField] public AudioSource audioSource;
    [SerializeField] private AudioClip zoomInSound;   // звук приближения
    [SerializeField] private AudioClip zoomOutSound;  // звук отдаления

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("TrainCameraController requires an Orthographic Camera component on the same GameObject");
            enabled = false;
            return;
        }

        wagons = wagons.OrderBy(w => w.position.x).ToList();

        if (startingWagonIndex <= 0 || startingWagonIndex >= wagons.Count)
        {
            Debug.LogWarning($"Starting wagon index ({startingWagonIndex}) is invalid or points to locomotive");
            startingWagonIndex = 1;
        }
        if (wagons.Count <= 1)
        {
            Debug.LogError("Not enough wagons assigned to the TrainCameraController");
            enabled = false;
            return;
        }

        currentWagonIndex = startingWagonIndex;
        lastWagonIndex = currentWagonIndex;
        isOverview = false;
        targetPosition = GetTargetPositionForWagon(currentWagonIndex);
        targetOrthographicSize = zoomInSize;

        transform.position = targetPosition;
        mainCamera.orthographicSize = targetOrthographicSize;

        CalculatePanLimits();

        locomotiveController = FindObjectOfType<LocomotiveController>();
        if (locomotiveController == null)
        {
            Debug.LogError("TrainCameraController не смог найти LocomotiveController на сцене!", this);
        }

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
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f && !isOverview) EnterOverviewMode();
        else if (scroll > 0f && isOverview) ExitOverviewMode(lastWagonIndex);

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        HandlePanning();
    }

    // ===================================================================
    // НОВАЯ, УЛУЧШЕННАЯ ЛОГИКА ОБРАБОТКИ КЛИКОВ
    // ===================================================================

    // ЗАМЕНИТЕ ВАШ СТАРЫЙ HandleLeftClick НА ЭТОТ

    void HandleLeftClick()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
        {
            return;
        }

        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] allHits = Physics2D.RaycastAll(worldPoint, Vector2.zero);

        if (allHits.Length == 0) return;

        if (isOverview)
        {
            // В режиме обзора ищем ТОЛЬКО вагоны (эта часть работает правильно)
            foreach (var hit in allHits)
            {
                if (hit.collider.CompareTag("Wagon"))
                {
                    int clickedWagonIndex = wagons.IndexOf(hit.transform);
                    if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count)
                    {
                        Debug.Log($"<color=cyan>[Overview Click]</color> Клик по вагону {clickedWagonIndex}. Приближаемся.");
                        ExitOverviewMode(clickedWagonIndex);
                        return;
                    }
                }
            }
            Debug.Log("[Overview Click] Клик был, но не по вагону.");
        }
        else
        {
            // В режиме фокуса ищем самый верхний интерактивный объект
            System.Array.Sort(allHits, (hit1, hit2) => {
                SpriteRenderer r1 = hit1.collider.GetComponentInParent<SpriteRenderer>();
                SpriteRenderer r2 = hit2.collider.GetComponentInParent<SpriteRenderer>();
                if (r1 == null && r2 == null) return 0;
                if (r1 == null) return 1;
                if (r2 == null) return -1;
                if (r1.sortingLayerID != r2.sortingLayerID) return r2.sortingLayerID.CompareTo(r1.sortingLayerID);
                if (r1.sortingOrder != r2.sortingOrder) return r2.sortingOrder.CompareTo(r1.sortingOrder);
                return 0;
            });

            // Проходимся по всем отсортированным хитам, пока не найдем что-то, с чем можно взаимодействовать
            foreach (var hit in allHits)
            {
                if (TryHandleHornClick(hit)) return; // Если кликнули по гудку, выходим

                // Сначала проверяем на конкретные интерактивные объекты
                if (TryHandleAnimalClick(hit)) return;
                if (TryHandleItemClick(hit)) return;
                if (TryHandleSlotClick(hit)) return;
                if (TryHandlePlantClick(hit)) return;
                // !!! ВОТ ИСПРАВЛЕНИЕ !!!
                // Если ничего из вышеперечисленного не сработало, то для ЭТОГО ЖЕ объекта
                // проверяем, не принадлежит ли он соседнему вагону.
                // Это наш "запасной" вариант.
                if (TryHandleAdjacentWagonClick(hit)) return;
            }
        }
    }

    // --- Вспомогательные методы для HandleLeftClick ---

    private bool TryHandleAnimalClick(RaycastHit2D hit)
    {
        AnimalController clickedAnimal = hit.collider.GetComponent<AnimalController>();
        if (clickedAnimal == null) return false;

        Transform parentWagon = FindParentWagon(clickedAnimal.transform);
        if (parentWagon == null) return false;

        int animalWagonIndex = wagons.IndexOf(parentWagon);
        if (animalWagonIndex < 1) return false;

        if (animalWagonIndex == currentWagonIndex)
        {
            Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in current wagon.");
            clickedAnimal.AttemptInteraction();
            return true;
        }

        if (Mathf.Abs(animalWagonIndex - currentWagonIndex) == 1)
        {
            Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in adjacent wagon {animalWagonIndex}. Moving camera.");
            MoveToWagon(animalWagonIndex);
            return true;
        }
        return false;
    }

    private bool TryHandleItemClick(RaycastHit2D hit)
    {
        ItemPickup clickedItem = hit.collider.GetComponent<ItemPickup>();
        if (clickedItem == null) return false;

        Transform parentWagon = FindParentWagon(clickedItem.transform);
        if (parentWagon == null) return false;

        int itemWagonIndex = wagons.IndexOf(parentWagon);
        if (itemWagonIndex < 1) return false;

        if (itemWagonIndex == currentWagonIndex)
        {
            Debug.Log($"Attempting pickup on {clickedItem.name} in current wagon {currentWagonIndex}");
            clickedItem.AttemptPickup();
            return true;
        }

        if (Mathf.Abs(itemWagonIndex - currentWagonIndex) == 1)
        {
            Debug.Log($"Clicked item {clickedItem.name} in adjacent wagon {itemWagonIndex}. Moving camera.");
            MoveToWagon(itemWagonIndex);
            return true;
        }
        return false;
    }

    private bool TryHandleHornClick(RaycastHit2D hit)
    {
        // Проверяем, что ссылка на контроллер есть и что мы попали именно в объект гудка
        // Теперь мы читаем публичное свойство hornObject
        if (locomotiveController != null && hit.collider.gameObject == locomotiveController.hornObject)
        {
            locomotiveController.OnHornClicked();
            return true;
        }
        return false;
    }


    private bool TryHandleSlotClick(RaycastHit2D hit)
    {
        
        if (!hit.collider.CompareTag("Slot")) { Debug.Log($"Object {hit.collider.name} dont have tag slot "); return false; }
        //
        
        SlotScripts slotScripts = hit.collider.GetComponent<SlotScripts>();


        if (slotScripts == null) {  return false; }
            Transform parentWagon = FindParentWagon(slotScripts.transform);
        if (parentWagon == null) { return false; }




        int bedWagonIndex = wagons.IndexOf(parentWagon);
        if (bedWagonIndex < 1) return false;

        if (bedWagonIndex == currentWagonIndex)
        {
            Debug.Log($"Clicked slot in current wagon {currentWagonIndex}.");
            Debug.Log($"Clicked on object {hit.collider.gameObject}");
            slotScripts.PlantSeeds();
            return true;
        }

        if (Mathf.Abs(bedWagonIndex - currentWagonIndex) == 1)
        {
            Debug.Log($"Clicked slot in adjacent wagon {bedWagonIndex}. Moving camera.");
            MoveToWagon(bedWagonIndex);
            return true;
        }
        return false;
    }
    private bool TryHandlePlantClick(RaycastHit2D hit)
    {

        if (!hit.collider.CompareTag("Plant")) { Debug.Log($"Object {hit.collider.name} dont have tag plant "); return false; }
      
        
         PlantController plantController= hit.collider.GetComponent<PlantController>();


        if (plantController == null) { return false; }
        Transform parentWagon = FindParentWagon(plantController.transform);
        if (parentWagon == null) { return false; }


        int bedWagonIndex = wagons.IndexOf(parentWagon);
        if (bedWagonIndex < 1) return false;

        if (bedWagonIndex == currentWagonIndex)
        {
            Debug.Log($"Clicked plant in current wagon {currentWagonIndex}.");
            Debug.Log($"Clicked on object {hit.collider.gameObject}");
            plantController.ClickHandler();
            return true;
        }

        if (Mathf.Abs(bedWagonIndex - currentWagonIndex) == 1)
        {
            Debug.Log($"Clicked plant in adjacent wagon {bedWagonIndex}. Moving camera.");
            MoveToWagon(bedWagonIndex);
            return true;
        }
        return false;
    }

    private bool TryHandleAdjacentWagonClick(RaycastHit2D hit)
    {
        Transform parentWagon = FindParentWagon(hit.transform);
        if (parentWagon == null) return false;

        int clickedWagonIndex = wagons.IndexOf(parentWagon);
        if (clickedWagonIndex < 1) return false;

        if (clickedWagonIndex != currentWagonIndex && Mathf.Abs(clickedWagonIndex - currentWagonIndex) == 1)
        {
            Debug.Log($"Clicked on non-interactive object '{hit.collider.name}' in adjacent wagon {clickedWagonIndex}. Moving camera.");
            MoveToWagon(clickedWagonIndex);
            return true;
        }

        return false;
    }

    // ===================================================================
    // ОСТАЛЬНАЯ ЧАСТЬ КОДА (без изменений)
    // ===================================================================

    private Transform FindParentWagon(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (wagons.Contains(current))
            {
                return current;
            }
            current = current.parent;
        }
        return null;
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
            panOrigin = transform.position;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            panOrigin -= mouseWorldPos;
        }
        if (Input.GetMouseButton(1) && isPanning)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 desiredPos = panOrigin + mouseWorldPos;
            float clampedX = Mathf.Clamp(desiredPos.x, minPanX, maxPanX);
            targetPosition = new Vector3(clampedX, targetPosition.y, targetPosition.z);
        }
        if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
    }

    void EnterOverviewMode()
    {
        isOverview = true;
        lastWagonIndex = currentWagonIndex;
        currentWagonIndex = -1;
        targetOrthographicSize = zoomOutSize;
        targetPosition = GetTargetPositionForWagon(lastWagonIndex);
        CalculatePanLimits();
        if (audioSource != null && zoomOutSound != null)
        {
            audioSource.PlayOneShot(zoomOutSound);
        }
    }

    void ExitOverviewMode(int targetIndex)
    {
        if (targetIndex <= 0 || targetIndex >= wagons.Count)
        {
            Debug.LogWarning($"Cannot focus on invalid index {targetIndex}. Returning to last valid wagon {lastWagonIndex}");
            targetIndex = lastWagonIndex;
            if (targetIndex <= 0) targetIndex = 1;
        }
        isOverview = false;
        isPanning = false;
        currentWagonIndex = targetIndex;
        lastWagonIndex = currentWagonIndex;
        targetOrthographicSize = zoomInSize;
        targetPosition = GetTargetPositionForWagon(currentWagonIndex);
        Debug.Log($"Exiting Overview Mode, focusing on Wagon {currentWagonIndex}");
        if (audioSource != null && zoomInSound != null)
        {
            audioSource.PlayOneShot(zoomInSound);
        }
    }

    void MoveToWagon(int index)
    {
        if (index > 0 && index < wagons.Count && !isOverview)
        {
            currentWagonIndex = index;
            lastWagonIndex = currentWagonIndex;
            targetPosition = GetTargetPositionForWagon(currentWagonIndex);
            Debug.Log($"Moving focus to Wagon {currentWagonIndex}");
        }
        else
        {
            Debug.LogWarning($"Cannot move to index {index}. It might be the locomotive, out of bounds, or in overview mode.");
        }
        if (audioSource != null && zoomInSound != null)
        {
            audioSource.PlayOneShot(zoomInSound);
        }

    }

    void SmoothCameraMovement()
    {
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, transitionSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        float smoothedSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, transitionSpeed * Time.deltaTime);
        mainCamera.orthographicSize = smoothedSize;
    }

    Vector3 GetTargetPositionForWagon(int index)
    {
        if (index >= 0 && index < wagons.Count)
        {
            return new Vector3(wagons[index].position.x, GetBaseYPosition(), transform.position.z);
        }
        return transform.position;
    }

    float GetBaseYPosition()
    {
        return wagons.Count > 0 ? wagons[0].position.y : 0f;
    }

    void CalculatePanLimits()
    {
        if (wagons.Count == 0) return;
        minPanX = wagons[0].position.x;
        maxPanX = wagons[wagons.Count - 1].position.x;
        Debug.Log($"Calculated Pan Limits: MinX={minPanX}, MaxX={maxPanX}");
    }

    // --- Публичные методы и геттеры ---
    public bool IsInOverview() => isOverview;
    public int GetCurrentWagonIndex() => isOverview ? -1 : currentWagonIndex;
    public Transform GetCurrentWagonTransform()
    {
        if (!isOverview && currentWagonIndex > 0 && currentWagonIndex < wagons.Count)
        {
            return wagons[currentWagonIndex];
        }
        return null;
    }
    public Transform GetWagonOwnerOfPosition(float worldXPosition)
    {
        if (wagons == null || wagons.Count <= 1) return null;
        for (int i = 1; i < wagons.Count; i++)
        {
            Transform currentWagon = wagons[i];
            Transform previousWagon = wagons[i - 1];
            float leftBoundary = previousWagon.position.x + (currentWagon.position.x - previousWagon.position.x) / 2.0f;
            float rightBoundary;
            if (i == wagons.Count - 1)
            {
                rightBoundary = float.PositiveInfinity;
            }
            else
            {
                Transform nextWagon = wagons[i + 1];
                rightBoundary = currentWagon.position.x + (nextWagon.position.x - currentWagon.position.x) / 2.0f;
            }
            if (worldXPosition >= leftBoundary && worldXPosition < rightBoundary)
            {
                return currentWagon;
            }
        }
        return null;
    }
    public bool AssignParentWagonByPosition(Transform itemTransform, Vector3 spawnPosition)
    {
        Transform parentWagon = GetWagonOwnerOfPosition(spawnPosition.x);
        if (parentWagon != null)
        {
            itemTransform.SetParent(parentWagon, true);
            return true;
        }
        return false;
    }
}