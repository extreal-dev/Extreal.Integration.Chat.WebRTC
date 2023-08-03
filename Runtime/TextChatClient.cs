using System;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC
{
    public abstract class TextChatClient : DisposableBase
    {
        protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

        public IObservable<string> OnMessageReceived => onMessageReceived.AddTo(Disposables);
        private readonly Subject<string> onMessageReceived = new Subject<string>();

        protected override void ReleaseManagedResources() => Disposables.Dispose();

        protected void FireOnMessageReceived(string message) => onMessageReceived.OnNext(message);

        public void Send(string message)
        {
            DoSend(message);
            FireOnMessageReceived(message);
        }

        protected abstract void DoSend(string message);

        public abstract void Clear();
    }
}
