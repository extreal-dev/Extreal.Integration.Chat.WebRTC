using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that provides text chat client.
    /// </summary>
    public class TextChatClientProvider
    {
        /// <summary>
        /// Provides text chat client.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        /// <returns>Text chat client.</returns>
        [SuppressMessage("Style", "CC0038"), SuppressMessage("Style", "CC0057"), SuppressMessage("Style", "IDE0022")]
        public static TextChatClient Provide(PeerClient peerClient)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeTextChatClient(peerClient as NativePeerClient);
#else
            return new WebGLTextChatClient();
#endif
        }
    }
}
