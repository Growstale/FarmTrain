using UnityEngine;
using TMPro;

public class StationPhaseController : MonoBehaviour
{

    void Start()
    {
        ExperienceManager.Instance.OnPhaseUnlocked += OnPhaseUnlocked;

        int currentLevel = ExperienceManager.Instance.CurrentLevel;

        // Если для этой фазы станции не нужно копить опыт (XP = 0)
        if (ExperienceManager.Instance.XpForNextPhase == 0)
        {
            // Сразу сообщаем главному менеджеру, что можно уезжать
            TransitionManager.Instance.UnlockDeparture();
        }
    }

    void OnDestroy() // <<< ИЗМЕНЕНО с OnDisable на OnDestroy
    {
        if (ExperienceManager.Instance != null)
        {
            ExperienceManager.Instance.OnPhaseUnlocked -= OnPhaseUnlocked;
        }
    }

    private void OnPhaseUnlocked(int level, GamePhase phase)
    {
        if (phase == GamePhase.Station)
        {
            // Сообщаем главному менеджеру, что теперь можно отправляться
            TransitionManager.Instance.UnlockDeparture();
        }
    }
}