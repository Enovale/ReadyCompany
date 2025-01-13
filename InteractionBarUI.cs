using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

namespace ReadyCompany
{
    public class InteractionBarUI : MonoBehaviour
    {
        private Image image = null!;

        public static InteractionBarUI Instance { get; private set; } = null!;
        
        internal IInputInteraction? ReadyInteraction { get; set; }
        internal IInputInteraction? UnreadyInteraction { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            image = GetComponent<Image>();
        }

        private void Update()
        {
            if (ReadyCompany.InputActions == null)
                return;

            var percentage = 0f;
            if (ReadyInteraction != null && !ReadyHandler.ReadyStatus.Value.LocalPlayerReady)
            {
                image.color = ReadyCompany.Config.ReadyBarColor.Value;
                if (ReadyInteraction is MultiTapInteraction m)
                {
                    percentage = (float)m.m_CurrentTapCount / m.tapCount;
                }
                else if (ReadyInteraction is HoldInteraction h)
                {
                    percentage = (float)(Time.realtimeSinceStartupAsDouble - h.m_TimePressed) / h.durationOrDefault;
                }
            }
            else if (UnreadyInteraction != null && ReadyHandler.ReadyStatus.Value.LocalPlayerReady)
            {
                image.color = ReadyCompany.Config.UnreadyBarColor.Value;
                if (UnreadyInteraction is MultiTapInteraction m)
                {
                    percentage = (float)m.m_CurrentTapCount / m.tapCount;
                }
                else if (UnreadyInteraction is HoldInteraction h)
                {
                    percentage = (float)(Time.realtimeSinceStartupAsDouble - h.m_TimePressed) / h.durationOrDefault;
                }
            }
            
            UpdatePercentage(percentage);
        }

        public void UpdatePercentage(float percentage)
        {
            image.fillAmount = percentage;
        }
    }
}