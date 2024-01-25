#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AOT;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// class that handles voice chat client for WebGL application.
    /// </summary>
    public class WebGLVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLVoiceChatClient));

        private static WebGLVoiceChatClient instance;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>
        /// Creates WebGLVoiceChatClient with voiceChatConfig.
        /// </summary>
        /// <param name="voiceChatConfig">Voice chat config.</param>
        public WebGLVoiceChatClient(VoiceChatConfig voiceChatConfig)
        {
            instance = this;
            var config = new WebGLVoiceChatConfig
            {
                initialMute = voiceChatConfig.InitialMute,
                initialInVolume = voiceChatConfig.InitialInVolume,
                initialOutVolume = voiceChatConfig.InitialOutVolume,
                audioLevelCheckIntervalSeconds = voiceChatConfig.AudioLevelCheckIntervalSeconds,
                isDebug = Logger.IsDebug()
            };
            var jsonVoiceChatConfig = JsonSerializer.Serialize(config);
            WebGLHelper.CallAction(WithPrefix(nameof(WebGLVoiceChatClient)), jsonVoiceChatConfig);
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnAudioLevelChanged)), HandleOnAudioLevelChanged);
        }

        public override bool HasMicrophone()
        {
            var hasMicrophone = WebGLHelper.CallFunction(WithPrefix(nameof(HasMicrophone)));
            return bool.Parse(hasMicrophone);
        }

        /// <inheritdoc/>
        public override void ToggleMute()
        {
            if (!HasMicrophone())
            {
                return;
            }

            var muted = WebGLHelper.CallFunction(WithPrefix(nameof(ToggleMute)));
            FireOnMuted(bool.Parse(muted));
        }

        /// <inheritdoc/>
        public override void SetInVolume(float volume)
        {
            if (!HasMicrophone())
            {
                return;
            }
            WebGLHelper.CallAction(WithPrefix(nameof(SetInVolume)), volume.ToString());
        }

        /// <inheritdoc/>
        public override void SetOutVolume(float volume)
            => WebGLHelper.CallAction(WithPrefix(nameof(SetOutVolume)), volume.ToString());

        /// <inheritdoc/>
        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLVoiceChatClient)}#{name}";

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnAudioLevelChanged(string audioLevelsStr, string unused)
        {
            var audioLevels = JsonSerializer.Deserialize<Dictionary<string, float>>(audioLevelsStr);
            instance.FireOnAudioLevelChanged(audioLevels);
        }

        protected override void ReleaseManagedResources()
        {
            disposables.Dispose();
            base.ReleaseManagedResources();
        }
    }

    /// <summary>
    /// Class that holds config for WebGL voice chat.
    /// </summary>
    [SuppressMessage("Usage", "IDE1006"), SuppressMessage("Usage", "CC0047")]
    public class WebGLVoiceChatConfig
    {
        public bool initialMute { get; set; }
        public float initialInVolume { get; set; }
        public float initialOutVolume { get; set; }
        public float audioLevelCheckIntervalSeconds { get; set; }
        public bool isDebug { get; set; }
    }
}
#endif
