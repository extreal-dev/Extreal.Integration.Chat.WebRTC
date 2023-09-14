using System;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Abstract class that becomes the base of text chat client classes.
    /// </summary>
    public abstract class TextChatClient : DisposableBase
    {
        /// <summary>
        /// Container for disposable objects.
        /// </summary>
        /// <returns>Composite disposable.</returns>
        protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

        /// <summary>
        /// <para>Invokes immediately after received a message.</para>
        /// Arg: Received message
        /// </summary>
        public IObservable<string> OnMessageReceived => onMessageReceived.AddTo(Disposables);
        private readonly Subject<string> onMessageReceived = new Subject<string>();

        /// <inheritdoc/>
        protected override void ReleaseManagedResources() => Disposables.Dispose();

        /// <summary>
        /// Publishes that a message has been received.
        /// </summary>
        /// <param name="message">Received message.</param>
        protected void FireOnMessageReceived(string message) => onMessageReceived.OnNext(message);

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        public void Send(string message)
        {
            DoSend(message);
            FireOnMessageReceived(message);
        }

        /// <summary>
        /// Uses for sending a message.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        protected abstract void DoSend(string message);

        /// <summary>
        /// Clears the status of this instance.
        /// </summary>
        public abstract void Clear();
    }
}
