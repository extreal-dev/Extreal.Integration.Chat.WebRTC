using System;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC
{
    public abstract class VoiceChatClient : DisposableBase
    {
        protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

        public IObservable<bool> OnMuted => onMuted.AddTo(Disposables);
        private readonly Subject<bool> onMuted = new Subject<bool>();

        protected override void ReleaseManagedResources() => Disposables.Dispose();

        protected void FireOnMuted(bool muted) => onMuted.OnNext(muted);

        public abstract void ToggleMute();

        public abstract void Clear();
    }
}
