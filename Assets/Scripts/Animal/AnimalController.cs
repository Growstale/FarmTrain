using UnityEngine;
using System.Collections;

// Этот enum остается без изменений
public enum AnimalProductionState
{
    WaitingForFeed,
    ReadyForProduct,
    ReadyForFertilizer
}

public class AnimalController : MonoBehaviour
{
    [Header("Data & Links")]
    public AnimalData animalData;
    public GameObject thoughtBubblePrefab;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float minIdleTime = 2.0f;
    [SerializeField] private float maxIdleTime = 5.0f;
    [SerializeField] private float minWalkTime = 3.0f;
    [SerializeField] private float maxWalkTime = 6.0f;
    [SerializeField] private Vector3 thoughtBubbleOffset = new Vector3(1.4f, 0.9f, 0);

    private Animator animalAnimator;

    private enum AnimalState { Idle, Walking, NeedsAttention }
    private AnimalState currentState = AnimalState.Idle;
    private AudioSource audioSource;
    private Coroutine soundCoroutine;

    private Bounds movementBounds;
    private bool boundsInitialized = false;

    private float cycleTimer;
    private AnimalProductionState productionState;
    private float stateChangeTimer;

    private bool needsFeeding = false;
    private bool hasProductReady = false;
    private bool hasFertilizerReady = false;
    private ItemData currentNeedIcon = null;

    private Transform myTransform;
    private ThoughtBubbleController activeThoughtBubble;
    private InventoryManager inventoryManager;
    private SpriteRenderer spriteRenderer;

    private Vector2 currentTargetPosition;
    private bool isMoving = false;

    private AnimalStateData currentStateData;

