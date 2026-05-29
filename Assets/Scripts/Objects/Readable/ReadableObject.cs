using UnityEngine;
using UnityEngine.Events;

namespace Objects
{
    public class ReadableObject : Interactable
    {
        [Header("Данные носителя")]
        public ReadableData data;

        [Header("Настройки подсказки")]
        public string interactionPrompt = "[E] Осмотреть";

        public UnityEvent OnClosedEvent;

        public override void OnInteract()
        {
            if (!canPlayerInteract || !isWorking || data == null) return;

            // Вызываем базовое событие (если нужно для звуков и тд)
            base.OnInteract();

            // Открываем UI
            ReadableUI.Instance.Open(data, OnClosed);
        }

        private void OnClosed()
        {
            // Здесь можно добавить логику после того, как игрок закончил чтение
            // Например, пометить что книга прочитана
            Debug.Log($"Игрок закончил читать {data.title}");

            OnClosedEvent.Invoke();
        }

        public override void OnHighlight()
        {
            base.OnHighlight();

            // Мы можем переопределить стандартную подсказку PlayerUI здесь, 
            // но текущая архитектура PlayerInteraction сама вызывает PlayerUI.ShowInteractionPrompt.
            // Поэтому нам нужно убедиться, что PlayerInteraction корректно обрабатывает наш тип.
        }
    }
}
