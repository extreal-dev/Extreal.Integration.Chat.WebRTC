using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Chat.WebRTC
{
    public class VoiceChatClientProvider
    {
        [SuppressMessage("Style", "CC0038"), SuppressMessage("Style", "CC0057"), SuppressMessage("Style", "IDE0060")]
        public static VoiceChatClient Provide(
            PeerClient peerClient, VoiceChatConfig config = null)
        {
            config ??= new VoiceChatConfig();
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeVoiceChatClient(peerClient as NativePeerClient, config);
#else
            return new WebGLVoiceChatClient(config);
#endif
        }
    }
}
