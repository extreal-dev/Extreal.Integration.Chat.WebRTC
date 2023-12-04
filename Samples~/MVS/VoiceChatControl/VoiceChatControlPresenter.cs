using System.Text;
using Extreal.Core.Common.System;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.VoiceChatControl
{
    public class VoiceChatControlPresenter : DisposableBase, IInitializable, ITickable
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
                    voiceChatClient.SetMicVolume(volume);
                    voiceChatControlView.SetMicVolumeText(volume);
                })
                .AddTo(disposables);

            voiceChatControlView.OnSpeakersVolumeSliderChanged
                .Subscribe(volume =>
                {
                    voiceChatClient.SetSpeakersVolume(volume);
                    voiceChatControlView.SetSpeakersVolumeText(volume);
                })
                .AddTo(disposables);

            voiceChatControlView.Initialize(voiceChatConfig.InitialMute);
        }

        public void Tick()
        {
            time += UnityEngine.Time.deltaTime;
            if (time >= 0.25f)
            {
                time -= 0.25f;
                var stringBuilder = new StringBuilder();
                var localAudioLevel = voiceChatClient.LocalAudioLevel;
                stringBuilder.Append($"local: {localAudioLevel:f2} dB{System.Environment.NewLine}");
                var remoteAudioLevelList = voiceChatClient.RemoteAudioLevelList;
                foreach (var id in remoteAudioLevelList.Keys)
                {
                    stringBuilder.Append($"{id[..8]}…: {remoteAudioLevelList[id]:f2} dB{System.Environment.NewLine}");
                }
                voiceChatControlView.SetAudioLevelsText(stringBuilder.ToString());
            }
        }

        protected override void ReleaseManagedResources()
        {
            voiceChatClient.Clear();
            disposables.Dispose();
        }
    }
}
