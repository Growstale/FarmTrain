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


    void HandleLeftClick()
    {
        // Выпускаем луч из камеры
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        // Находит все 2D-коллайдеры (Collider2D), которые пересекаются с лучом, выпущенным из точки worldPoint
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.zero);

        if (hits.Length == 0)
        {
            return;
        }

        // Для каждого попадания он пытается найти SpriteRenderer (у самого объекта или у его родителя), чтобы получить информацию о слое (Layer),
        // сортировочном слое (SortLayerID) и порядке в слое (Order)
        if (hits.Length > 0)
        {
            foreach (var h in hits)
            {
                SpriteRenderer r = h.collider.GetComponentInParent<SpriteRenderer>();
            }
        }


        // Сортируем попадания
        System.Array.Sort(hits, (hit1, hit2) =>
        {
            SpriteRenderer r1 = hit1.collider.GetComponentInParent<SpriteRenderer>();
            SpriteRenderer r2 = hit2.collider.GetComponentInParent<SpriteRenderer>();
            if (r1 == null && r2 == null) return 0;
            if (r1 == null) return -1;
            if (r2 == null) return 1;
            if (r1.sortingLayerID != r2.sortingLayerID) return r2.sortingLayerID.CompareTo(r1.sortingLayerID);
            if (r1.sortingOrder != r2.sortingOrder) return r2.sortingOrder.CompareTo(r1.sortingOrder);
            return 0;
        });

        // Логируем верхнее попадание после сортировки
        RaycastHit2D topHit = hits[0];
        Transform topHitTransform = topHit.transform;
        Collider2D topHitCollider = topHit.collider;
        SpriteRenderer topRenderer = topHitCollider.GetComponentInParent<SpriteRenderer>();


        if (!isOverview) // РЕЖИМ ФОКУСА
        {
            // Проверяем, кликнули ли мы на ПРЕДМЕТ
            ItemPickup clickedItem = topHitCollider.GetComponent<ItemPickup>();
            if (clickedItem != null)
            {
                // Ищем родительский вагон
                Transform parentWagonTransform = FindParentWagon(clickedItem.transform);

                if (parentWagonTransform != null)
                {
                    int itemWagonIndex = wagons.IndexOf(parentWagonTransform);
                    // Проверка: Предмет принадлежит ТЕКУЩЕМУ вагону?
                    if (itemWagonIndex == currentWagonIndex)
                    {
                        clickedItem.AttemptPickup();
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            AnimalController clickedAnimal = topHitCollider.GetComponent<AnimalController>();
            if (clickedAnimal != null)
            {
                Debug.Log($"Кликнули на животное: {clickedAnimal.gameObject.name} с состоянием {clickedAnimal.GetCurrentStateName()}"); // Используем новый метод

                // Ищем родительский вагон животного
                Transform parentWagonTransform = FindParentWagon(clickedAnimal.transform);
                if (parentWagonTransform != null)
                {
                    int animalWagonIndex = wagons.IndexOf(parentWagonTransform);

                    // Проверяем: животное в ТЕКУЩЕМ вагоне (если не режим обзора)
                    if (isOverview || animalWagonIndex == currentWagonIndex)
                    {
                        // Проверяем, нуждается ли животное во внимании (используем новый метод)
                        if (clickedAnimal.GetCurrentStateName() == "NeedsAttention")
                        {
                            Debug.Log($"Животное {clickedAnimal.gameObject.name} нуждается во внимании. Вызываем AttemptInteraction.");
                            clickedAnimal.AttemptInteraction(); // Вызываем ПУБЛИЧНЫЙ метод из AnimalController
                            return; // Взаимодействие произошло, выходим из HandleLeftClick
                        }
                        else
                        {
                            Debug.Log($"Животное {clickedAnimal.gameObject.name} сейчас не нуждается во внимании.");
                            // Можно добавить звук клика по животному или другую реакцию
                            // ВАЖНО: Ставим return, чтобы не проверять вагон ПОД животным
                            return;
                        }
                    }
                    else
                    {
                        Debug.Log($"Клик на животном {clickedAnimal.gameObject.name}, но оно не в текущем вагоне ({animalWagonIndex} vs {currentWagonIndex})");
                        // Если животное в другом вагоне (в режиме фокуса), просто игнорируем клик
                        // ВАЖНО: Ставим return, чтобы не проверять вагон ПОД животным
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"Не удалось определить вагон для животного {clickedAnimal.gameObject.name}");
                    // Если не нашли вагон, тоже выходим, чтобы не проверять дальше
                    return;
                }
            }


            // 2. Если не кликнули на интерактивный предмет в текущем вагоне, проверяем ВАГОН
            Debug.Log($"  Checking if top hit object has 'Wagon' tag: {topHitCollider.CompareTag("Wagon")}");

            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);

                // Если кликнули на видимый СОСЕДНИЙ вагон
                if (clickedWagonIndex == currentWagonIndex + 1 || clickedWagonIndex == currentWagonIndex - 1)
                {
                    if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count)
                    {
                        MoveToWagon(clickedWagonIndex);
                        return;
                    }
                }
            }
        }
        else // РЕЖИМ ОБЗОРА (isOverview == true)
        {

            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);

                if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count)
                {
                    ExitOverviewMode(clickedWagonIndex);
                    return;
                }
                
            }

        }
    }

    // --- Вспомогательная функция FindParentWagon без изменений ---
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
    public Transform GetWagonOwnerOfPosition(float worldXPosition)
    {
        // Убедимся, что список вагонов инициализирован и отсортирован (Start должен был это сделать)
        if (wagons == null || wagons.Count <= 1)
        {
            Debug.LogError("Список вагонов не инициализирован или содержит менее 2 элементов.");
            return null;
        }

        // Итерируем по вагонам, начиная со второго (индекс 1), так как первому (локомотиву) предметы не принадлежат
        for (int i = 1; i < wagons.Count; i++)
        {
            Transform currentWagon = wagons[i];
            Transform previousWagon = wagons[i - 1]; // Вагон слева (может быть локомотив)

            // Вычисляем левую границу текущего вагона = середина между previousWagon и currentWagon
            float leftBoundary = previousWagon.position.x + (currentWagon.position.x - previousWagon.position.x) / 2.0f;

            // Вычисляем правую границу
            float rightBoundary;
            if (i == wagons.Count - 1) // Если это последний вагон
            {
                // Правая граница - бесконечность (или можно ограничить размером вагона, но для определения принадлежности хватит и этого)
                rightBoundary = float.PositiveInfinity;
            }
            else // Если есть следующий вагон
            {
                Transform nextWagon = wagons[i + 1];
                // Правая граница = середина между currentWagon и nextWagon
                rightBoundary = currentWagon.position.x + (nextWagon.position.x - currentWagon.position.x) / 2.0f;
            }

            // Проверяем, попадает ли позиция в границы этого вагона
            // Используем >= для левой и < для правой, чтобы избежать двойного присвоения на границе
            if (worldXPosition >= leftBoundary && worldXPosition < rightBoundary)
            {
                // Нашли подходящий вагон
                return currentWagon;
            }
        }

        // Если цикл завершился, значит позиция находится левее середины между локомотивом и первым вагоном,
        // или возникла другая ошибка. Возвращаем null.
        Debug.LogWarning($"Не удалось найти вагон для X-координаты: {worldXPosition}. Возможно, позиция слишком левее?");
        return null;
    }
    public bool AssignParentWagonByPosition(Transform itemTransform, Vector3 spawnPosition)
    {
        Transform parentWagon = GetWagonOwnerOfPosition(spawnPosition.x);

        if (parentWagon != null)
        {
            Debug.Log($"Назначен родительский вагон '{parentWagon.name}' для объекта '{itemTransform.name}' в позиции {spawnPosition}");
            // Устанавливаем родителя. true - сохраняет мировую позицию объекта после установки родителя.
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