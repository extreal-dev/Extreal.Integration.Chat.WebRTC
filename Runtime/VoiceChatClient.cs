using System;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Abstract class that becomes the base of voice chat client classes.
    /// </summary>
    public abstract class VoiceChatClient : DisposableBase
    {
        /// <summary>
        /// Container for disposable objects.
        /// </summary>
        /// <returns>Composite disposable.</returns>
        protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

        /// <summary>
        /// <para>Invokes immediately after the mute status is changed.</para>
        /// Arg: True if muted, false otherwise
        /// </summary>
        public IObservable<bool> OnMuted => onMuted.AddTo(Disposables);
        private readonly Subject<bool> onMuted = new Subject<bool>();

        /// <inheritdoc/>
        protected override void ReleaseManagedResources() => Disposables.Dispose();

        /// <summary>
        /// Publishes that the mute status is changed.
        /// </summary>
        /// <param name="muted">True if muted, false otherwise</param>
        protected void FireOnMuted(bool muted) => onMuted.OnNext(muted);

        /// <summary>
        /// Toggles mute or not.
        /// </summary>
        public abstract void ToggleMute();

        /// <summary>
        /// Clears the status of this instance.
        /// </summary>
        public abstract void Clear();
    }
}
