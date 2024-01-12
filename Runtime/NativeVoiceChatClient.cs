#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using Unity.WebRTC;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that handles voice chat client for native application.
    /// </summary>
    public class NativeVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeVoiceChatClient));

        private readonly PeerClient peerClient;
        private readonly VoiceChatConfig voiceChatConfig;
        private readonly Dictionary<string, (
            NativeInOutAudio inOutAudio, MediaStream inStream,
            AudioStreamTrack inTrack, RTCRtpTransceiver inTransceiver, MediaStream outStream)> resources;

        private readonly Transform voiceChatContainer;

        private readonly AudioClip mic;

        private bool mute;
        private float inVolume;
        private float outVolume;
        private readonly float audioLevelCheckIntervalSeconds;
        private float[] samples = new float[2048];

        private readonly Dictionary<string, float> audioLevelList = new Dictionary<string, float>();
        private readonly Dictionary<string, float> previousAudioLevelList = new Dictionary<string, float>();

        private string ownId;

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

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
                AudioStreamTrack inTrack, RTCRtpTransceiver inTransceiver, MediaStream outStream)>();
            this.peerClient = peerClient;
            this.voiceChatConfig = voiceChatConfig;
            mute = voiceChatConfig.InitialMute;
            inVolume = voiceChatConfig.InitialInVolume;
            outVolume = voiceChatConfig.InitialOutVolume;
            audioLevelCheckIntervalSeconds = voiceChatConfig.AudioLevelCheckIntervalSeconds;
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);

            if (Microphone.devices.Length > 0)
            {
                mic = Microphone.Start(null, true, 1, 48000);
                while (Microphone.GetPosition(null) > 0)
                {
                    // do nothing
                }
            }
            if (Logger.IsDebug())
            {
                Logger.LogDebug(HasMicrophone() ? "Microphone found" : "Microphone not found");
            }

            peerClient.OnStarted
                .Subscribe(id => ownId = id)
                .AddTo(disposables);

            peerClient.OnDisconnected
                .Subscribe(_ => ownId = null)
                .AddTo(disposables);

            Observable.Interval(TimeSpan.FromSeconds(audioLevelCheckIntervalSeconds))
                .Subscribe(_ => AudioLevelChangeHandler())
                .AddTo(disposables);
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (resources.ContainsKey(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }

            var inOutAudio = GetInOutAudio();

            MediaStream inStream = null;
            AudioStreamTrack inTrack = null;
            RTCRtpTransceiver inTransceiver = null;
            if (HasMicrophone())
            {
                inOutAudio.InAudio.volume = inVolume;
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
            else
            {
                inTransceiver = pc.AddTransceiver(TrackKind.Audio, new RTCRtpTransceiverInit { direction = RTCRtpTransceiverDirection.RecvOnly });
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

            resources.Add(id, (inOutAudio, inStream, inTrack, inTransceiver, outStream));
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

            if (resource.inTransceiver != null)
            {
                resource.inTransceiver.Stop();
                resource.inTransceiver.Dispose();
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
                if (inAudio != null)
                {
                    inAudio.mute = mute;
                }
            });
            FireOnMuted(mute);
        }

        /// <inheritdoc/>
        public override void SetInVolume(float volume)
        {
            inVolume = Mathf.Clamp(volume, 0f, 1f);
            resources.Values.ToList().ForEach(resource =>
            {
                var inAudio = resource.inOutAudio.InAudio;
                inAudio.volume = inVolume;
            });
        }

        /// <inheritdoc/>
        public override void SetOutVolume(float volume)
        {
            outVolume = Mathf.Clamp(volume, 0f, 1f);
            resources.Values.ToList().ForEach(resource =>
            {
                var outAudio = resource.inOutAudio.OutAudio;
                outAudio.volume = outVolume;
            });
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            resources.Keys.ToList().ForEach(ClosePc);
            resources.Clear();
            mute = voiceChatConfig.InitialMute;
            inVolume = voiceChatConfig.InitialInVolume;
            outVolume = voiceChatConfig.InitialOutVolume;
        }

        private float GetAudioLevel(AudioSource audioSource)
        {
            audioSource.GetOutputData(samples, 0);
            var audioLevel = samples.Average(Mathf.Abs);
            return audioLevel;
        }

        private void AudioLevelChangeHandler()
        {
            if (string.IsNullOrEmpty(ownId))
            {
                return;
            }

            previousAudioLevelList.Clear();
            foreach (var id in audioLevelList.Keys)
            {
                previousAudioLevelList[id] = audioLevelList[id];
            }
            audioLevelList.Clear();

            var inAudio = resources.Values.Select(resource => resource.inOutAudio.InAudio).FirstOrDefault();
            if (inAudio != null)
            {
                var inAudioLevel = mute ? 0f : GetAudioLevel(inAudio);
                audioLevelList[ownId] = inAudioLevel;
            }
            foreach (var id in resources.Keys)
            {
                var outAudio = resources[id].inOutAudio.OutAudio;
                var outAudioLevel = GetAudioLevel(outAudio);
                audioLevelList[id] = outAudioLevel;
            }

            foreach (var id in previousAudioLevelList.Keys)
            {
                if (!audioLevelList.ContainsKey(id) || audioLevelList[id] != previousAudioLevelList[id])
                {
                    FireOnAudioLevelChanged(audioLevelList);
                    return;
                }
            }
            foreach (var id in audioLevelList.Keys)
            {
                if (!previousAudioLevelList.ContainsKey(id))
                {
                    FireOnAudioLevelChanged(audioLevelList);
                    return;
                }
            }
        }

        /// <inheritdoc/>
        protected override void ReleaseManagedResources()
        {
            Microphone.End(null);
            Object.Destroy(voiceChatContainer);
            disposables.Dispose();
            base.ReleaseManagedResources();
        }
    }
}
#endif
