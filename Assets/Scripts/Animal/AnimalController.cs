using UnityEngine;
using System.Collections;

public class AnimalController : MonoBehaviour
{
    
    [Header("Data & Links")]
    public AnimalData animalData; 
    public GameObject thoughtBubblePrefab; // облако мыслей

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.0f; // Скорость передвижения
    [SerializeField] private float minIdleTime = 2.0f; // Мин. время стояния на месте
    [SerializeField] private float maxIdleTime = 5.0f; // Макс. время стояния на месте
    [SerializeField] private float minWalkTime = 3.0f; // Мин. время ходьбы
    [SerializeField] private float maxWalkTime = 6.0f; // Макс. время ходьбы
    [SerializeField] private Vector3 thoughtBubbleOffset = new Vector3(1.4f, 0.9f, 0);

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

    void Awake() 
    {
        myTransform = transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        inventoryManager = InventoryManager.Instance; 
        if (inventoryManager == null)
        {
            Debug.LogError($"InventoryManager не найден! Awake() в AnimalController");
        }
    }

    void Start()
    {
        if (animalData == null)
        {
            Debug.LogError($"AnimalData не назначено для {gameObject.name}! Животное не будет работать.", gameObject);
            enabled = false; 
            return;
        }
        if (thoughtBubblePrefab == null)
        {
            Debug.LogError($"ThoughtBubblePrefab не назначен для {gameObject.name}! Не сможем показать потребности.", gameObject);
        }
        if (inventoryManager == null)
        {
            Debug.LogError($"InventoryManager не найден на сцене! Сбор предметов не будет работать.", gameObject);
        }

        // Запускаем начальные таймеры
        ResetFeedTimer();
        ResetProductionTimer();
        ResetFertilizerTimer();

        currentState = AnimalState.Idle;
        SetNewStateTimer(AnimalState.Idle);
        UpdateAppearance();
    }