    void Awake()
    {
        myTransform = transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError($"InventoryManager не найден! Awake() в AnimalController");
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        animalAnimator = GetComponent<Animator>();
        if (animalAnimator == null)
        {
            Debug.LogWarning($"На животном {gameObject.name} отсутствует компонент Animator.", gameObject);
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

        currentState = AnimalState.Idle;
        SetNewStateTimer(AnimalState.Idle);
        UpdateAppearance();
        CheckForAchievement(animalData.speciesName);

        if (currentStateData == null)
        {
            this.productionState = AnimalProductionState.WaitingForFeed;
            this.cycleTimer = animalData.feedInterval;
        }

        if (soundCoroutine == null)
        {
            soundCoroutine = StartCoroutine(RandomSoundCoroutine());
        }

    }

    public void InitializeWithState(AnimalStateData stateData, Bounds bounds)
    {
        currentStateData = stateData;
        animalData = stateData.animalData;

        this.productionState = stateData.productionState;
        this.cycleTimer = stateData.cycleTimer;

        CheckNeeds();

        InitializeMovementBounds(bounds, false);
    }

    public void InitializeMovementBounds(Bounds bounds, bool setInitialPosition)
    {
        if (myTransform == null)
        {
            Debug.LogError($"ОШИБКА: myTransform все еще null в InitializeMovementBounds! Проверьте Awake() у {gameObject.name}", gameObject);
            myTransform = transform;
        }

        movementBounds = bounds;
        boundsInitialized = true;
        Debug.Log($"{animalData.speciesName} ({gameObject.name}) получил границы движения: {movementBounds}");

        if (setInitialPosition)
        {
            myTransform.position = GetRandomPositionInBounds();
            Debug.Log($"Устанавливаю начальную случайную позицию для {animalData.speciesName}: {myTransform.position}");
        }
        PickNewWanderTarget();

        if (boundsInitialized)
        {
            StartCoroutine(StateMachineCoroutine());
            Debug.Log($"StateMachineCoroutine запущен для {animalData.speciesName} ({gameObject.name})");
        }
        else
        {
            Debug.LogError($"Не удалось запустить StateMachineCoroutine для {animalData.speciesName}, т.к. границы не инициализированы!", gameObject);
        }
    }

    public void SaveState()
    {
        Debug.Log("SaveState!!!");
        if (currentStateData != null)
        {
            currentStateData.productionState = this.productionState;
            currentStateData.cycleTimer = this.cycleTimer;

            currentStateData.lastPosition = transform.position;
            currentStateData.hasBeenPlaced = true;

            Debug.Log($"<color=orange>[AnimalController]</color> Сохраняю состояние для {animalData.speciesName}. " +
          $"Позиция: {currentStateData.lastPosition}. " +
          $"Флаг hasBeenPlaced теперь: <color=yellow>{currentStateData.hasBeenPlaced}</color>");
        }
        else
        {
            Debug.Log($"<color=red>[AnimalController]</color> Попытка сохранить состояние для {gameObject.name}, но currentStateData is null! Сохранение не удалось.");
        }
    }

    void Update()
    {
        if (!boundsInitialized || animalData == null) return;

        if (currentState != AnimalState.NeedsAttention)
        {
            UpdateTimers(Time.deltaTime);
            CheckNeeds();
        }

        if (activeThoughtBubble != null && activeThoughtBubble.gameObject.activeSelf)
        {
            activeThoughtBubble.transform.position = myTransform.position + thoughtBubbleOffset;
        }

        if (isMoving && spriteRenderer != null)
        {
            float horizontalDifference = currentTargetPosition.x - myTransform.position.x;

            if (Mathf.Abs(horizontalDifference) > 0.01f)
            {
                spriteRenderer.flipX = (horizontalDifference > 0);
            }
        }
    }

    private IEnumerator StateMachineCoroutine()
    {
        Debug.Log($"StateMachineCoroutine НАЧАЛ работу для {gameObject.name}. Начальное состояние: {currentState}");

        while (boundsInitialized)
        {
            if (currentState == AnimalState.NeedsAttention)
            {
                isMoving = false;
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(stateChangeTimer);

            if (currentState == AnimalState.NeedsAttention)
            {
                Debug.Log($"{gameObject.name}: Состояние изменилось на NeedsAttention ВО ВРЕМЯ ожидания. Прерываем смену.");
                isMoving = false;
                yield return null;
                continue;
            }

            Debug.Log($"{gameObject.name}: Время ожидания для {currentState} истекло. Меняем состояние.");
            if (currentState == AnimalState.Idle)
            {
                currentState = AnimalState.Walking;
                PickNewWanderTarget();
                SetNewStateTimer(AnimalState.Walking);
                isMoving = true;
                animalAnimator?.SetBool("isWalking", true);
                Debug.Log($"{gameObject.name}: Перешел в состояние Walking. Новая цель: {currentTargetPosition}, время: {stateChangeTimer} сек.");
            }
            else
            {
                currentState = AnimalState.Idle;
                SetNewStateTimer(AnimalState.Idle);
                isMoving = false;
                animalAnimator?.SetBool("isWalking", false);
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

    // --- ИЗМЕНЕНИЕ: Логика авто-кормушки теперь вызывает новый метод перехода ---
    private void UpdateTimers(float deltaTime)
    {
        if (productionState == AnimalProductionState.WaitingForFeed && AnimalPenManager.Instance.HasAutoFeeder(this.animalData))
        {
            Debug.Log($"[Auto-Feeder] Животное {animalData.speciesName} накормлено автоматически. Переход к следующему этапу.");
            TransitionToNextProductionState(); // Используем новый метод для перехода
            return;
        }

        if (cycleTimer > 0)
        {
            cycleTimer -= deltaTime;
        }
    }

    private void CheckNeeds()
    {
        if (cycleTimer > 0)
        {
            if (currentState == AnimalState.NeedsAttention && !needsFeeding && !hasProductReady && !hasFertilizerReady)
            {
                Debug.LogWarning($"[CheckNeeds] In NeedsAttention state, but no active need flags. Reverting to Idle.");
                HideThoughtBubble();
                currentState = AnimalState.Idle;
                SetNewStateTimer(currentState);
            }
            return;
        }

        bool needsAttentionNow = false;
        ItemData nextNeedIcon = null;

        switch (productionState)
        {
            case AnimalProductionState.WaitingForFeed:
                if (!needsFeeding)
                {
                    Debug.Log($"[CheckNeeds] State: WaitingForFeed. Timer is up. Animal needs food.");
                    needsFeeding = true;
                    nextNeedIcon = animalData.requiredFood;
                    needsAttentionNow = true;
                }
                break;

            case AnimalProductionState.ReadyForProduct:
                if (!hasProductReady)
                {
                    Debug.Log($"[CheckNeeds] State: ReadyForProduct. Timer is up. Product is ready.");
                    hasProductReady = true;
                    UpdateAppearance();
                    nextNeedIcon = animalData.productProduced;
                    needsAttentionNow = true;
                }
                break;

            case AnimalProductionState.ReadyForFertilizer:
                if (!hasFertilizerReady)
                {
                    Debug.Log($"[CheckNeeds] State: ReadyForFertilizer. Timer is up. Fertilizer is ready.");
                    hasFertilizerReady = true;
                    nextNeedIcon = animalData.fertilizerProduced;
                    needsAttentionNow = true;
                }
                break;
        }

        if (needsAttentionNow)
        {
            if (currentState != AnimalState.NeedsAttention || currentNeedIcon != nextNeedIcon)
            {
                currentState = AnimalState.NeedsAttention;
                currentNeedIcon = nextNeedIcon;
                isMoving = false;
                animalAnimator?.SetBool("isWalking", false);
                ShowThoughtBubble(currentNeedIcon);
            }
        }
    }

    private void SetNewStateTimer(AnimalState forState)
    {
        if (forState == AnimalState.Idle)
        {
            stateChangeTimer = Random.Range(minIdleTime, maxIdleTime);
        }
        else
        {
            stateChangeTimer = Random.Range(minWalkTime, maxWalkTime);
        }
    }

    private void UpdateAppearance()
    {
        if (spriteRenderer == null || animalData == null) return;

        if (animalAnimator != null && animalData.speciesName == "Sheep")
        {
            animalAnimator.SetBool("doCut", hasProductReady);
            Debug.Log($"[Animator] Овца: isReady = {hasProductReady}");
        }
        else
        {
            if (hasProductReady && animalData.productReadySprite == null && animalData.defaultSprite == null)
                Debug.LogWarning($"У {animalData.speciesName} готов продукт, но не назначены 'productReadySprite' и 'defaultSprite' в AnimalData!", gameObject);
            else if (!hasProductReady && animalData.defaultSprite == null)
                Debug.LogWarning($"У {animalData.speciesName} нет продукта, но не назначен 'defaultSprite' в AnimalData!", gameObject);
        }
    }

    private void PickNewWanderTarget()
    {
        currentTargetPosition = GetRandomPositionInBounds();
    }

    private Vector2 GetRandomPositionInBounds()
    {
        if (!boundsInitialized) return myTransform.position;

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
            animalAnimator?.SetBool("isWalking", false);
        }
    }

    private void ShowThoughtBubble(ItemData itemToShow)
    {
        Debug.Log($"[ShowThoughtBubble] Вызван. Пытаемся показать: {itemToShow?.itemName ?? "NULL"}");

        if (thoughtBubblePrefab == null)
        {
            Debug.LogError("Нет префаба облачка!");
            return;
        }

        if (activeThoughtBubble == null)
        {
            GameObject bubbleInstance = Instantiate(thoughtBubblePrefab, myTransform.position + thoughtBubbleOffset, Quaternion.identity, myTransform);
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
                bubbleSorter.SetOwner(myTransform);
            }
            else
            {
                Debug.LogError($"На префабе облачка {thoughtBubblePrefab.name} отсутствует скрипт BubbleYSorter!", bubbleInstance);
            }
        }


        if (itemToShow != null && itemToShow.itemIcon != null)
        {
            Debug.Log($"[ShowThoughtBubble] Иконка для {itemToShow.itemName} НАЙДЕНА. Вызываем activeThoughtBubble.Show().");
            activeThoughtBubble.Show(itemToShow);
        }
        else
        {
            Debug.LogWarning($"Попытка показать облачко для {animalData.speciesName}, но у предмета {itemToShow?.itemName} нет иконки!");
            activeThoughtBubble.Hide();
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
        return currentState.ToString();
    }

    private void TransitionToNextProductionState()
    {
        // Сбрасываем все флаги перед тем, как определить новое состояние
        needsFeeding = false;
        hasProductReady = false;
        hasFertilizerReady = false;

        Debug.Log($"[Transition] Начинаем переход из состояния {productionState} для {animalData.speciesName}.");

        // Последовательно проверяем, куда двигаться дальше
        if (productionState == AnimalProductionState.WaitingForFeed)
        {
            // После кормления пытаемся перейти к производству продукта
            if (animalData.productAmount > 0)
            {
                productionState = AnimalProductionState.ReadyForProduct;
                cycleTimer = animalData.productionInterval;
                Debug.Log($"[Transition] -> Состояние: ReadyForProduct. Таймер: {cycleTimer} сек.");
            }
            // Если продукт не производится, пытаемся перейти к удобрению
            else if (animalData.fertilizerAmount > 0)
            {
                productionState = AnimalProductionState.ReadyForFertilizer;
                cycleTimer = animalData.fertilizerInterval;
                Debug.Log($"[Transition] (Продукт пропущен) -> Состояние: ReadyForFertilizer. Таймер: {cycleTimer} сек.");
            }
            // Если ничего не производится, возвращаемся к кормежке
            else
            {
                productionState = AnimalProductionState.WaitingForFeed;
                cycleTimer = animalData.feedInterval;
                Debug.LogWarning($"[Transition] (Продукт и удобрение пропущены) -> Состояние: WaitingForFeed. Таймер: {cycleTimer} сек.");
            }
        }
        else if (productionState == AnimalProductionState.ReadyForProduct)
        {
            // После сбора продукта пытаемся перейти к производству удобрения
            if (animalData.fertilizerAmount > 0)
            {
                productionState = AnimalProductionState.ReadyForFertilizer;
                cycleTimer = animalData.fertilizerInterval;
                Debug.Log($"[Transition] -> Состояние: ReadyForFertilizer. Таймер: {cycleTimer} сек.");
            }
            // Если удобрение не производится, возвращаемся к кормежке
            else
            {
                productionState = AnimalProductionState.WaitingForFeed;
                cycleTimer = animalData.feedInterval;
                Debug.Log($"[Transition] (Удобрение пропущено) -> Состояние: WaitingForFeed. Таймер: {cycleTimer} сек.");
            }
        }
        else // ReadyForFertilizer
        {
            // После сбора удобрения всегда возвращаемся к кормежке
            productionState = AnimalProductionState.WaitingForFeed;
            cycleTimer = animalData.feedInterval;
            Debug.Log($"[Transition] -> Состояние: WaitingForFeed. Таймер: {cycleTimer} сек.");
        }

        // После всех переходов скрываем облачко и проверяем новые потребности (если таймер уже истек)
        HideThoughtBubble();
        CheckNeeds();
    }

    public void AttemptInteraction()
    {
        if (inventoryManager == null)
        {
            Debug.LogError("Нет ссылки на InventoryManager!");
            return;
        }

        bool interactionSuccessful = false;

        // 1. Попытка покормить
        if (needsFeeding)
        {
            InventoryItem selectedItem = inventoryManager.GetSelectedItem();
            if (selectedItem != null && !selectedItem.IsEmpty && selectedItem.itemData == animalData.requiredFood)
            {
                inventoryManager.RemoveItem(inventoryManager.SelectedSlotIndex, 1);
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.AddQuestProgress(GoalType.FeedAnimal, animalData.speciesName, 1);
                }
                Debug.Log($"<color=green>УСПЕХ:</color> {animalData.speciesName} покормлен.");
                interactionSuccessful = true;

                if (animalAnimator != null)
                {
                    animalAnimator.SetTrigger("doEat");
                    Debug.Log($"Запущена анимация кормления для {animalData.speciesName}");
                }
            }
        }
        // 2. Попытка собрать продукт (яйца, молоко)
        else if (hasProductReady)
        {
            bool toolCheckPassed = true;
            if (animalData.requiredToolForHarvest != null)
            {
                InventoryItem selectedItem = inventoryManager.GetSelectedItem();
                if (selectedItem == null || selectedItem.IsEmpty || selectedItem.itemData != animalData.requiredToolForHarvest)
                {
                    toolCheckPassed = false;
                    Debug.Log($"Не удалось собрать продукт: нужен инструмент '{animalData.requiredToolForHarvest.itemName}'");
                }
            }

            if (toolCheckPassed)
            {
                if (inventoryManager.AddItem(animalData.productProduced, animalData.productAmount))
                {
                    CheckForAchievement(); // Для ачивки
                    Debug.Log($"<color=green>УСПЕХ:</color> Продукт '{animalData.productProduced.itemName}' собран.");
                    interactionSuccessful = true;
                    animalAnimator.SetBool("doCut", false);
                }
                else
                {
                    Debug.Log("Не удалось собрать продукт - инвентарь полон!");
                }
            }


        }
        // 3. Попытка собрать удобрение
        else if (hasFertilizerReady)
        {
            if (inventoryManager.AddItem(animalData.fertilizerProduced, animalData.fertilizerAmount))
            {
                Debug.Log($"<color=green>УСПЕХ:</color> Удобрение '{animalData.fertilizerProduced.itemName}' собрано.");
                interactionSuccessful = true;
            }
            else
            {
                Debug.Log("Не удалось собрать удобрение - инвентарь полон!");
            }
        }

        // Если любое из взаимодействий было успешным
        if (interactionSuccessful)
        {
            // Главное исправление: вызываем новый, надежный метод перехода состояния
            TransitionToNextProductionState();

            // Перезапускаем блуждание животного, чтобы оно не стояло на месте
            currentState = AnimalState.Idle;
            SetNewStateTimer(currentState); // Устанавливаем таймер для нового состояния (Idle)
            StopAllCoroutines(); // Останавливаем все предыдущие корутины
            StartCoroutine(StateMachineCoroutine()); // Запускаем машину состояний движения
            StartCoroutine(RandomSoundCoroutine()); // Запускаем проигрывание звуков
        }
    }

    private void OnDestroy()
    {
        Debug.Log($"<color=red>[AnimalController]</color> {gameObject.name} уничтожается (OnDestroy). Вызываю SaveState().");
        SaveState();
        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
        }
    }

    void CheckForAchievement(string name)
    {
        if (AchievementManager.allTpyesAnimal.Contains(name))
        {
            if (AchievementManager.allTpyesAnimal.Remove(name))
            {
                GameEvents.TriggerAddedNewAnimal(1);
            }
            else
            {
                Debug.LogWarning($"This type of animal {name} is can not remove!");

            }
        }

    }
    void CheckForAchievement()
    {
        GameEvents.TriggerCollectAnimalProduct(1);
    }
    private IEnumerator RandomSoundCoroutine()
    {
        while (true)
        {
            float delay = Random.Range(5f, 100f);
            yield return new WaitForSeconds(delay);
            PlayRandomSound();
        }
    }
    private void PlayRandomSound()
    {
        if (animalData.animalSounds != null && animalData.animalSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = animalData.animalSounds[Random.Range(0, animalData.animalSounds.Length)];
            audioSource.PlayOneShot(clip);
            Debug.Log($"Звук животного ({animalData.speciesName}): {clip.name}");
        }
    }
}
