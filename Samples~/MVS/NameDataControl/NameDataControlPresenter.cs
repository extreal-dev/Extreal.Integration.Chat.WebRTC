using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using Extreal.Integration.Chat.WebRTC.MVS.App;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public class NameDataControlPresenter : DisposableBase, IInitializable
    {
        private readonly AppState appState;
        private readonly NameDataClient nameDataClient;
        private readonly PeerClient peerClient;


        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public NameDataControlPresenter(
            AppState appState,
            NameDataClient nameDataClient,
            PeerClient peerClient
        )
        {
            this.appState = appState;
            this.nameDataClient = nameDataClient;
            this.peerClient = peerClient;
        }

        public void Initialize()
        {
            nameDataClient.OnOpen
                .Subscribe(async id =>
                {
                    // Wait to not Send before pc.OnDataChannel
                    await UniTask.Delay(50);
                    nameDataClient.Send(id, appState.Name);
                })
                .AddTo(disposables);

            nameDataClient.OnClose
                .Subscribe(appState.RemoveNameDict)
                .AddTo(disposables);

            nameDataClient.OnMessageReceived
                .Subscribe(values => appState.SetNameDict(values.from, values.name))
                .AddTo(disposables);

            peerClient.OnStarted
                .Subscribe(id => appState.SetNameDict(id, appState.Name))
                .AddTo(disposables);

            peerClient.OnUserDisconnecting
                .Subscribe(appState.RemoveNameDict)
                .AddTo(disposables);

            peerClient.OnDisconnected
                .Subscribe(_ => appState.ClearNameDict())
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources()
        {
            nameDataClient.Clear();
            appState.ClearNameDict();
            disposables.Dispose();
        }
    }
}
