using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<bool> onMuted = new Subject<bool>();

        /// <summary>
        /// <para>Invokes at the time of Update immediately after the audio level has been changed.</para>
        /// Arg: Dictionary whose key is ID and value is audio level (contains unchanged values)
        /// </summary>
        public IObservable<IReadOnlyDictionary<string, float>> OnAudioLevelChanged => onAudioLevelChanged.AddTo(Disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<IReadOnlyDictionary<string, float>> onAudioLevelChanged = new Subject<IReadOnlyDictionary<string, float>>();

        /// <inheritdoc/>
        protected override void ReleaseManagedResources() => Disposables.Dispose();

        /// <summary>
        /// Publishes that the mute status is changed.
        /// </summary>
        /// <param name="muted">True if muted, false otherwise</param>
        protected void FireOnMuted(bool muted) => onMuted.OnNext(muted);

        /// <summary>
        /// Publishes that the audio levels are changed.
        /// </summary>
        /// <param name="audioLevelList">ID and audio level pairs (contains unchanged values)</param>
        protected void FireOnAudioLevelChanged(IReadOnlyDictionary<string, float> audioLevelList) => onAudioLevelChanged.OnNext(audioLevelList);

        /// <summary>
        /// Returns whether a microphone is available or not.
        /// </summary>
        /// <returns>True if it is available, false otherwise</returns>
        public abstract bool HasMicrophone();

        /// <summary>
        /// Toggles mute or not.
        /// </summary>
        public abstract void ToggleMute();

        /// <summary>
        /// Sets input volume.
        /// </summary>
        /// <param name="volume">volume to be set (0.0 - 1.0)</param>
        public abstract void SetInVolume(float volume);

        /// <summary>
        /// Sets output volume.
        /// </summary>
        /// <param name="volume">volume to be set (0.0 - 1.0)</param>
        public abstract void SetOutVolume(float volume);

        /// <summary>
        /// Clears the status of this instance.
        /// </summary>
        public abstract void Clear();
    }
}
