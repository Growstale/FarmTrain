using UnityEngine;
using System.Collections; // Нужно для корутин (задержек)

public class AnimalController : MonoBehaviour
{
    
    [Header("Data & Links")]
    public AnimalData animalData; // Сюда будем назначать ScriptableObject с данными коровы
    public GameObject thoughtBubblePrefab; // Префаб "облачка мыслей" (создадим его позже)

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.0f; // Скорость передвижения
    [SerializeField] private float minIdleTime = 2.0f; // Мин. время стояния на месте
    [SerializeField] private float maxIdleTime = 5.0f; // Макс. время стояния на месте
    [SerializeField] private float minWalkTime = 3.0f; // Мин. время ходьбы
    [SerializeField] private float maxWalkTime = 6.0f; // Макс. время ходьбы
    [SerializeField] private Vector3 thoughtBubbleOffset = new Vector3(0, 1.2f, 0); // Смещение облачка над животным

    // --- Состояние Животного ---
    private enum AnimalState { Idle, Walking, NeedsAttention }
    private AnimalState currentState = AnimalState.Idle;

    // --- Границы Движения ---
    private Bounds movementBounds; // Границы зоны AnimalPlacementArea
    private bool boundsInitialized = false; // Флаг, что границы установлены

    // --- Таймеры ---
    private float feedTimer;
    private float productionTimer;
    private float fertilizerTimer;
    private float stateChangeTimer; // Таймер для смены состояния Idle/Walking

    // --- Флаги Потребностей ---
    private bool needsFeeding = false;
    private bool hasProductReady = false;
    private bool hasFertilizerReady = false;
    private ItemData currentNeedIcon = null; // Какой предмет показывать в облачке

    // --- Ссылки ---
    private Transform myTransform; // Кэшируем transform для производительности
    private ThoughtBubbleController activeThoughtBubble; // Ссылка на активное облачко
    private InventoryManager inventoryManager; // Ссылка на менеджер инвентаря
    private SpriteRenderer spriteRenderer; // Для возможного поворота спрайта при ходьбе

    // --- Передвижение ---
    private Vector2 currentTargetPosition;
    private bool isMoving = false;

    //=========================================================================
    // ИНИЦИАЛИЗАЦИЯ
    //=========================================================================

    void Awake() // Используем Awake вместо Start для инициализации ссылок
    {
        myTransform = transform; // Инициализируем здесь! Теперь это произойдет ДО InitializeMovementBounds
        spriteRenderer = GetComponent<SpriteRenderer>(); // И другие GetComponent лучше делать здесь
        inventoryManager = InventoryManager.Instance; // И поиск синглтона тоже
        if (inventoryManager == null)
        {
            // Предупреждение можно оставить и в Start, но поиск лучше сделать здесь
            Debug.LogError($"InventoryManager не найден! Awake() в AnimalController.");
        }
    }

    void Start()
    {
        // myTransform УЖЕ инициализирован в Awake()

        if (animalData == null)
        {
            Debug.LogError($"AnimalData не назначено для {gameObject.name}! Животное не будет работать.", gameObject);
            enabled = false; // Выключаем скрипт
            return;
        }
        if (thoughtBubblePrefab == null)
        {
            Debug.LogError($"ThoughtBubblePrefab не назначен для {gameObject.name}! Не сможем показать потребности.", gameObject);
            // Можно не выключать
        }
        if (inventoryManager == null) // Повторная проверка, если Awake не нашел
        {
            Debug.LogError($"InventoryManager не найден на сцене! Сбор предметов не будет работать.", gameObject);
            // Можно не выключать
        }

        // Запускаем начальные таймеры
        ResetFeedTimer();
        ResetProductionTimer();
        ResetFertilizerTimer();

        // Устанавливаем начальное состояние и таймер для него
        currentState = AnimalState.Idle;
        SetNewStateTimer(AnimalState.Idle);

        // ВАЖНО: Границы и запуск поведения будут в InitializeMovementBounds
        // Не вызываем StartCoroutine здесь!
    }


