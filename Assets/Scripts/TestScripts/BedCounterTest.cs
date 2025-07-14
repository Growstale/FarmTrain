using UnityEngine;

public class BedCounterTest : MonoBehaviour
{
    void Update()
    {
        // При нажатии на клавишу B в консоль выведется информация
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Получаем количество грядок в инвентаре
            int bedsInInventory = InventoryManager.Instance.GetTotalItemQuantityByType(ItemType.Pot);

            // Получаем количество грядок в вагоне
            int bedsOnWagon = PlantManager.instance.GetPlacedBedsCount();

            // Выводим результат
            Debug.Log($"--- Статистика по грядкам ---");
            Debug.Log($"В инвентаре: {bedsInInventory} шт.");
            Debug.Log($"Установлено в вагоне: {bedsOnWagon} шт.");
            Debug.Log($"-----------------------------");
        }
    }
}