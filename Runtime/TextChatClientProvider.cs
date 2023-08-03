using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Chat.WebRTC
{
    public class TextChatClientProvider
    {
        [SuppressMessage("Style", "CC0038")]
        public static TextChatClient Provide(PeerClient peerClient)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeTextChatClient(peerClient as NativePeerClient);
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WebGLTextChatClient();
#endif
        }
    }
}