    // Этот метод должен вызываться ИЗВНЕ (например, из спавнера) после создания животного
    public void InitializeMovementBounds(Bounds bounds)
    {
        // Добавим проверку на всякий случай, хотя Awake должен был сработать
        if (myTransform == null)
        {
            Debug.LogError($"ОШИБКА: myTransform все еще null в InitializeMovementBounds! Проверьте Awake() у {gameObject.name}", gameObject);
            myTransform = transform; // Попытка инициализировать аварийно
        }

        movementBounds = bounds;
        boundsInitialized = true;
        Debug.Log($"{animalData.speciesName} ({gameObject.name}) получил границы движения: {movementBounds}");

        // Устанавливаем начальную позицию ВНУТРИ границ (на всякий случай)
        // Теперь myTransform должен быть не null
        myTransform.position = GetRandomPositionInBounds(); // Строка ~101 - теперь должна работать

        // Теперь можно выбрать первую цель для движения, если нужно
        PickNewWanderTarget();

        // ЗАПУСКАЕМ КОРУТИНУ ПОВЕДЕНИЯ ЗДЕСЬ, ПОСЛЕ ИНИЦИАЛИЗАЦИИ ГРАНИЦ!
        if (boundsInitialized) // Доп. проверка, что все хорошо
        {
            StartCoroutine(StateMachineCoroutine());
            Debug.Log($"StateMachineCoroutine запущен для {animalData.speciesName} ({gameObject.name})");
        }
        else
        {
            Debug.LogError($"Не удалось запустить StateMachineCoroutine для {animalData.speciesName}, т.к. границы не инициализированы!", gameObject);
        }
    }


    //=========================================================================
    // ОБНОВЛЕНИЕ (Update) - Управление таймерами и состояниями
    //=========================================================================

    void Update()
    {
        // Если границы не установлены, ничего не делаем
        if (!boundsInitialized || animalData == null) return;

        // Обновляем таймеры, ТОЛЬКО если животное НЕ ждет внимания игрока
        if (currentState != AnimalState.NeedsAttention)
        {
            UpdateTimers(Time.deltaTime);
            CheckNeeds(); // Проверяем, не пора ли что-то попросить
        }

        // Обновление позиции облачка, если оно активно
        if (activeThoughtBubble != null && activeThoughtBubble.gameObject.activeSelf)
        {
            activeThoughtBubble.transform.position = myTransform.position + thoughtBubbleOffset;
        }

        // Простая логика поворота спрайта (опционально)
        if (isMoving && spriteRenderer != null)
        {
            // Поворачиваем спрайт влево/вправо в зависимости от направления к цели
            spriteRenderer.flipX = (currentTargetPosition.x < myTransform.position.x);
        }
    }