    // Этот метод должен вызываться ИЗВНЕ
    public void InitializeMovementBounds(Bounds bounds)
    {
        if (myTransform == null)
        {
            Debug.LogError($"ОШИБКА: myTransform все еще null в InitializeMovementBounds! Проверьте Awake() у {gameObject.name}", gameObject);
            myTransform = transform;
        }

        movementBounds = bounds;
        boundsInitialized = true;
        Debug.Log($"{animalData.speciesName} ({gameObject.name}) получил границы движения: {movementBounds}");

        myTransform.position = GetRandomPositionInBounds();
        PickNewWanderTarget();

        if (boundsInitialized) // Доп. проверка
        {
            StartCoroutine(StateMachineCoroutine());
            Debug.Log($"StateMachineCoroutine запущен для {animalData.speciesName} ({gameObject.name})");
        }
        else
        {
            Debug.LogError($"Не удалось запустить StateMachineCoroutine для {animalData.speciesName}, т.к. границы не инициализированы!", gameObject);
        }
    }


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
            // Обновляем его позицию каждый кадр, чтобы оно оставалось над головой движущегося животного
            activeThoughtBubble.transform.position = myTransform.position + thoughtBubbleOffset;
        }

        // Логика поворота спрайта
        if (isMoving && spriteRenderer != null)
        {
            // Сравниваем текущую позицию с целевой
            float horizontalDifference = currentTargetPosition.x - myTransform.position.x;

            if (Mathf.Abs(horizontalDifference) > 0.01f) 
            {
                spriteRenderer.flipX = (horizontalDifference > 0);
            }
        }
    }

    // Корутина для управления состояниями Idle/Walking
    // Корутина для управления состояниями Idle/Walking
    private IEnumerator StateMachineCoroutine()
    {
        Debug.Log($"StateMachineCoroutine НАЧАЛ работу для {gameObject.name}. Начальное состояние: {currentState}");

        while (boundsInitialized) 
        {
            if (currentState == AnimalState.NeedsAttention)
            {
                isMoving = false;
                yield return null; // Ждать 1 кадр
                continue; // Вернуться к началу цикла while и проверить состояние снова
            }

            // Idle или Walking

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
        if (currentState == AnimalState.Walking && isMoving && boundsInitialized)
        {
            MoveTowardsTarget();
        }
    }

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
        ItemData nextNeedIcon = null;
        bool didProductBecomeReady = false;

        if (!needsFeeding && feedTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Еда ({animalData.requiredFood.itemName})"); 
            needsFeeding = true;
            nextNeedIcon = animalData.requiredFood;
            needsAttentionNow = true;
        }

        else if (!hasProductReady && productionTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Продукт ({animalData.productProduced.itemName})");
            hasProductReady = true;
            didProductBecomeReady = true;
            nextNeedIcon = animalData.productProduced;
            needsAttentionNow = true;
        }

        else if (!hasFertilizerReady && fertilizerTimer <= 0)
        {
            Debug.Log($"[CheckNeeds] Обнаружена потребность: Удобрение ({animalData.fertilizerProduced.itemName})");
            hasFertilizerReady = true;
            nextNeedIcon = animalData.fertilizerProduced;
            needsAttentionNow = true;
        }

        if (didProductBecomeReady)
        {
            UpdateAppearance(); // <--- ОБНОВЛЯЕМ ВИД ПРИ ГОТОВНОСТИ ПРОДУКТА
        }

        // Если что-то требуется, переходим в состояние NeedsAttention
        if (needsAttentionNow)
        {
            if (currentState != AnimalState.NeedsAttention || currentNeedIcon != nextNeedIcon)
            {
                currentState = AnimalState.NeedsAttention;
                currentNeedIcon = nextNeedIcon;
                isMoving = false;
                ShowThoughtBubble(currentNeedIcon);
            }
        }
        else if (currentState == AnimalState.NeedsAttention)
        {
            // Если НЕТ активных потребностей (needsAttentionNow == false),
            // а мы все еще в состоянии NeedsAttention, значит, потребность была только что удовлетворена
            // или возникла ошибка. Возвращаем в Idle.
            Debug.LogWarning($"[CheckNeeds] Потребностей не найдено, но состояние было NeedsAttention. Возвращаем в Idle.");
            HideThoughtBubble();
            currentState = AnimalState.Idle;
            SetNewStateTimer(currentState);
        }
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
        UpdateAppearance();
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

    private void UpdateAppearance()
    {
        if (spriteRenderer == null || animalData == null) return; // Безопасность

        // Если продукт готов И есть спрайт для этого состояния
        if (hasProductReady && animalData.productReadySprite != null)
        {
            spriteRenderer.sprite = animalData.productReadySprite;
            // Debug.Log($"{animalData.speciesName} показывает спрайт 'productReadySprite'");
        }
        // Иначе, если есть спрайт по умолчанию (используем его)
        else if (animalData.defaultSprite != null)
        {
            spriteRenderer.sprite = animalData.defaultSprite;
            // Debug.Log($"{animalData.speciesName} показывает спрайт 'defaultSprite'");
        }
        // Если нет ни того, ни другого (или продукт не готов, но нет спрайта по умолчанию)
        else
        {
            // Оставляем текущий спрайт или логируем предупреждение, если спрайты не назначены в AnimalData
            if (hasProductReady && animalData.productReadySprite == null && animalData.defaultSprite == null)
                Debug.LogWarning($"У {animalData.speciesName} готов продукт, но не назначены 'productReadySprite' и 'defaultSprite' в AnimalData!", gameObject);
            else if (!hasProductReady && animalData.defaultSprite == null)
                Debug.LogWarning($"У {animalData.speciesName} нет продукта, но не назначен 'defaultSprite' в AnimalData!", gameObject);
        }
    }


    //=========================================================================
    // ЛОГИКА ПЕРЕДВИЖЕНИЯ
    //=========================================================================

    private void PickNewWanderTarget()
    {
        currentTargetPosition = GetRandomPositionInBounds();
    }

    private Vector2 GetRandomPositionInBounds()
    {
        if (!boundsInitialized) return myTransform.position; // Безопасность

        float randomX = Random.Range(movementBounds.min.x, movementBounds.max.x);
        float randomY = Random.Range(movementBounds.min.y, movementBounds.max.y);

        return new Vector2(randomX, randomY);
    }

    private void MoveTowardsTarget()
    {
        if (!isMoving) return;

        Vector2 currentPosition = myTransform.position;
        Vector2 direction = (currentTargetPosition - currentPosition).normalized;
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, currentTargetPosition, moveSpeed * Time.fixedDeltaTime);

        newPosition.x = Mathf.Clamp(newPosition.x, movementBounds.min.x, movementBounds.max.x);
        newPosition.y = Mathf.Clamp(newPosition.y, movementBounds.min.y, movementBounds.max.y);


        myTransform.position = newPosition;


        if (Vector2.Distance(currentPosition, currentTargetPosition) < 0.1f)
        {
            isMoving = false; 
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
            BubbleYSorter bubbleSorter = bubbleInstance.GetComponent<BubbleYSorter>();
            if (bubbleSorter != null)
            {
                bubbleSorter.SetOwner(myTransform); // Передаем transform этого животного
            }
            else
            {
                Debug.LogError($"На префабе облачка {thoughtBubblePrefab.name} отсутствует скрипт BubbleYSorter!", bubbleInstance);
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
        currentNeedIcon = null; 
    }


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
        ItemData itemInvolved = null; 

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
        // 2. Проверка сбора продукта (ИЗМЕНЕНО!)
        else if (hasProductReady)
        {
            itemInvolved = animalData.productProduced; // Запомним продукт для лога
            Debug.Log($"Попытка собрать {itemInvolved.itemName} c {animalData.speciesName}");

            // -------- ПРОВЕРКА ИНСТРУМЕНТА --------
            bool toolCheckPassed = true; // По умолчанию считаем, что инструмент не нужен или есть
            if (animalData.requiredToolForHarvest != null) // Требуется ли инструмент?
            {
                Debug.Log($"Для сбора {itemInvolved.itemName} требуется {animalData.requiredToolForHarvest.itemName}");
                InventoryItem selectedItem = inventoryManager.GetSelectedItem();

                if (selectedItem == null || selectedItem.IsEmpty || selectedItem.itemData != animalData.requiredToolForHarvest)
                {
                    // Инструмент не выбран или не тот
                    Debug.Log($"Не удалось собрать: Игрок не держит {animalData.requiredToolForHarvest.itemName}. Выбрано: {selectedItem?.itemData?.itemName ?? "Ничего"}");
                    toolCheckPassed = false;
                    // Можно добавить показ "сообщения" игроку, что нужен инструмент
                    // например, через UI или другое облачко мысли
                }
                else
                {
                    Debug.Log($"Игрок держит нужный инструмент: {selectedItem.itemData.itemName}");
                    // Инструмент правильный, toolCheckPassed остается true
                }
            }
            // ---------------------------------------

            // Если проверка инструмента пройдена (или он не требовался)
            if (toolCheckPassed)
            {
                // Пытаемся добавить предмет в инвентарь
                bool added = inventoryManager.AddItem(animalData.productProduced, animalData.productAmount);

                if (added)
                {
                    Debug.Log($"Успешно собрано {animalData.productAmount} {animalData.productProduced.itemName}");
                    // Сбрасываем флаг ДО сброса таймера, чтобы UpdateAppearance сработал корректно
                    hasProductReady = false;
                    ResetProductionTimer(); // Этот метод вызовет UpdateAppearance()
                    interactionSuccessful = true;
                }
                else
                {
                    Debug.Log("Не удалось собрать продукт - инвентарь полон!");
                    // Вид не меняем, потребность остается
                }
            }
            // Если инструмент не тот, interactionSuccessful остается false
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