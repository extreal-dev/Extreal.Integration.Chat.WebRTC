using System;
using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public abstract class NameDataClient : DisposableBase
    {
        protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

        public IObservable<string> OnOpen => onOpen.AddTo(Disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onOpen = new Subject<string>();

        public IObservable<string> OnClose => onClose.AddTo(Disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onClose = new Subject<string>();

        public IObservable<(string from, string name)> OnMessageReceived => onMessageReceived.AddTo(Disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<(string, string)> onMessageReceived = new Subject<(string, string)>();

        protected override void ReleaseManagedResources() => Disposables.Dispose();

        protected void FireOnOpen(string id) => onOpen.OnNext(id);
        protected void FireOnClose(string id) => onClose.OnNext(id);

        protected void FireOnMessageReceived(string id, string message) => onMessageReceived.OnNext((id, message));

        public void Send(string to, string message) => DoSend(to, message);

        protected abstract void DoSend(string to, string message);

        public abstract void Clear();
    }
}
