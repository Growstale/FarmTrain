using UnityEngine;

public class SceneTransitionButton : MonoBehaviour
{
    // Этот публичный метод будет вызываться из события OnClick() на кнопке.
    // Он не требует никаких параметров и ссылок в инспекторе.
    public void GoToTrainScene()
    {
        // Проверяем, существует ли наш менеджер в игре
        if (TransitionManager.Instance != null)
        {
            // Находим синглтон через код и вызываем его метод
            TransitionManager.Instance.GoToTrainScene();
        }
        else
        {
            // Это сообщение поможет, если вы забудете добавить TransitionManager на сцену Initializer
            Debug.LogError("TransitionManager не найден в игре! Невозможно перейти на сцену поезда.");
        }
    }
}