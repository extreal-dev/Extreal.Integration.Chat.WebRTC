using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that provides voice chat client.
    /// </summary>
    public class VoiceChatClientProvider
    {
        /// <summary>
        /// Provides voice chat client.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        /// <param name="config">Voice chat config.</param>
        /// <returns>Voice chat client.</returns>
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