    // Корутина для управления состояниями Idle/Walking
    // Корутина для управления состояниями Idle/Walking
    private IEnumerator StateMachineCoroutine()
    {
        Debug.Log($"StateMachineCoroutine НАЧАЛ работу для {gameObject.name}. Начальное состояние: {currentState}"); // Добавим лог при старте

        while (boundsInitialized) // Работаем, пока все хорошо
        {
            // ----- ПРОВЕРЯЕМ СОСТОЯНИЕ В НАЧАЛЕ ЦИКЛА -----
            if (currentState == AnimalState.NeedsAttention)
            {
                // Если нужно внимание, просто ждем один кадр и проверяем снова.
                // Не ждем stateChangeTimer!
                // Debug.Log($"{gameObject.name}: В состоянии NeedsAttention, ожидание 1 кадра.");
                isMoving = false; // Убедимся, что не двигаемся
                yield return null; // Ждать 1 кадр
                continue; // Вернуться к началу цикла while и проверить состояние снова
            }

            // ----- ЕСЛИ СОСТОЯНИЕ НЕ NeedsAttention (т.е. Idle или Walking) -----

            // Debug.Log($"{gameObject.name}: В состоянии {currentState}. Ожидание {stateChangeTimer} секунд.");
            yield return new WaitForSeconds(stateChangeTimer); // Ждем пока пройдет время текущего состояния

            // После ожидания, ПРОВЕРЯЕМ СНОВА, не изменилось ли состояние на NeedsAttention ПОКА мы ждали
            if (currentState == AnimalState.NeedsAttention)
            {
                // Если за время ожидания появилась потребность, прерываем смену состояния
                Debug.Log($"{gameObject.name}: Состояние изменилось на NeedsAttention ВО ВРЕМЯ ожидания. Прерываем смену.");
                isMoving = false;
                yield return null; // Ждать 1 кадр
                continue; // Вернуться к началу цикла while
            }

            // Если состояние все еще Idle или Walking, производим смену
            Debug.Log($"{gameObject.name}: Время ожидания для {currentState} истекло. Меняем состояние.");
            if (currentState == AnimalState.Idle)
            {
                currentState = AnimalState.Walking;
                PickNewWanderTarget(); // Выбираем новую точку для ходьбы
                SetNewStateTimer(AnimalState.Walking); // Устанавливаем время ходьбы
                isMoving = true;
                Debug.Log($"{gameObject.name}: Перешел в состояние Walking. Новая цель: {currentTargetPosition}, время: {stateChangeTimer} сек.");
            }
            else // Были в состоянии Walking
            {
                currentState = AnimalState.Idle;
                SetNewStateTimer(AnimalState.Idle); // Устанавливаем время отдыха
                isMoving = false;
                Debug.Log($"{gameObject.name}: Перешел в состояние Idle. Время: {stateChangeTimer} сек.");
            }
        }
        Debug.LogWarning($"StateMachineCoroutine ЗАВЕРШИЛ работу для {gameObject.name} (boundsInitialized стал false?)");
    }


    void FixedUpdate()
    {
        // Передвижение лучше делать в FixedUpdate для стабильности физики (хотя у нас ее нет)
        // или просто в Update, если нет Rigidbody
        if (currentState == AnimalState.Walking && isMoving && boundsInitialized)
        {
            MoveTowardsTarget();
        }
    }

    //=========================================================================
    // ЛОГИКА ТАЙМЕРОВ И ПОТРЕБНОСТЕЙ
    //=========================================================================

    private void UpdateTimers(float deltaTime)
    {
        // Уменьшаем таймеры, если они больше нуля
        if (feedTimer > 0) feedTimer -= deltaTime;
        if (productionTimer > 0) productionTimer -= deltaTime;
        if (fertilizerTimer > 0) fertilizerTimer -= deltaTime;
    }

    private void CheckNeeds()
    {
        bool needsAttentionNow = false;
        ItemData nextNeedIcon = null; // Временная переменная для лога

        if (!needsFeeding && feedTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Еда ({animalData.requiredFood.itemName})"); // ЛОГ 1
            needsFeeding = true;
            nextNeedIcon = animalData.requiredFood;
            needsAttentionNow = true;
        }
        // Используем else if, чтобы проверить продукт только если не голоден
        else if (!hasProductReady && productionTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Продукт ({animalData.productProduced.itemName})"); // ЛОГ 2
            hasProductReady = true;
            nextNeedIcon = animalData.productProduced;
            needsAttentionNow = true;
        }
        // Используем else if, чтобы проверить удобрение только если не голоден и нет продукта
        else if (!hasFertilizerReady && fertilizerTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Удобрение ({animalData.fertilizerProduced.itemName})"); // ЛОГ 3
            hasFertilizerReady = true;
            nextNeedIcon = animalData.fertilizerProduced;
            needsAttentionNow = true;
        }

        // Если что-то требуется, переходим в состояние NeedsAttention
        if (needsAttentionNow)
        {
            // Сверяем, нужно ли ДЕЙСТВИТЕЛЬНО менять состояние (оптимизация)
            if (currentState != AnimalState.NeedsAttention || currentNeedIcon != nextNeedIcon)
            {
                // Debug.LogWarning($"[CheckNeeds] Найдена потребность ({nextNeedIcon?.itemName}). Устанавливаем NeedsAttention.");
                currentState = AnimalState.NeedsAttention;
                currentNeedIcon = nextNeedIcon; // Важно обновить иконку!
                isMoving = false;
                ShowThoughtBubble(currentNeedIcon);
            }
            // else { Debug.Log($"[CheckNeeds] Потребность ({nextNeedIcon?.itemName}) уже активна. Состояние не меняем."); }
        }
        // ----- НОВОЕ: ЕСЛИ ПОТРЕБНОСТЕЙ НЕТ, А СОСТОЯНИЕ БЫЛО NeedsAttention -----
        else if (currentState == AnimalState.NeedsAttention)
        {
            // Если НЕТ активных потребностей (needsAttentionNow == false),
            // а мы все еще в состоянии NeedsAttention, значит, потребность была только что удовлетворена
            // или возникла ошибка. Возвращаем в Idle.
            Debug.LogWarning($"[CheckNeeds] Потребностей не найдено, но состояние было NeedsAttention. Возвращаем в Idle.");
            HideThoughtBubble(); // Скрываем облачко на всякий случай
            currentState = AnimalState.Idle;
            SetNewStateTimer(currentState);
            // Корутина должна сама подхватить новое состояние Idle при следующей итерации
            // НЕ перезапускаем корутину здесь, чтобы избежать возможных конфликтов с перезапуском в AttemptInteraction
        }
        // else if (needsAttentionNow && nextNeedIcon == null) {
        //     Debug.LogError("[CheckNeeds] needsAttentionNow is true, but nextNeedIcon is null! Это не должно происходить.");
        // }
    }

