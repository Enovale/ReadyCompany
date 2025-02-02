using LethalNetworkAPI.Utils;
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
            ReadyHandler.ReadyStatusUpdated += ReadyStatusUpdated;
        }

        private void ReadyStatusUpdated(ReadyMap map)
        {
            var mapEmpty = map.LobbySize <= 0;
            _canPullLever = !mapEmpty && ReadyHandler.InVotingPhase &&
                            !StartOfRound.Instance.travellingToNewLevel && ReadyHandler.IsLobbyReady(map);
            _currentReadyMap = map;
            
            if (!mapEmpty)
                _lever.updateInterval = 0.000000001f;
        }

        private void FixedUpdate()
        {
            if (_canPullLever)
            {
                if (ReadyCompany.Config.AutoStartWhenReady.Value)
                {
                    if (ReadyCompany.Config.CountdownEnabled.Value)
                    {
                        var timeSinceReady = Time.time - _currentReadyMap.Timestamp;
                        var secondsSinceReady = Mathf.FloorToInt(timeSinceReady);

                        if (Mathf.FloorToInt(timeSinceReady - Time.fixedDeltaTime) < secondsSinceReady)
                        {
                            var countdownTime = ReadyCompany.Config.CountdownTime.Value - timeSinceReady;

                            CountdownUpdate(countdownTime);
                        }
                    }
                    else
                    {
                        PullLever();
                    }
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
            var roundedTime = Mathf.RoundToInt(time);
            var countdownStr = roundedTime;
            ReadyCompany.Logger.LogDebug($"{time} {countdownStr}");
            
            var hud = HUDManager.Instance;
            if (hud is null)
                return;

            var bodyText = $"Lever will pull in {countdownStr} second{(roundedTime is not (1 or -1) ? "s" : string.Empty)}";
            var sfx = ReadyCompany.Config.CustomCountdownSounds.ToArray();
            var clipToPlay = sfx;

            if (sfx.Length > 0)
                clipToPlay = [sfx[roundedTime % sfx.Length]];
            ReadyHandler.CustomDisplayTip("Ready Countdown", bodyText, clipToPlay, true);
            ReadyHandler.CustomDisplaySpectatorTip(bodyText);

            if (time <= 0)
                PullLever();
        }

        ~ReadyCountdown()
        {
            ReadyHandler.ReadyStatusUpdated -= ReadyStatusUpdated;
        }
    }
}