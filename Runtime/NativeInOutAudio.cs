using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that holds in/out audio sources.
    /// </summary>
    public class NativeInOutAudio : MonoBehaviour
    {
        /// <summary>
        /// Audio source for input.
        /// </summary>
        /// <value>Audio source for input.</value>
        public AudioSource InAudio { get; private set; }

        /// <summary>
        /// Audio source for output.
        /// </summary>
        /// <value>Audio source for output.</value>
        public AudioSource OutAudio { get; private set; }

        /// <summary>
        /// Initializes NativeInOutAudio.
        /// </summary>
        /// <param name="inAudio">Audio source for input.</param>
        /// <param name="outAudio">Audio source for output.</param>
        public void Initialize(AudioSource inAudio, AudioSource outAudio)
        {
            InAudio = inAudio;
            OutAudio = outAudio;
        }
    }
}
