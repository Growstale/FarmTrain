using UnityEngine;
using TMPro;

public class StationPhaseController : MonoBehaviour
{

    void Start()
    {
        ExperienceManager.Instance.OnPhaseUnlocked += OnPhaseUnlocked;

        int currentLevel = ExperienceManager.Instance.CurrentLevel;

        if (stationTitle != null)
        {
            stationTitle.text = $"STATION {currentLevel}";
        }

        // Åñëè äëÿ ýòîé ôàçû ñòàíöèè íå íóæíî êîïèòü îïûò (XP = 0)
        if (ExperienceManager.Instance.XpForNextPhase == 0)
        {
            // Ñðàçó ñîîáùàåì ãëàâíîìó ìåíåäæåðó, ÷òî ìîæíî óåçæàòü
            TransitionManager.Instance.UnlockDeparture();
        }
    }

    void OnDestroy() // <<< ÈÇÌÅÍÅÍÎ ñ OnDisable íà OnDestroy
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
            // Ñîîáùàåì ãëàâíîìó ìåíåäæåðó, ÷òî òåïåðü ìîæíî îòïðàâëÿòüñÿ
            TransitionManager.Instance.UnlockDeparture();
        }
    }
}