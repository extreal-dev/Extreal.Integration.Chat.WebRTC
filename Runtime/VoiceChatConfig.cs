using System.Diagnostics.CodeAnalysis;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that holds the config for voice chat.
    /// </summary>
    public class VoiceChatConfig
    {
        /// <summary>
        /// Initial status of mute.
        /// </summary>
        /// <value>True if initial muted, false otherwise.</value>
        public bool InitialMute { get; }

        /// <summary>
        /// Creates VoiceChatConfig with initialMute.
        /// </summary>
        /// <param name="initialMute">True if initial muted, false otherwise.</param>
        [SuppressMessage("Style", "CC0057")]
        public VoiceChatConfig(bool initialMute = true) => InitialMute = initialMute;
    }
}
