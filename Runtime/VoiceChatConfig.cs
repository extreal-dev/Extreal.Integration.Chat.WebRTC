using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Chat.WebRTC
{
    public class VoiceChatConfig
    {
        public bool InitialMute { get; private set; }

        [SuppressMessage("Style", "CC0057")]
        public VoiceChatConfig(bool initialMute = true) => InitialMute = initialMute;
    }
}
