using UnityEngine;
using System;
using TMPro; 

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [SerializeField] private int startingMoney = 500;

    private int currentMoney;

    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentMoney = startingMoney;
        }
    }

    public int GetCurrentMoney()
    {
        return currentMoney;
    }

    public bool HasEnoughMoney(int amount)
    {
        return currentMoney >= amount;
    }

    public void AddMoney(int amount)
    {
        if (amount < 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"Добавлено {amount} денег. Всего: {currentMoney}");
    }

    public void SpendMoney(int amount)
    {
        if (amount < 0 || !HasEnoughMoney(amount)) return;
        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"Потрачено {amount} денег. Всего: {currentMoney}");
    }

}