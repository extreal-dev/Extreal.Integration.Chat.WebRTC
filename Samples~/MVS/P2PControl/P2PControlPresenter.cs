using System;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Chat.WebRTC.MVS.App;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.P2PControl
{
    public class P2PControlPresenter : DisposableBase, IInitializable
    {
        private readonly StageNavigator<StageName, SceneName> stageNavigator;
        private readonly AppState appState;
        private readonly PeerClient peerClient;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private Action<string> handleHostNameAlreadyExistsException;

        public P2PControlPresenter(
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState,
            PeerClient peerClient)
        {
            this.stageNavigator = stageNavigator;
            this.appState = appState;
            this.peerClient = peerClient;
        }

        public void Initialize()
        {
            handleHostNameAlreadyExistsException = message =>
            {
                appState.Notify($"Received: {nameof(HostNameAlreadyExistsException)} {message}");
                stageNavigator.ReplaceAsync(StageName.GroupSelectionStage).Forget();
            };

            peerClient.OnConnectFailed
                .Subscribe(_ => appState.Notify($"Received: {nameof(PeerClient.OnConnectFailed)}"))
                .AddTo(disposables);

            peerClient.OnDisconnected
                .Subscribe(_ => appState.Notify($"Received: {nameof(PeerClient.OnDisconnected)}"))
                .AddTo(disposables);

            stageNavigator.OnStageTransitioned
                .Subscribe(_ => StartPeerClientAsync(appState).Forget())
                .AddTo(disposables);

            stageNavigator.OnStageTransitioning
                .Subscribe(_ => peerClient.Stop())
                .AddTo(disposables);
        }

        private async UniTask StartPeerClientAsync(AppState appState)
        {
            try
            {
                if (appState.IsHost)
                {
                    await peerClient.StartHostAsync(appState.GroupName);
                }
                else
                {
                    await peerClient.StartClientAsync(appState.GroupId);
                }
            }
            catch (HostNameAlreadyExistsException e)
            {
                handleHostNameAlreadyExistsException.Invoke(e.Message);
            }
        }

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
