#if UNITY_WEBGL
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// class that handles voice chat client for WebGL application.
    /// </summary>
    public class WebGLVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLVoiceChatClient));

        /// <summary>
        /// Creates WebGLVoiceChatClient with voiceChatConfig.
        /// </summary>
        /// <param name="voiceChatConfig">Voice chat config.</param>
        public WebGLVoiceChatClient(VoiceChatConfig voiceChatConfig)
        {
            var config = new WebGLVoiceChatConfig
            {
                initialMute = voiceChatConfig.InitialMute,
                isDebug = Logger.IsDebug()
            };
            var jsonVoiceJsonChatConfig = JsonSerializer.Serialize(config);
            WebGLHelper.CallAction(WithPrefix(nameof(WebGLVoiceChatClient)), jsonVoiceJsonChatConfig);
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
        public override void SetMicVolume(float volume) => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public override void SetSpeakersVolume(float volume) => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLVoiceChatClient)}#{name}";
    }

    /// <summary>
    /// Class that holds config for WebGL voice chat.
    /// </summary>
    [SuppressMessage("Usage", "IDE1006")]
    public class WebGLVoiceChatConfig
    {
        public bool initialMute { get; set; }
        public bool isDebug { get; set; }
    }
}
#endif
