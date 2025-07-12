using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ќпредел€ем "контракт", которому должны следовать все наши панели
public interface IUIManageable
{
    void CloseUI();
    bool IsOpen();
}

public class ExclusiveUIManager : MonoBehaviour
{
    public static ExclusiveUIManager Instance { get; private set; }

    // —писок всех панелей, которые хот€т, чтобы ими управл€ли
    private List<IUIManageable> registeredPanels = new List<IUIManageable>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // ћетод дл€ регистрации панели в нашей системе
    public void Register(IUIManageable panel)
    {
        if (!registeredPanels.Contains(panel))
        {
            registeredPanels.Add(panel);
        }
    }

    // ћетод дл€ удалени€ панели из системы (на вс€кий случай)
    public void Deregister(IUIManageable panel)
    {
        if (registeredPanels.Contains(panel))
        {
            registeredPanels.Remove(panel);
        }
    }

    // √лавный метод! ¬ызываетс€ панелью, котора€ хочет открытьс€.
    public void NotifyPanelOpening(IUIManageable openingPanel)
    {
        // ѕроходим по всем зарегистрированным панел€м
        foreach (var panel in registeredPanels)
        {
            // ≈сли это не та панель, котора€ открываетс€, » она сейчас открыта...
            if (panel != openingPanel && panel.IsOpen())
            {
                // ...закрываем ее!
                panel.CloseUI();
            }
        }
    }
}