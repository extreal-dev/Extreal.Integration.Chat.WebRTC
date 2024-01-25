using System.Diagnostics.CodeAnalysis;
using Extreal.Integration.P2P.WebRTC;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public class NameDataClientProvider
    {
        [SuppressMessage("Style", "CC0038"), SuppressMessage("Style", "IDE0022")]
        public static NameDataClient Provide(PeerClient peerClient)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            return new NativeNameDataClient(peerClient as NativePeerClient);
#else
            return new WebGLNameDataClient();
#endif
        }
    }
}
