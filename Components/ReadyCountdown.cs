using System;
using LethalNetworkAPI.Utils;
using ReadyCompany.Util;
using UnityEngine;

namespace ReadyCompany.Components
{
    public class ReadyCountdown : MonoBehaviour
    {
        private StartMatchLever _lever = null!;
        private bool _canPullLever;
        private ReadyMap _currentReadyMap = null!;
        
        private void Awake()
        {
            _lever = GetComponent<StartMatchLever>();
            ReadyHandler.ReadyStatusChanged += ReadyStatusChanged;
        }

        private void ReadyStatusChanged(ReadyMap map)
        {
            var mapEmpty = map.LobbySize <= 0;
            _canPullLever = !mapEmpty && ReadyHandler.InVotingPhase && !StartOfRound.Instance.travellingToNewLevel && ReadyHandler.IsLobbyReady(map);
            _currentReadyMap = map;
            
            if (!mapEmpty)
                _lever.updateInterval = 0.000000001f;
        }

        private void FixedUpdate()
        {
            if (_canPullLever)
            {
                if (ReadyCompany.Config.AutoStartWhenReady.Value && ReadyCompany.Config.CountdownEnabled.Value)
                {
                    var timeSinceReady = Time.time - _currentReadyMap.Timestamp;
                    var secondsSinceReady = Mathf.FloorToInt(timeSinceReady);

                    if (Mathf.FloorToInt(timeSinceReady - (Time.fixedDeltaTime)) < secondsSinceReady)
                    {
                        var countdownTime = ReadyCompany.Config.CountdownTime.Value - timeSinceReady;

                        CountdownUpdate(countdownTime);
                    }
                }
                else if (ReadyCompany.Config.AutoStartWhenReady.Value)
                {
                    PullLever();
                }
            }
        }

        private void PullLever()
        {
            if (!LNetworkUtils.IsHostOrServer || !_canPullLever)
                return;
            
            var oldDead = GameNetworkManager.Instance.localPlayerController.isPlayerDead;
            GameNetworkManager.Instance.localPlayerController.isPlayerDead = false;
            _lever.LeverAnimation();
            _lever.PullLever();
            GameNetworkManager.Instance.localPlayerController.isPlayerDead = oldDead;
        }

        private void CountdownUpdate(float time)
        {
            var countdownStr = time.ToString("N0");
            ReadyCompany.Logger.LogDebug($"{time} {countdownStr}");
            
            var hud = HUDManager.Instance;
            if (hud is null)
                return;
            Utils.PlayRandomClip(hud.UIAudio, hud.warningSFX, ReadyCompany.Config.SoundVolume.Value / 100f);

            if (time <= 0)
            {
                PullLever();
            }
        }

        ~ReadyCountdown()
        {
            ReadyHandler.ReadyStatusChanged -= ReadyStatusChanged;
        }
    }
}