#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using UniRx;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Android;
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
            AudioStreamTrack inTrack, MediaStream outStream)> resources;

        private readonly Transform voiceChatContainer;

        private AudioClip mic;

        private bool mute;
        private float inVolume;
        private float outVolume;
        private float[] samples = new float[2048];
        private bool isMicrophoneInitialized;
        private bool isMicrophonePermissionCheckRequired;
        private bool isMicrophonePermissionChecked;

        private readonly Dictionary<string, float> audioLevels = new Dictionary<string, float>();

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
                AudioStreamTrack inTrack, MediaStream outStream)>();
            this.peerClient = peerClient;
            this.voiceChatConfig = voiceChatConfig;
            mute = voiceChatConfig.InitialMute;
            inVolume = voiceChatConfig.InitialInVolume;
            outVolume = voiceChatConfig.InitialOutVolume;
            isMicrophonePermissionCheckRequired = voiceChatConfig.IsMicrophonePermissionCheckRequired;
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);

            peerClient.OnStarted
                .Subscribe(id => ownId = id)
                .AddTo(disposables);

            peerClient.OnDisconnected
                .Subscribe(_ => ownId = null)
                .AddTo(disposables);

            Observable.Interval(TimeSpan.FromSeconds(voiceChatConfig.AudioLevelCheckIntervalSeconds))
                .Subscribe(_ => HandleAudioLevelChange())
                .AddTo(disposables);
        }

        private async UniTask InitializeMicrophoneAsync()
        {
            isMicrophoneInitialized = true;
            if (await HasMicrophoneAsync())
            {
                mic = Microphone.Start(null, true, 1, 48000);
                await UniTask.WaitUntil(() => Microphone.GetPosition(null) > 0);

                foreach (var (inOutAudio, _, _, _) in resources.Values)
                {
                    inOutAudio.InAudio.clip = mic;
                    inOutAudio.InAudio.Play();
                }
            }
            if (Logger.IsDebug())
            {
                if (mic != null)
                {
                    Logger.LogDebug("Microphone found");
                }
                else if (!HasMicrophonePermission())
                {
                    Logger.LogDebug("Microphone permission denied");
                }
                else
                {
                    Logger.LogDebug("Microphone not found");
                }
            }
        }

#pragma warning disable CS1998
        private async UniTask RequestMicrophonePermissionAsync()
        {
#if UNITY_IOS
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                await Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#endif

#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var callbacks = new PermissionCallbacks();
                var requestCompleted = false;
                callbacks.PermissionGranted += _ => requestCompleted = true;
                callbacks.PermissionDenied += _ => requestCompleted = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => requestCompleted = true;

                Permission.RequestUserPermission(Permission.Microphone, callbacks);

                await UniTask.WaitUntil(() => requestCompleted);
            }
#endif
        }
#pragma warning restore CS1998

        [SuppressMessage("Usage", "IDE0022")]
        private bool HasMicrophonePermission()
        {
#if UNITY_IOS
            return Application.HasUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (resources.ContainsKey(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }

            if (!isMicrophoneInitialized)
            {
                InitializeMicrophoneAsync().Forget();
            }

            var inOutAudio = GetInOutAudio();

            MediaStream inStream = null;
            AudioStreamTrack inTrack = null;

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

            var inAudioGo = new GameObject("InAudio");
            var inAudio = inAudioGo.AddComponent<AudioSource>();
            inAudioGo.transform.SetParent(inOutAudioGo.transform);

            inAudio.loop = true;
            inAudio.mute = mute;
            if (mic != null)
            {
                inAudio.clip = mic;
                inAudio.Play();
            }

            var outAudioGo = new GameObject("OutAudio");
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

            if (resources.Count == 0)
            {
                StopMicrophone();
            }
        }

        /// <inheritdoc/>
        public override async UniTask<bool> HasMicrophoneAsync()
        {
            if (isMicrophonePermissionCheckRequired && !isMicrophonePermissionChecked)
            {
                await RequestMicrophonePermissionAsync();
                isMicrophonePermissionChecked = true;
            }
            return HasMicrophonePermission() && Microphone.devices.Length > 0;
        }

        /// <inheritdoc/>
        public override void ToggleMute()
        {
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

        private void HandleAudioLevelChange()
        {
            if (string.IsNullOrEmpty(ownId))
            {
                return;
            }

            HandleInAudioLevelChange();
            HandleOutAudioLevelChange();
        }

        private void HandleInAudioLevelChange()
        {
            var inAudio = resources.Values.Select(resource => resource.inOutAudio.InAudio).FirstOrDefault();
            if (inAudio != null)
            {
                var audioLevel = mute ? 0f : GetAudioLevel(inAudio);
                if (!audioLevels.ContainsKey(ownId) || audioLevels[ownId] != audioLevel)
                {
                    audioLevels[ownId] = audioLevel;
                    FireOnAudioLevelChanged(ownId, audioLevel);
                }
            }
        }

        private void HandleOutAudioLevelChange()
        {
            foreach (var id in resources.Keys)
            {
                var outAudio = resources[id].inOutAudio.OutAudio;
                var audioLevel = GetAudioLevel(outAudio);
                if (!audioLevels.ContainsKey(id) || audioLevels[id] != audioLevel)
                {
                    audioLevels[id] = audioLevel;
                    FireOnAudioLevelChanged(id, audioLevel);
                }
            }
        }

        private void StopMicrophone()
        {
            if (mic != null)
            {
                Microphone.End(null);
                mic = null;
            }
            isMicrophoneInitialized = false;
        }

        /// <inheritdoc/>
        protected override void ReleaseManagedResources()
        {
            StopMicrophone();
            Object.Destroy(voiceChatContainer);
            disposables.Dispose();
            base.ReleaseManagedResources();
        }
    }
}
#endif
