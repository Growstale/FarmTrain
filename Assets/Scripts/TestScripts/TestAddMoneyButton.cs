using UnityEngine;
using UnityEngine.UI;

// Этот скрипт можно повесить на любой GameObject с компонентом Button в UI.
[RequireComponent(typeof(Button))]
public class TestAddMoneyButton : MonoBehaviour
{
    [Tooltip("Какую сумму денег добавлять за одно нажатие.")]
    [SerializeField] private int amountToAdd = 50;

    private Button testButton;

    void Start()
    {
        // Получаем ссылку на компонент кнопки
        testButton = GetComponent<Button>();

        // Проверяем, что PlayerWallet существует на сцене
        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("TestAddMoneyButton: PlayerWallet не найден на сцене! Кнопка будет отключена.");
            testButton.interactable = false; // Отключаем кнопку, чтобы избежать ошибок
            return;
        }

        // Привязываем наш метод к событию onClick кнопки
        testButton.onClick.AddListener(AddMoneyForTest);
    }

    /// <summary>
    /// Этот метод будет вызываться при нажатии на кнопку.
    /// </summary>
    private void AddMoneyForTest()
    {
        // Проверяем еще раз на всякий случай
        if (PlayerWallet.Instance != null)
        {
            Debug.Log($"<color=lime>[TEST BUTTON]</color> Нажата кнопка добавления денег. Добавляем {amountToAdd}.");

            // Вызываем публичный метод из PlayerWallet
            PlayerWallet.Instance.AddMoney(amountToAdd);

            // PlayerWallet.AddMoney() сам вызовет все нужные события (OnMoneyAdded и OnMoneyChanged),
            // поэтому QuestManager и другие подписчики получат уведомление автоматически.
        }
        else
        {
            Debug.LogError("TestAddMoneyButton: Попытка добавить деньги, но PlayerWallet.Instance равен null!");
        }
    }

    // Хорошая практика - отписываться от событий, когда объект уничтожается
    void OnDestroy()
    {
        if (testButton != null)
        {
            testButton.onClick.RemoveListener(AddMoneyForTest);
        }
    }
}