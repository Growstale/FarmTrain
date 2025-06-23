using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    }

    void Update()
    {
        HandleInput();
        SmoothCameraMovement();
    }

    void HandleInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f && !isOverview)
        {
            EnterOverviewMode();
        }
        else if (scroll > 0f && isOverview)
        {
            ExitOverviewMode(lastWagonIndex);
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        HandlePanning();
    }

    void HandleLeftClick()
    {
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.zero);

        if (hits.Length == 0) return;

        System.Array.Sort(hits, (hit1, hit2) =>
        {
            SpriteRenderer r1 = hit1.collider.GetComponentInParent<SpriteRenderer>();
            SpriteRenderer r2 = hit2.collider.GetComponentInParent<SpriteRenderer>();
            if (r1 == null && r2 == null) return 0;
            if (r1 == null) return 1;
            if (r2 == null) return -1;
            if (r1.sortingLayerID != r2.sortingLayerID) return r2.sortingLayerID.CompareTo(r1.sortingLayerID);
            if (r1.sortingOrder != r2.sortingOrder) return r2.sortingOrder.CompareTo(r1.sortingOrder);
            return 0;
        });

        RaycastHit2D topHit = hits[0];
        Transform topHitTransform = topHit.transform;
        Collider2D topHitCollider = topHit.collider;

        if (!isOverview)
        {
            ItemPickup clickedItem = topHitCollider.GetComponent<ItemPickup>();
            if (clickedItem != null)
            {
                Transform parentWagon = FindParentWagon(clickedItem.transform);
                if (parentWagon != null)
                {
                    int itemWagonIndex = wagons.IndexOf(parentWagon);
                    if (itemWagonIndex == currentWagonIndex)
                    {
                        clickedItem.AttemptPickup();
                        Debug.Log($"Attempting pickup on {clickedItem.name} in current wagon {currentWagonIndex}");
                        return;
                    }
                    else if (itemWagonIndex > 0 && itemWagonIndex < wagons.Count && Mathf.Abs(itemWagonIndex - currentWagonIndex) == 1)
                    {
                        Debug.Log($"Clicked item {clickedItem.name} in adjacent wagon {itemWagonIndex}. Moving camera.");
                        MoveToWagon(itemWagonIndex);
                        return;
                    }
                    else
                    {
                        Debug.Log($"Clicked item {clickedItem.name} in non-adjacent wagon {itemWagonIndex}. Ignoring.");
                        return;
                    }
                }
                else { return; }
            }

            AnimalController clickedAnimal = topHitCollider.GetComponent<AnimalController>();
            if (clickedAnimal != null)
            {
                Transform parentWagon = FindParentWagon(clickedAnimal.transform);
                if (parentWagon != null)
                {
                    int animalWagonIndex = wagons.IndexOf(parentWagon);
                    if (animalWagonIndex == currentWagonIndex)
                    {
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in current wagon {currentWagonIndex}. State: {clickedAnimal.GetCurrentStateName()}");
                        if (clickedAnimal.GetCurrentStateName() == "NeedsAttention")
                        {
                            clickedAnimal.AttemptInteraction();
                        }
                        return;
                    }
                    else if (animalWagonIndex > 0 && animalWagonIndex < wagons.Count && Mathf.Abs(animalWagonIndex - currentWagonIndex) == 1)
                    {
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in adjacent wagon {animalWagonIndex}. Moving camera.");
                        MoveToWagon(animalWagonIndex);
                        return;
                    }
                    else
                    {
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in non-adjacent wagon {animalWagonIndex}. Ignoring.");
                        return;
                    }
                }
                else { return; }
            }

            if (topHitCollider.CompareTag("Bed"))
            {
                SlotScripts bedsScripts = topHitCollider.GetComponent<SlotScripts>();
                if (bedsScripts != null)
                {
                    Transform parentWagon = FindParentWagon(bedsScripts.transform);
                    if (parentWagon != null)
                    {
                        int bedWagonIndex = wagons.IndexOf(parentWagon);
                        if (bedWagonIndex == currentWagonIndex)
                        {
                            Debug.Log($"Clicked bed in current wagon {currentWagonIndex}.");
                            bedsScripts.PlantSeeds();
                            return;
                        }
                        else if (bedWagonIndex > 0 && bedWagonIndex < wagons.Count && Mathf.Abs(bedWagonIndex - currentWagonIndex) == 1)
                        {
                            Debug.Log($"Clicked bed in adjacent wagon {bedWagonIndex}. Moving camera.");
                            MoveToWagon(bedWagonIndex);
                            return;
                        }
                        else
                        {
                            Debug.Log($"Clicked bed in non-adjacent wagon {bedWagonIndex}. Ignoring.");
                            return;
                        }
                    }
                    else { return; }
                }
                else { return; }
            }

            Transform clickedObjectParentWagon = FindParentWagon(topHitTransform);
            if (clickedObjectParentWagon != null)
            {
                int clickedObjectWagonIndex = wagons.IndexOf(clickedObjectParentWagon);

                if (clickedObjectWagonIndex > 0 && clickedObjectWagonIndex < wagons.Count && Mathf.Abs(clickedObjectWagonIndex - currentWagonIndex) == 1)
                {
                    Debug.Log($"Clicked on non-interactive object '{topHitCollider.name}' belonging to adjacent wagon {clickedObjectWagonIndex}. Moving camera.");
                    MoveToWagon(clickedObjectWagonIndex);
                    return;
                }
            }

            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);
                if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count && Mathf.Abs(clickedWagonIndex - currentWagonIndex) == 1)
                {
                    Debug.Log($"Clicked directly on adjacent wagon {clickedWagonIndex}. Moving camera.");
                    MoveToWagon(clickedWagonIndex);
                    return;
                }
            }
        }
        else
        {
            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);
                if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count)
                {
                    Debug.Log($"Clicked on wagon {clickedWagonIndex} in overview mode. Zooming in.");
                    ExitOverviewMode(clickedWagonIndex);
                    return;
                }
            }
        }
    }

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

    public bool IsInOverview()
    {
        return isOverview;
    }

    public int GetCurrentWagonIndex()
    {
        return isOverview ? -1 : currentWagonIndex;
    }

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
        if (wagons == null || wagons.Count <= 1)
        {
            Debug.LogError("Список вагонов не инициализирован или содержит менее 2 элементов.");
            return null;
        }

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

        Debug.LogWarning($"Не удалось найти вагон для X-координаты: {worldXPosition}. Возможно, позиция слишком левее?");
        return null;
    }
    public bool AssignParentWagonByPosition(Transform itemTransform, Vector3 spawnPosition)
    {
        Transform parentWagon = GetWagonOwnerOfPosition(spawnPosition.x);

        if (parentWagon != null)
        {
            Debug.Log($"Назначен родительский вагон '{parentWagon.name}' для объекта '{itemTransform.name}' в позиции {spawnPosition}");
            itemTransform.SetParent(parentWagon, true);
            return true;
        }
        else
        {
            Debug.LogWarning($"Не удалось назначить родительский вагон для объекта '{itemTransform.name}' в позиции {spawnPosition}. Объект останется без родителя.");
            return false;
        }
    }
}