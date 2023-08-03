namespace Extreal.Integration.Chat.WebRTC
{
    public class VoiceChatConfig
    {
        public bool InitialMute { get; private set; }

        public VoiceChatConfig(bool initialMute = true) => InitialMute = initialMute;
    }
}
