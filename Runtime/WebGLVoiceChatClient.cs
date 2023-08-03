using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;

namespace Extreal.Integration.Chat.WebRTC
{
    public class WebGLVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLVoiceChatClient));

        public WebGLVoiceChatClient(VoiceChatConfig voiceChatConfig)
            => WebGLHelper.CallAction(WithPrefix(nameof(WebGLVoiceChatClient)),
                JsonSerializer.Serialize(new WebGLVoiceChatConfig
                {
                    InitialMute = voiceChatConfig.InitialMute,
                    IsDebug = Logger.IsDebug()
                }));

        public override void ToggleMute()
        {
            var muted = WebGLHelper.CallFunction(WithPrefix(nameof(ToggleMute)));
            FireOnMuted(bool.Parse(muted));
        }

        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLVoiceChatClient)}#{name}";
    }

    [SuppressMessage("Usage", "CC0047")]
    public class WebGLVoiceChatConfig
    {
        [JsonPropertyName("initialMute")]
        public bool InitialMute { get; set; }

        [JsonPropertyName("isDebug")]
        public bool IsDebug { get; set; }
    }
}
