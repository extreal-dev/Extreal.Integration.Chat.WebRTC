using System.Collections.Generic;
using System.Text;
using Extreal.Core.Common.System;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.VoiceChatControl
{
    public class VoiceChatControlPresenter : DisposableBase, IInitializable, ITickable
    {
        private readonly VoiceChatClient voiceChatClient;
        private readonly VoiceChatControlView voiceChatControlView;
        private readonly VoiceChatConfig voiceChatConfig;
        private readonly Dictionary<string, float> audioLevels = new Dictionary<string, float>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private float time;

        public VoiceChatControlPresenter
        (
            VoiceChatClient voiceChatClient,
            VoiceChatControlView voiceChatControlView,
            VoiceChatConfig voiceChatConfig,
            PeerClient peerClient
        )
        {
            this.voiceChatClient = voiceChatClient;
            this.voiceChatControlView = voiceChatControlView;
            this.voiceChatConfig = voiceChatConfig;
            this.peerClient = peerClient;
        }

        public void Initialize()
        {
            voiceChatControlView.OnMuteButtonClicked
                .Subscribe(_ => voiceChatClient.ToggleMute())
                .AddTo(disposables);

            voiceChatClient.OnMuted
                .Subscribe(voiceChatControlView.ToggleMute)
                .AddTo(disposables);

            voiceChatControlView.OnMicVolumeSliderChanged
                .Subscribe(volume =>
                {
                    voiceChatClient.SetInVolume(volume);
                    voiceChatControlView.SetMicVolumeText(volume);
                })
                .AddTo(disposables);

            voiceChatControlView.OnSpeakersVolumeSliderChanged
                .Subscribe(volume =>
                {
                    voiceChatClient.SetOutVolume(volume);
                    voiceChatControlView.SetSpeakersVolumeText(volume);
                })
                .AddTo(disposables);

            voiceChatClient.OnAudioLevelChanged
                .Subscribe(audioLevelList =>
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var audioLevelChange in audioLevelList)
                    {
                        if (audioLevelChange.State is AudioLevelChangeState.New or AudioLevelChangeState.Change)
                        {
                            audioLevels[audioLevelChange.Id] = audioLevelChange.Value;
                        }
                        else
                        {
                            audioLevels.Remove(audioLevelChange.Id);
                        }
                    }
                    foreach (var id in audioLevels.Keys)
                    {
                        stringBuilder.Append($"{id}: {audioLevels[id]:f4}{System.Environment.NewLine}");
                    }
                    voiceChatControlView.SetAudioLevelsText(stringBuilder.ToString());
                })
                .AddTo(disposables);

            voiceChatControlView.Initialize(voiceChatConfig.InitialMute);
        }

        protected override void ReleaseManagedResources()
        {
            voiceChatClient.Clear();
            disposables.Dispose();
        }
    }
}
