using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;
using UnityEngine;

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
        [SuppressMessage("Style", "CC0057")]
        public WebGLVoiceChatClient(VoiceChatConfig voiceChatConfig)
            => WebGLHelper.CallAction(WithPrefix(nameof(WebGLVoiceChatClient)),
                JsonUtility.ToJson(new WebGLVoiceChatConfig
                {
                    initialMute = voiceChatConfig.InitialMute,
                    isDebug = Logger.IsDebug()
                }));

        /// <inheritdoc/>
        public override void ToggleMute()
        {
            var muted = WebGLHelper.CallFunction(WithPrefix(nameof(ToggleMute)));
            FireOnMuted(bool.Parse(muted));
        }

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
        /// <summary>
        /// Initial status of mute.
        /// </summary>
        public bool initialMute;

        /// <summary>
        /// Indicates if debug logs are output.
        /// </summary>
        public bool isDebug;
    }
}
