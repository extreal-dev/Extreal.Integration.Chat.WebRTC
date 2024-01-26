using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extreal.Core.Common.System;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.TextChatControl
{
    public class TextChatControlPresenter : DisposableBase, IInitializable
    {
        private readonly TextChatClient textChatClient;
        private readonly TextChatControlView textChatControlView;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public TextChatControlPresenter
        (
            TextChatClient textChatClient,
            TextChatControlView textChatControlView
        )
        {
            this.textChatClient = textChatClient;
            this.textChatControlView = textChatControlView;
        }

        public void Initialize()
        {
            textChatControlView.OnSendButtonClicked
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Subscribe(textChatClient.Send)
                .AddTo(disposables);

            textChatClient.OnMessageReceived
                .Subscribe(textChatControlView.ShowMessage)
                .AddTo(disposables);

            textChatControlView.Initialize();
        }

        protected override void ReleaseManagedResources()
        {
            textChatClient.Clear();
            disposables.Dispose();
        }
    }
}
