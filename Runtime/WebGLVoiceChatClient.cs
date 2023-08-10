using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    public class WebGLVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLVoiceChatClient));

        [SuppressMessage("Style", "CC0057")]
        public WebGLVoiceChatClient(VoiceChatConfig voiceChatConfig)
            => WebGLHelper.CallAction(WithPrefix(nameof(WebGLVoiceChatClient)),
                JsonUtility.ToJson(new WebGLVoiceChatConfig
                {
                    initialMute = voiceChatConfig.InitialMute,
                    isDebug = Logger.IsDebug()
                }));

        public override void ToggleMute()
        {
            var muted = WebGLHelper.CallFunction(WithPrefix(nameof(ToggleMute)));
            FireOnMuted(bool.Parse(muted));
        }

        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLVoiceChatClient)}#{name}";
    }

    [SuppressMessage("Usage", "IDE1006")]
    public class WebGLVoiceChatConfig
    {
        public bool initialMute;
        public bool isDebug;
    }
}
