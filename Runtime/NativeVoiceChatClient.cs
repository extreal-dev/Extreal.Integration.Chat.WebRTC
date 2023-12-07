#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Unity.WebRTC;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that handles voice chat client for native application.
    /// </summary>
    public class NativeVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeVoiceChatClient));

        private readonly VoiceChatConfig voiceChatConfig;
        private readonly Dictionary<string, (
            NativeInOutAudio inOutAudio, MediaStream inStream,
            AudioStreamTrack inTrack, MediaStream outStream)> resources;

        private readonly Transform voiceChatContainer;

        private readonly AudioClip mic;

        private bool mute;
        private float micVolume;
        private float speakersVolume;
        private float[] samples = new float[2048];
        private readonly Dictionary<string, float> remoteAudioLevelList = new Dictionary<string, float>();

        /// <summary>
        /// Creates NativeVoiceChatClient with peerClient and voiceChatConfig.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        /// <param name="voiceChatConfig">Voice chat config.</param>
        public NativeVoiceChatClient(
            NativePeerClient peerClient, VoiceChatConfig voiceChatConfig)
        {
            voiceChatContainer = new GameObject("VoiceChatContainer").transform;
            Object.DontDestroyOnLoad(voiceChatContainer);

            resources = new Dictionary<string, (
                NativeInOutAudio inOutAudio, MediaStream inStream,
                AudioStreamTrack inTrack, MediaStream outStream)>();
            this.voiceChatConfig = voiceChatConfig;
            mute = voiceChatConfig.InitialMute;
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);

            if (Microphone.devices.Length > 0)
            {
                mic = Microphone.Start(null, true, 1, 48000);
            }
            if (Logger.IsDebug())
            {
                Logger.LogDebug(HasMicrophone() ? "Microphone found" : "Microphone not found");
            }
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (resources.ContainsKey(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }

            var inOutAudio = GetInOutAudio();

            inOutAudio.InAudio.volume = micVolume;

            MediaStream inStream = null;
            AudioStreamTrack inTrack = null;
            if (HasMicrophone())
            {
                inTrack = new AudioStreamTrack(inOutAudio.InAudio)
                {
                    Loopback = false
                };
                inStream = new MediaStream();
                pc.AddTrack(inTrack, inStream);
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"AddTrack(IN): id={id}");
                }
            }

            var outStream = new MediaStream();
            outStream.OnAddTrack += evt =>
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"OnAddTrack(OUT): kind={evt.Track.Kind} id={id}");
                }
                if (evt.Track is AudioStreamTrack outTrack)
                {
                    inOutAudio.OutAudio.SetTrack(outTrack);
                }
            };
            pc.OnTrack += evt =>
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"OnTrack(OUT): kind={evt.Track.Kind} id={id}");
                }
                if (evt.Track.Kind == TrackKind.Audio)
                {
                    outStream.AddTrack(evt.Track);
                }
            };

            resources.Add(id, (inOutAudio, inStream, inTrack, outStream));
        }

        private NativeInOutAudio GetInOutAudio()
        {
            var inOutAudioGo = new GameObject("InOutAudio");
            var inOutAudio = inOutAudioGo.AddComponent<NativeInOutAudio>();
            inOutAudioGo.transform.SetParent(voiceChatContainer);

            AudioSource inAudio = null;
            if (HasMicrophone())
            {
                var inAudioGo = new GameObject("InAudio");
                inAudio = inAudioGo.AddComponent<AudioSource>();
                inAudioGo.transform.SetParent(inOutAudioGo.transform);

                inAudio.loop = true;
                inAudio.clip = mic;
                inAudio.Play();
                inAudio.mute = mute;
            }

            var outAudioGo = new GameObject("OutAudio", typeof(AudioSourceLogger));
            var outAudio = outAudioGo.AddComponent<AudioSource>();
            outAudioGo.transform.SetParent(inOutAudioGo.transform);

            inOutAudio.Initialize(inAudio, outAudio);

            outAudio.loop = true;
            outAudio.Play();

            return inOutAudio;
        }

        private void ClosePc(string id)
        {
            if (!resources.TryGetValue(id, out var resource))
            {
                return;
            }

            if (resource.inOutAudio.InAudio != null)
            {
                resource.inOutAudio.InAudio.Stop();
            }
            if (resource.inOutAudio.OutAudio != null)
            {
                resource.inOutAudio.OutAudio.Stop();
            }
            if (resource.inOutAudio.gameObject != null)
            {
                Object.Destroy(resource.inOutAudio.gameObject);
            }

            if (resource.inStream != null)
            {
                resource.inStream.GetTracks().ToList().ForEach((track) => track.Stop());
                resource.inStream.Dispose();
            }

            if (resource.inTrack != null)
            {
                resource.inTrack.Dispose();
            }

            if (resource.outStream != null)
            {
                resource.outStream.GetTracks().ToList().ForEach((track) => track.Stop());
                resource.outStream.Dispose();
            }

            resources.Remove(id);
        }

        /// <inheritdoc/>
        public override bool HasMicrophone() => mic != null;

        /// <inheritdoc/>
        public override void ToggleMute()
        {
            if (!HasMicrophone())
            {
                return;
            }

            mute = !mute;
            resources.Values.ToList().ForEach(resource =>
            {
                var inAudio = resource.inOutAudio.InAudio;
                inAudio.mute = mute;
            });
            FireOnMuted(mute);
        }

        /// <inheritdoc/>
        public override void SetInVolume(float volume)
        {
            micVolume = volume;
            resources.Values.ToList().ForEach(resource =>
            {
                var inAudio = resource.inOutAudio.InAudio;
                inAudio.volume = micVolume;
            });
        }

        /// <inheritdoc/>
        public override void SetOutVolume(float volume)
        {
            speakersVolume = volume;
            resources.Values.ToList().ForEach(resource =>
            {
                var outAudio = resource.inOutAudio.OutAudio;
                outAudio.volume = speakersVolume;
            });
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            resources.Keys.ToList().ForEach(ClosePc);
            resources.Clear();
            mute = voiceChatConfig.InitialMute;
        }

        /// <inheritdoc/>
        public override float LocalAudioLevel
        {
            get
            {
                AudioSource inAudio;
                if (resources.Count == 0 || mute || (inAudio = resources.Values.First().inOutAudio.InAudio) == null)
                {
                    return -0f;
                }
                var audioLevel = GetAudioLevel(inAudio);
                return audioLevel;
            }
        }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, float> RemoteAudioLevelList
        {
            get
            {
                remoteAudioLevelList.Clear();
                foreach (var id in resources.Keys)
                {
                    var outAudio = resources[id].inOutAudio.OutAudio;
                    var audioLevel = GetAudioLevel(outAudio);
                    remoteAudioLevelList[id] = audioLevel;
                }
                return remoteAudioLevelList;
            }
        }

        private float GetAudioLevelDB(AudioSource audioSource)
        {
            audioSource.GetOutputData(samples, 0);
            var audioLevel = samples.Average(Mathf.Abs);
            var dB = Mathf.Clamp(20 * Mathf.Log10(audioLevel), -80f, 0f);
            return dB;
        }

        private float GetAudioLevel(AudioSource audioSource)
        {
            audioSource.GetOutputData(samples, 0);
            var audioLevel = samples.Average(Mathf.Abs);
            return audioLevel;
        }

        /// <inheritdoc/>
        protected override void ReleaseManagedResources()
        {
            Microphone.End(null);
            Object.Destroy(voiceChatContainer);
            base.ReleaseManagedResources();
        }
    }
}
#endif
