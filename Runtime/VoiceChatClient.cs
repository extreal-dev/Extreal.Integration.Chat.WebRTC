using System;
using System.Collections.Generic;
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
        /// Returns whether or not a microphone is available.
        /// </summary>
        /// <returns>True if it is available, false otherwise</returns>
        public abstract bool HasMicrophone();

        /// <summary>
        /// Toggles mute or not.
        /// </summary>
        public abstract void ToggleMute();

        /// <summary>
        /// Sets microphone volume.
        /// </summary>
        /// <param name="volume">volume to set</param>
        public abstract void SetInVolume(float volume);

        /// <summary>
        /// Sets speakers volume.
        /// </summary>
        /// <param name="volume">volume to set</param>
        public abstract void SetOutVolume(float volume);

        /// <summary>
        /// Clears the status of this instance.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Returns own audio level.
        /// </summary>
        /// <value>local audio level (-80 dB ~ 0 dB)</value>
        public abstract float LocalAudioLevel { get; }

        /// <summary>
        /// Returns other participants' audio levels.
        /// </summary>
        /// <value>other participants' id and audio levels (-80 dB ~ 0 dB) pair</value>
        public abstract IReadOnlyDictionary<string, float> RemoteAudioLevelList { get; }
    }
}
