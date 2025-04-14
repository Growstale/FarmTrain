using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrainCameraController : MonoBehaviour
{
    [Header("Wagon Setup")]
    public List<Transform> wagons; // Список всех вагонов (включая локомотив)
    public int startingWagonIndex = 1; // Индекс вагона, с которого начинаем

    [Header("Camera Settings")]
    public float transitionSpeed = 5f; // Скорость перемещения и зума
    public float zoomInSize = 5f;  // Размер камеры для крупного плана
    public float zoomOutSize = 10f; // Размер камеры для общего вида

    [Header("Panning Settings (Overview)")]
    public float panSpeed = 50f;   // Скорость перемещения камеры при зажатой правой кнопке мыши в режиме обзора
    public float minPanX = 10f;    // Минимальная позиция X при панорамировании
    public float maxPanX = 70f;    // Максимальная позиция X при панорамировании

    private Camera mainCamera;
    private Vector3 targetPosition; // Целевая позиция, к которой камера будет плавно двигаться
    private float targetOrthographicSize; // Целевой ортографический размер, к которому камера будет плавно стремиться
    private int currentWagonIndex = -1; // Индекс вагона, на котором камера СЕЙЧАС сфокусирована
    private int lastWagonIndex = 1;    // Запоминаем индекс вагона, на котором камера была сфокусирована ПОСЛЕДНИЙ РАЗ перед переходом в режим обзора
    private bool isOverview = false; // Флаг (переключатель): true - камера в режиме обзора, false - камера сфокусирована на вагоне

    // Panning State
    private bool isPanning = false; // Флаг: true - игрок сейчас зажал правую кнопку мыши и двигает камеру
    private Vector3 panOrigin; // Вспомогательная переменная для расчета смещения при панорамировании

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogError("TrainCameraController requires an Orthographic Camera component on the same GameObject");
            enabled = false;
            return;
        }

        // Сортируем вагоны по их X-координате (слева направо)
        wagons = wagons.OrderBy(w => w.position.x).ToList();

        // Проверка на валидность стартового индекса
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

        // Начальная установка
        currentWagonIndex = startingWagonIndex; // Фокус на первом вагоне
        lastWagonIndex = currentWagonIndex; // На каком вагоне был последний фокус
        isOverview = false; // Режим фокуса
        targetPosition = GetTargetPositionForWagon(currentWagonIndex); // Целевая позиция - текущая
        targetOrthographicSize = zoomInSize; // Целевой зум - текущий

        transform.position = targetPosition;
        mainCamera.orthographicSize = targetOrthographicSize;

        CalculatePanLimits(); // Рассчитываем границы панорамирования
    }

    void Update()
    {
        HandleInput(); // Обработка ввода пользователя
        SmoothCameraMovement(); // Плавное перемещение камеры к целевой позиции и целевому зуму
    }

    void HandleInput() // Обработка ввода пользователя
    {
        // --- Обработка колесика мыши ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0f && !isOverview) // Отдаление в режиме фокуса
        {
            EnterOverviewMode(); // Переход в режим обзора
        }
        else if (scroll > 0f && isOverview) // Приближение в режиме фокуса
        {
            ExitOverviewMode(lastWagonIndex); // Возвращаемся к последнему вагону
        }

        // --- Обработка кликов мыши ---
        if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
        {
            HandleLeftClick();
        }

        // --- Обработка Панорамирования (Правая Кнопка Мыши) ---
        HandlePanning();
    }
     
    void HandleLeftClick() // Обработка кликов мыши
    {
        // Выпускаем луч из камеры в точку экрана, где кликнула мышь
        RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero); 

        if (hit.collider != null) // Луч попал в какой-то коллайдер
        {
            if (hit.collider.CompareTag("Wagon")) // Кликнули по вагону
            {
                int clickedWagonIndex = wagons.IndexOf(hit.transform);

                if (isOverview) // Режим обзора
                {
                    if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count) // Валидность индекса вагона
                    {
                        ExitOverviewMode(clickedWagonIndex); // Переход к режиму фокуса на выбранный вагон
                    }
                }
                else // Режим фокуса
                {
                    // Если кликнули на видимый соседний вагон
                    if (clickedWagonIndex == currentWagonIndex + 1 || clickedWagonIndex == currentWagonIndex - 1)
                    {
                        if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count) // Валидность индекса вагона
                        {
                            MoveToWagon(clickedWagonIndex); // Переход к выбранному вагону
                        }
                    }
                }
            }
        }
    }

    void HandlePanning() // Обрабатывает панорамирование камеры с помощью правой кнопки мыши
    {
        // Функция работает только в режиме обзора

        if (!isOverview)
        {
            isPanning = false; // Убедимся, что флаг сброшен
            return;
        }

        if (Input.GetMouseButtonDown(1)) // Правая кнопка нажата
        {
            isPanning = true;
            // Запоминаем начальную позицию камеры (точка отсчёта)
            panOrigin = transform.position;
            // Запоминаем позицию мыши в МСК
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            panOrigin -= mouseWorldPos; // Вычисляем и сохраняем СМЕЩЕНИЕ между камерой и точкой клика мыши

            // Это делается для эффекта, будто мы схватили мир в точке клика

        }

        if (Input.GetMouseButton(1) && isPanning) 
        {
            // Рассчитываем желаемую позицию камеры 
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 desiredPos = panOrigin + mouseWorldPos;

            // Ограничиваем движение по X (камера не может уехать левее minPanX и правее maxPanX)
            float clampedX = Mathf.Clamp(desiredPos.x, minPanX, maxPanX);

            // Обновляем только X координату целевой позиции
            targetPosition = new Vector3(clampedX, targetPosition.y, targetPosition.z);
        }

        if (Input.GetMouseButtonUp(1)) // Правая кнопка отпущена
        {
            isPanning = false;
        }
    }


    void EnterOverviewMode() // Переход в режим обзора
    {
        isOverview = true;
        lastWagonIndex = currentWagonIndex; // Запомнить, откуда ушли
        currentWagonIndex = -1; // Нет фокуса
        targetOrthographicSize = zoomOutSize; // Устанавливаем целевой размер для отдаления

        targetPosition = GetTargetPositionForWagon(lastWagonIndex); // Остаёмся на позиции последнего вагона, но с большим зумом
        CalculatePanLimits(); // Пересчитать лимиты при изменении зума
    }

    void ExitOverviewMode(int targetIndex) // Переход в режим фокуса
    {
        if (targetIndex <= 0 || targetIndex >= wagons.Count) // Валидность индекса вагона
        {
            Debug.LogWarning($"Cannot focus on invalid index {targetIndex}. Returning to last valid wagon {lastWagonIndex}");
            targetIndex = lastWagonIndex; // Возврат к последнему валидному
            if (targetIndex <= 0) targetIndex = 1; // Крайний случай, если lastWagonIndex был некорректным
        }

        isOverview = false;
        isPanning = false; // Выключаем панорамирование при выходе из обзора
        currentWagonIndex = targetIndex;
        lastWagonIndex = currentWagonIndex;
        targetOrthographicSize = zoomInSize;
        targetPosition = GetTargetPositionForWagon(currentWagonIndex);
        Debug.Log($"Exiting Overview Mode, focusing on Wagon {currentWagonIndex}");
    }

    void MoveToWagon(int index) // Устанавливает цель для плавного перехода камеры к вагону с заданным индексом
    {
        if (index > 0 && index < wagons.Count && !isOverview) // Валидность индекса вагона
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

    void SmoothCameraMovement() // Выполняет плавное движение и зум камеры каждый кадр
    {
        // Плавное перемещение
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, transitionSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Плавный зум
        float smoothedSize = Mathf.Lerp(mainCamera.orthographicSize, targetOrthographicSize, transitionSpeed * Time.deltaTime);
        mainCamera.orthographicSize = smoothedSize;
    }

    // Вспомогательная функция для получения целевой позиции по центру вагона
    Vector3 GetTargetPositionForWagon(int index)
    {
        if (index >= 0 && index < wagons.Count) // Валидность индекса вагона
        {
            return new Vector3(wagons[index].position.x, GetBaseYPosition(), transform.position.z);
        }
        return transform.position;
    }

    // Определяем базовую Y-позицию камеры
    float GetBaseYPosition()
    {
        // Y первого вагона 
        return wagons.Count > 0 ? wagons[0].position.y : 0f;
    }


    void CalculatePanLimits()     // Рассчитывает границы для панорамирования в режиме обзора
    {
        if (wagons.Count == 0) return;


        // Ограничиваем ЦЕНТР камеры позициями самого левого и самого правого вагонов
        minPanX = wagons[0].position.x; // Центр камеры не левее локомотива
        maxPanX = wagons[wagons.Count - 1].position.x; // Центр камеры не правее последнего вагона

        Debug.Log($"Calculated Pan Limits: MinX={minPanX}, MaxX={maxPanX}");
    }


    // --- Публичные методы для возможного использования из других скриптов ---

    public bool IsInOverview()
    {
        return isOverview;
        // true - камера в режиме обзора, false - камера сфокусирована на вагоне
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
}