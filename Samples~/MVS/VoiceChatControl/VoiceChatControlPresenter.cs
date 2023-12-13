using System.Diagnostics;
using System.Text;
using Extreal.Core.Common.System;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.VoiceChatControl
{
    public class VoiceChatControlPresenter : DisposableBase, IInitializable
    {
        private readonly VoiceChatClient voiceChatClient;
        private readonly VoiceChatControlView voiceChatControlView;
        private readonly VoiceChatConfig voiceChatConfig;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private float time;

        public VoiceChatControlPresenter
        (
            VoiceChatClient voiceChatClient,
            VoiceChatControlView voiceChatControlView,
            VoiceChatConfig voiceChatConfig
        )
        {
            this.voiceChatClient = voiceChatClient;
            this.voiceChatControlView = voiceChatControlView;
            this.voiceChatConfig = voiceChatConfig;
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
                    foreach (var id in audioLevelList.Keys)
                    {
                        stringBuilder.Append($"{id}: {audioLevelList[id]:f3}{System.Environment.NewLine}");
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
