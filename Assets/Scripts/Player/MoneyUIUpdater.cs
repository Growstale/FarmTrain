using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyUIUpdater : MonoBehaviour
{
    private TextMeshProUGUI moneyText;

    private void Awake()
    {
        moneyText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("PlayerWallet.Instance не найден! Убедитесь, что сцена с GameManager загружена первой.");
            moneyText.enabled = false;
            return;
        }

        PlayerWallet.Instance.OnMoneyChanged += UpdateMoneyText;

        UpdateMoneyText(PlayerWallet.Instance.GetCurrentMoney());
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnMoneyChanged -= UpdateMoneyText;
        }
    }

    private void UpdateMoneyText(int newAmount)
    {
        moneyText.text = $"{newAmount} BYN";
    }
}