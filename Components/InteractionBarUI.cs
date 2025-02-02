using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

namespace ReadyCompany.Components
{
    public class InteractionBarUI : MonoBehaviour
    {
        private Image image = null!;

        public static InteractionBarUI Instance { get; private set; } = null!;

        internal IInputInteraction? ReadyInteraction;
        internal IInputInteraction? UnreadyInteraction;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            image = GetComponent<Image>();
        }

        private void Update()
        {
            if (ReadyCompany.InputActions == null)
                return;

            var percentage = 0f;
            if (ReadyInteraction != null && ReadyHandler.ReadyStatus is { Value.LocalPlayerReady: false })
            {
                image.color = ReadyCompany.Config.ReadyBarColor.Value;
                if (ReadyInteraction is MultiTapInteraction m)
                    percentage = (float)m.m_CurrentTapCount / m.tapCount;
                else if (ReadyInteraction is HoldInteraction h)
                    percentage = (float)(Time.realtimeSinceStartupAsDouble - h.m_TimePressed) / h.durationOrDefault;
            }
            else if (UnreadyInteraction != null && ReadyHandler.ReadyStatus is { Value.LocalPlayerReady: true })
            {
                image.color = ReadyCompany.Config.UnreadyBarColor.Value;
                if (UnreadyInteraction is MultiTapInteraction m)
                    percentage = (float)m.m_CurrentTapCount / m.tapCount;
                else if (UnreadyInteraction is HoldInteraction h)
                    percentage = (float)(Time.realtimeSinceStartupAsDouble - h.m_TimePressed) / h.durationOrDefault;
            }

            UpdatePercentage(percentage);
        }

        public void UpdatePercentage(float percentage)
        {
            image.fillAmount = percentage;
        }
    }
}