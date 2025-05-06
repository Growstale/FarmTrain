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
        // --- Raycast и Сортировка (без изменений) ---
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPoint, Vector2.zero);

        if (hits.Length == 0) return; // Ни во что не попали

        // Сортируем попадания (без изменений)
        System.Array.Sort(hits, (hit1, hit2) =>
        {
            // Используем SpriteRenderer на самом объекте или его родителях для сортировки
            SpriteRenderer r1 = hit1.collider.GetComponentInParent<SpriteRenderer>();
            SpriteRenderer r2 = hit2.collider.GetComponentInParent<SpriteRenderer>();
            if (r1 == null && r2 == null) return 0;
            if (r1 == null) return 1; // Объекты без рендерера считаем "ниже"
            if (r2 == null) return -1;
            // Сначала по слою сортировки (более высокий слой = визуально выше)
            if (r1.sortingLayerID != r2.sortingLayerID) return r2.sortingLayerID.CompareTo(r1.sortingLayerID);
            // Потом по порядку в слое (больший порядок = визуально выше)
            if (r1.sortingOrder != r2.sortingOrder) return r2.sortingOrder.CompareTo(r1.sortingOrder);
            // Если всё совпадает, можно добавить сравнение по Z или расстоянию от камеры, но пока оставим 0
            return 0;
        });

        // Логируем попадания (опционально, но полезно для отладки)
        // Debug.Log($"Raycast Hits ({hits.Length}):");
        // for(int i = 0; i < hits.Length; i++) { /* ... подробный лог ... */ }

        RaycastHit2D topHit = hits[0]; // Самый верхний объект по сортировке
        Transform topHitTransform = topHit.transform;
        Collider2D topHitCollider = topHit.collider;

        // Debug.Log($"Top Hit: Name='{topHitCollider.name}', Tag='{topHitCollider.tag}'"); // Лог верхнего попадания

        // --- Логика обработки клика ---

        if (!isOverview) // --- РЕЖИМ ФОКУСА ---
        {
            // Проверяем все попадания, а не только topHit, на случай если вагон перекрыт, но клик был по нему
            // Однако, для простоты пока оставим логику с topHit, но изменим порядок

            // 1. Является ли верхний объект ИНТЕРАКТИВНЫМ ПРЕДМЕТОМ?
            ItemPickup clickedItem = topHitCollider.GetComponent<ItemPickup>();
            if (clickedItem != null)
            {
                Transform parentWagon = FindParentWagon(clickedItem.transform);
                if (parentWagon != null)
                {
                    int itemWagonIndex = wagons.IndexOf(parentWagon);
                    if (itemWagonIndex == currentWagonIndex) // Предмет в ТЕКУЩЕМ вагоне?
                    {
                        clickedItem.AttemptPickup(); // Взаимодействуем
                        Debug.Log($"Attempting pickup on {clickedItem.name} in current wagon {currentWagonIndex}");
                        return; // Выходим
                    }
                    else if (itemWagonIndex > 0 && itemWagonIndex < wagons.Count && Mathf.Abs(itemWagonIndex - currentWagonIndex) == 1) // Предмет в СОСЕДНЕМ вагоне?
                    {
                        // Клик по предмету в соседнем вагоне = команда перейти к этому вагону
                        Debug.Log($"Clicked item {clickedItem.name} in adjacent wagon {itemWagonIndex}. Moving camera.");
                        MoveToWagon(itemWagonIndex);
                        return; // Выходим
                    }
                    else
                    {
                        Debug.Log($"Clicked item {clickedItem.name} in non-adjacent wagon {itemWagonIndex}. Ignoring.");
                        return; // Игнорируем клик по предмету в далеком вагоне
                    }
                }
                else { return; } // Не нашли вагон для предмета, игнорируем
            }

            // 2. Является ли верхний объект ЖИВОТНЫМ?
            AnimalController clickedAnimal = topHitCollider.GetComponent<AnimalController>();
            if (clickedAnimal != null)
            {
                Transform parentWagon = FindParentWagon(clickedAnimal.transform);
                if (parentWagon != null)
                {
                    int animalWagonIndex = wagons.IndexOf(parentWagon);
                    if (animalWagonIndex == currentWagonIndex) // Животное в ТЕКУЩЕМ вагоне?
                    {
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in current wagon {currentWagonIndex}. State: {clickedAnimal.GetCurrentStateName()}");
                        if (clickedAnimal.GetCurrentStateName() == "NeedsAttention")
                        {
                            clickedAnimal.AttemptInteraction(); // Взаимодействуем, если нужно
                        }
                        // Даже если не нужно взаимодействие, клик обработан (показали лог), выходим
                        return;
                    }
                    else if (animalWagonIndex > 0 && animalWagonIndex < wagons.Count && Mathf.Abs(animalWagonIndex - currentWagonIndex) == 1) // Животное в СОСЕДНЕМ вагоне?
                    {
                        // Клик по животному в соседнем вагоне = команда перейти к этому вагону
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in adjacent wagon {animalWagonIndex}. Moving camera.");
                        MoveToWagon(animalWagonIndex);
                        return; // Выходим
                    }
                    else
                    {
                        Debug.Log($"Clicked animal {clickedAnimal.gameObject.name} in non-adjacent wagon {animalWagonIndex}. Ignoring.");
                        return; // Игнорируем клик по животному в далеком вагоне (старое сообщение было здесь)
                    }
                }
                else { return; } // Не нашли вагон для животного, игнорируем
            }

            // 3. Является ли верхний объект ГРЯДКОЙ (Bed)?
            if (topHitCollider.CompareTag("Bed"))
            {
                SlotScripts bedsScripts = topHitCollider.GetComponent<SlotScripts>();
                if (bedsScripts != null)
                {
                    Transform parentWagon = FindParentWagon(bedsScripts.transform);
                    if (parentWagon != null)
                    {
                        int bedWagonIndex = wagons.IndexOf(parentWagon);
                        if (bedWagonIndex == currentWagonIndex) // Грядка в ТЕКУЩЕМ вагоне?
                        {
                            Debug.Log($"Clicked bed in current wagon {currentWagonIndex}.");
                            bedsScripts.PlantSeeds();
                            return; // Взаимодействие, выходим
                        }
                        else if (bedWagonIndex > 0 && bedWagonIndex < wagons.Count && Mathf.Abs(bedWagonIndex - currentWagonIndex) == 1) // Грядка в СОСЕДНЕМ вагоне?
                        {
                            // Клик по грядке в соседнем вагоне = команда перейти к этому вагону
                            Debug.Log($"Clicked bed in adjacent wagon {bedWagonIndex}. Moving camera.");
                            MoveToWagon(bedWagonIndex);
                            return; // Выходим
                        }
                        else
                        {
                            Debug.Log($"Clicked bed in non-adjacent wagon {bedWagonIndex}. Ignoring.");
                            return; // Игнорируем клик по грядке в далеком вагоне
                        }
                    }
                    else { return; } // Не нашли вагон для грядки, игнорируем
                }
                else { return; } // Нет скрипта BedsScripts, игнорируем
            }

            // 4. Если НЕ попали ни в один известный интерактивный объект,
            //    проверяем, принадлежит ли ВООБЩЕ кликнутый объект (topHit) соседнему вагону.
            //    Это обработает клики по зонам, декорациям и т.д. в соседних вагонах.
            Transform clickedObjectParentWagon = FindParentWagon(topHitTransform);
            if (clickedObjectParentWagon != null) // Нашли родительский вагон для topHit?
            {
                int clickedObjectWagonIndex = wagons.IndexOf(clickedObjectParentWagon);

                // Проверяем, является ли этот вагон СОСЕДНИМ и валидным
                if (clickedObjectWagonIndex > 0 && clickedObjectWagonIndex < wagons.Count && Mathf.Abs(clickedObjectWagonIndex - currentWagonIndex) == 1)
                {
                    Debug.Log($"Clicked on non-interactive object '{topHitCollider.name}' belonging to adjacent wagon {clickedObjectWagonIndex}. Moving camera.");
                    MoveToWagon(clickedObjectWagonIndex);
                    return; // Переходим
                }
                // Если объект принадлежит ТЕКУЩЕМУ вагону (или далекому), просто ничего не делаем здесь,
                // позволяя коду дойти до проверки на тег "Wagon" ниже (на всякий случай).
            }


            // 4. Если НЕ попали ни в один интерактивный объект ИЛИ попали в интерактивный объект в ДАЛЕКОМ вагоне,
            //    тогда проверяем, был ли клик по КОЛЛАЙДЕРУ СОСЕДНЕГО ВАГОНА.
            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);
                // Проверяем, является ли этот вагон СОСЕДНИМ и валидным
                if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count && Mathf.Abs(clickedWagonIndex - currentWagonIndex) == 1)
                {
                    Debug.Log($"Clicked directly on adjacent wagon {clickedWagonIndex}. Moving camera.");
                    MoveToWagon(clickedWagonIndex);
                    return; // Переходим
                }
                // Если кликнули на текущий вагон или локомотив, ничего не делаем
            }

            // Если дошли до сюда, значит клик был либо по текущему вагону, либо по локомотиву,
            // либо по неинтерактивному объекту в текущем вагоне. Никаких действий не требуется.
        }
        else // --- РЕЖИМ ОБЗОРА (isOverview == true) ---
        {
            // Логика для режима обзора: клик по любому вагону (кроме локомотива) должен приближать
            if (topHitCollider.CompareTag("Wagon"))
            {
                int clickedWagonIndex = wagons.IndexOf(topHitTransform);
                if (clickedWagonIndex > 0 && clickedWagonIndex < wagons.Count) // Проверяем, что это не локомотив и вагон существует
                {
                    Debug.Log($"Clicked on wagon {clickedWagonIndex} in overview mode. Zooming in.");
                    ExitOverviewMode(clickedWagonIndex);
                    return;
                }
            }
            // Клик мимо вагонов в режиме обзора ничего не делает
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