    private void ResetFeedTimer()
    {
        feedTimer = animalData.feedInterval;
        needsFeeding = false;
    }

    private void ResetProductionTimer()
    {
        productionTimer = animalData.productionInterval;
        hasProductReady = false;
    }

    private void ResetFertilizerTimer()
    {
        fertilizerTimer = animalData.fertilizerInterval;
        hasFertilizerReady = false;
    }

    private void SetNewStateTimer(AnimalState forState)
    {
        if (forState == AnimalState.Idle)
        {
            stateChangeTimer = Random.Range(minIdleTime, maxIdleTime);
        }
        else // Walking
        {
            stateChangeTimer = Random.Range(minWalkTime, maxWalkTime);
        }
    }

    //=========================================================================
    // ЛОГИКА ПЕРЕДВИЖЕНИЯ
    //=========================================================================

    private void PickNewWanderTarget()
    {
        currentTargetPosition = GetRandomPositionInBounds();
        // Debug.Log($"{animalData.speciesName} идет к {currentTargetPosition}");
    }

    private Vector2 GetRandomPositionInBounds()
    {
        if (!boundsInitialized) return myTransform.position; // Безопасность

        // Генерируем случайную точку внутри прямоугольника movementBounds
        float randomX = Random.Range(movementBounds.min.x, movementBounds.max.x);
        float randomY = Random.Range(movementBounds.min.y, movementBounds.max.y);

        // Возвращаем как Vector2 (или Vector3 с нужным Z, если надо)
        return new Vector2(randomX, randomY);
    }

    private void MoveTowardsTarget()
    {
        if (!isMoving) return;

        // Двигаем объект к цели с заданной скоростью
        Vector2 currentPosition = myTransform.position;
        Vector2 direction = (currentTargetPosition - currentPosition).normalized;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, currentTargetPosition, moveSpeed * Time.fixedDeltaTime);

        // Важно: Проверяем, не выйдет ли новый шаг за границы
        // Хотя GetRandomPositionInBounds выбирает точку внутри, MoveTowards может вывести за край на последнем шаге
        // Простой способ - просто зажать позицию внутри границ
        newPosition.x = Mathf.Clamp(newPosition.x, movementBounds.min.x, movementBounds.max.x);
        newPosition.y = Mathf.Clamp(newPosition.y, movementBounds.min.y, movementBounds.max.y);


        myTransform.position = newPosition;


