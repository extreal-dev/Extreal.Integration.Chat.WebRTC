using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.VoiceChatControl
{
    public class VoiceChatControlView : MonoBehaviour
    {
        [SerializeField] private Button muteButton;
        [SerializeField] private TMP_Text mutedString;

        public IObservable<Unit> OnMuteButtonClicked
            => muteButton.OnClickAsObservable().TakeUntilDestroy(this);

        private string muteOffButtonLabel;
        private string muteOnButtonLabel;

        public void Initialize(bool initialMute)
        {
            muteOffButtonLabel = "OFF";
            muteOnButtonLabel = "ON";
            ToggleMute(initialMute);
        }

        public void ToggleMute(bool isMute)
            => mutedString.text = isMute ? muteOffButtonLabel : muteOnButtonLabel;
    }
}
