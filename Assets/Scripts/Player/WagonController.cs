// WagonController.cs
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WagonController : MonoBehaviour
{
    private Animator wagonAnimator;

    private void Awake()
    {
        wagonAnimator = GetComponent<Animator>();

        LocomotiveController.OnLocomotiveReady += Initialize;
    }

    private void OnDestroy()
    {
        // Отписываемся от обоих событий при уничтожении вагона
        if (LocomotiveController.Instance != null)
        {
            LocomotiveController.Instance.OnTrainStateChanged -= HandleTrainStateChange;
        }
        LocomotiveController.OnLocomotiveReady -= Initialize;
    }

    private void Initialize()
    {
        // Теперь мы уверены, что LocomotiveController.Instance не null.
        if (LocomotiveController.Instance != null)
        {
            // 1. Подписываемся на будущие изменения состояния
            LocomotiveController.Instance.OnTrainStateChanged += HandleTrainStateChange;

            // 2. Сразу же устанавливаем правильное начальное состояние
            bool isCurrentlyMoving = LocomotiveController.Instance.currentState == LocomotiveController.TrainState.Moving;
            HandleTrainStateChange(isCurrentlyMoving);
        }
    }

    private void HandleTrainStateChange(bool isMoving)
    {
        if (wagonAnimator != null)
        {
            wagonAnimator.SetBool("isMoving", isMoving);
            Debug.Log($"<color=yellow>[Wagon]</color> Вагон '{gameObject.name}' получил команду и установил isMoving в: {isMoving}");
        }
    }
}