        // Проверяем, достигли ли мы цели (с небольшой погрешностью)
        if (Vector2.Distance(currentPosition, currentTargetPosition) < 0.1f)
        {
            isMoving = false; // Останавливаемся по достижении
                              // Состояние сменится по таймеру stateChangeTimer в корутине
                              // Debug.Log($"{animalData.speciesName} достиг цели.");
        }
    }

    //=========================================================================
    // ВИЗУАЛЬНОЕ ОПОВЕЩЕНИЕ (Облачко)
    //=========================================================================

    private void ShowThoughtBubble(ItemData itemToShow)
    {
        Debug.Log($"[ShowThoughtBubble] Вызван. Пытаемся показать: {itemToShow?.itemName ?? "NULL"}"); // ЛОГ 5

        if (thoughtBubblePrefab == null)
        {
            Debug.LogError("Нет префаба облачка!");
            return;
        }

        // Если облачко еще не создано, создаем его
        if (activeThoughtBubble == null)
        {
            GameObject bubbleInstance = Instantiate(thoughtBubblePrefab, myTransform.position + thoughtBubbleOffset, Quaternion.identity, myTransform); // Делаем дочерним к животному
            activeThoughtBubble = bubbleInstance.GetComponent<ThoughtBubbleController>();
            if (activeThoughtBubble == null)
            {
                Debug.LogError("Префаб облачка не содержит скрипт ThoughtBubbleController!");
                Destroy(bubbleInstance);
                return;
            }
        }

        // Настраиваем и показываем облачко
        if (itemToShow != null && itemToShow.itemIcon != null)
        {
            Debug.Log($"[ShowThoughtBubble] Иконка для {itemToShow.itemName} НАЙДЕНА. Вызываем activeThoughtBubble.Show()."); // ЛОГ 6
            activeThoughtBubble.Show(itemToShow.itemIcon);
        }
        else
        {
            Debug.LogWarning($"Попытка показать облачко для {animalData.speciesName}, но у предмета {itemToShow?.itemName} нет иконки!");
            activeThoughtBubble.Hide(); // Скрываем, если иконки нет
        }
    }

    private void HideThoughtBubble()
    {
        if (activeThoughtBubble != null)
        {
            activeThoughtBubble.Hide();
        }
        currentNeedIcon = null; // Сбрасываем текущую потребность
    }

    //=========================================================================
    // ВЗАИМОДЕЙСТВИЕ С ИГРОКОМ
    //=========================================================================

    public string GetCurrentStateName()
    {
        return currentState.ToString(); // Возвращает имя состояния ("Idle", "Walking", "NeedsAttention")
    }


    public void AttemptInteraction()
    {
        if (inventoryManager == null)
        {
            Debug.LogError("Нет ссылки на InventoryManager!");
            return;
        }

        bool interactionSuccessful = false;
        ItemData itemInvolved = null; // Запомним, какой предмет участвовал

        // 1. Проверяем, нужно ли кормить
        if (needsFeeding)
        {
            Debug.Log($"Попытка взаимодействия: {animalData.speciesName} нуждается в {animalData.requiredFood.itemName}");
            itemInvolved = animalData.requiredFood;

            // Проверяем, что у нас есть ссылка на InventoryManager
            if (inventoryManager == null)
            {
                Debug.LogError($"Не могу проверить инвентарь для кормления {animalData.speciesName} - ссылка на InventoryManager отсутствует!");
                interactionSuccessful = false; // Не можем продолжить
            }
            else
            {
                // Получаем ВЫБРАННЫЙ предмет и ИНДЕКС выбранного слота
                InventoryItem selectedItem = inventoryManager.GetSelectedItem();
                int selectedIndex = inventoryManager.SelectedSlotIndex; // Используем новое свойство

                // Проверяем, есть ли выбранный предмет и совпадает ли он с нужной едой
                if (selectedItem != null && !selectedItem.IsEmpty && selectedItem.itemData == animalData.requiredFood)
                {
                    // Ура, игрок держит нужную еду!
                    Debug.Log($"Игрок держит {selectedItem.itemData.itemName} в слоте {selectedIndex}. Пытаемся удалить 1 шт.");

                    // Пытаемся удалить 1 единицу предмета из выбранного слота
                    inventoryManager.RemoveItem(selectedIndex, 1);

                    // Сбрасываем таймер кормления и флаг потребности
                    ResetFeedTimer();
                    needsFeeding = false;
                    interactionSuccessful = true; // Кормление успешно!
                    Debug.Log($"Успешно покормлен {animalData.speciesName}");
                }
                else
                {
                    // Определяем причину неудачи для более информативного лога
                    string reason = "Неизвестная причина";
                    if (selectedItem == null || selectedItem.IsEmpty)
                    {
                        reason = "В выбранном слоте (хотбар) нет предмета";
                    }
                    else if (selectedItem.itemData != animalData.requiredFood)
                    {
                        reason = $"Выбран неверный предмет ({selectedItem.itemData.itemName}), нужен {animalData.requiredFood.itemName}";
                    }
                    Debug.Log($"Не удалось покормить {animalData.speciesName}: {reason}.");
                    interactionSuccessful = false; // Кормление НЕ успешно
                }
            }
        }
        // 2. Проверяем, готов ли продукт (если не кормили)
        else if (hasProductReady)
        {
            Debug.Log($"Попытка собрать {animalData.productProduced.itemName} c {animalData.speciesName}");
            // Пытаемся добавить предмет в инвентарь
            bool added = inventoryManager.AddItem(animalData.productProduced, animalData.productAmount);

            if (added)
            {
                Debug.Log($"Успешно собрано {animalData.productAmount} {animalData.productProduced.itemName}");
                ResetProductionTimer();
                hasProductReady = false; // Сбрасываем флаг
                interactionSuccessful = true;
            }
            else
            {
                Debug.Log("Не удалось собрать продукт - инвентарь полон!");
                // Ничего не делаем, облачко остается
            }
        }
        // 3. Проверяем, готово ли удобрение (если не кормили и не собирали продукт)
        else if (hasFertilizerReady)
        {
            Debug.Log($"Попытка собрать {animalData.fertilizerProduced.itemName} c {animalData.speciesName}");
            bool added = inventoryManager.AddItem(animalData.fertilizerProduced, animalData.fertilizerAmount);

            if (added)
            {
                Debug.Log($"Успешно собрано {animalData.fertilizerAmount} {animalData.fertilizerProduced.itemName}");
                ResetFertilizerTimer();
                hasFertilizerReady = false; // Сбрасываем флаг
                interactionSuccessful = true;
            }
            else
            {
                Debug.Log("Не удалось собрать удобрение - инвентарь полон!");
            }
        }

        // Если взаимодействие было УСПЕШНЫМ, проверяем, есть ли ЕЩЕ потребности
        // Если взаимодействие было УСПЕШНЫМ
        if (interactionSuccessful)
        {
            Debug.Log($"Взаимодействие с {animalData.speciesName} (предмет: {itemInvolved?.itemName}) было УСПЕШНЫМ.");
            // Скрываем текущее облачко
            HideThoughtBubble();

            // Сразу проверяем, нет ли следующей потребности по таймеру
            Debug.Log("Проверяем нужды СРАЗУ ПОСЛЕ успешного взаимодействия...");
            CheckNeeds(); // Этот вызов УСТАНОВИТ currentState либо в NeedsAttention, либо в Idle (согласно Изменению 1)

            Debug.Log($"Состояние ПОСЛЕ CheckNeeds: {currentState}. Перезапускаем StateMachineCoroutine.");

            // Перезапускаем корутину ВСЕГДА после успеха
            // Она начнет работу с тем состоянием, которое установил CheckNeeds
            StopAllCoroutines();
            StartCoroutine(StateMachineCoroutine());

            Debug.Log($"{animalData.speciesName} обработал успешное взаимодействие и перезапустил StateMachine.");
        }
        else // Если interactionSuccessful == false
        {
            // Добавим проверку состояния для диагностики
            Debug.Log($"Взаимодействие с {animalData.speciesName} (предмет: {itemInvolved?.itemName ?? "NULL"}) было НЕУСПЕШНЫМ. Текущее состояние: {currentState}. Флаги: Feed={needsFeeding}, Prod={hasProductReady}, Fert={hasFertilizerReady}");
        }
    }

}