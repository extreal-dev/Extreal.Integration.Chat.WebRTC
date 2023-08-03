#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Unity.WebRTC;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    public class NativeVoiceChatClient : VoiceChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeVoiceChatClient));

        private readonly Dictionary<string, (
            NativeInOutAudio inOutAudio, MediaStream inStream,
            AudioStreamTrack inTrack, MediaStream outStream)> resources;

        private readonly VoiceChatConfig voiceChatConfig;

        private readonly Transform voiceChatContainer;

        private readonly AudioClip mic;

        public NativeVoiceChatClient(
            NativePeerClient peerClient, VoiceChatConfig voiceChatConfig)
        {
            voiceChatContainer = new GameObject("VoiceChatContainer").transform;
            Object.DontDestroyOnLoad(voiceChatContainer);

            resources = new Dictionary<string, (
                NativeInOutAudio inOutAudio, MediaStream inStream,
                AudioStreamTrack inTrack, MediaStream outStream)>();
            this.voiceChatConfig = voiceChatConfig;
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);

            mic = Microphone.Start(null, true, 1, 48000);
            while (!(Microphone.GetPosition(null) > 0))
            {
                // do nothing
            }
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (resources.ContainsKey(id))
            {
                return;
            }

            var inOutAudio = GetInOutAudio();

            var inTrack = new AudioStreamTrack(inOutAudio.InAudio);
            inTrack.Loopback = false;
            var inStream = new MediaStream();
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
            inOutAudioGo.transform.SetParent(voiceChatContainer);
            var inOutAudio = inOutAudioGo.AddComponent<NativeInOutAudio>();

            var inAudioGo = new GameObject("InAudio");
            var inAudio = inAudioGo.AddComponent<AudioSource>();
            inAudioGo.transform.SetParent(inOutAudioGo.transform);

            var outAudioGo = new GameObject("OutAudio", typeof(AudioSourceLogger));
            var outAudio = outAudioGo.AddComponent<AudioSource>();
            outAudioGo.transform.SetParent(inOutAudioGo.transform);

            inOutAudio.Initialize(inAudio, outAudio);

            inAudio.loop = true;
            inAudio.clip = mic;
            inAudio.Play();
            inAudio.mute = voiceChatConfig.InitialMute;

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
            resource.inOutAudio.InAudio.Stop();
            resource.inOutAudio.OutAudio.Stop();
            resource.inStream.GetTracks().ToList().ForEach((track) => track.Stop());
            resource.inStream.Dispose();
            resource.inTrack.Dispose();
            resource.outStream.GetTracks().ToList().ForEach((track) => track.Stop());
            resource.outStream.Dispose();
            Object.Destroy(resource.inOutAudio.gameObject);
            resources.Remove(id);
        }

        public override void ToggleMute()
        {
            resources.Values.ToList().ForEach(resource =>
            {
                var inAudio = resource.inOutAudio.InAudio;
                inAudio.mute = !inAudio.mute;
            });
            FireOnMuted(resources.First().Value.inOutAudio.InAudio.mute);
        }

        public override void Clear()
        {
            resources.Keys.ToList().ForEach(ClosePc);
            resources.Clear();
        }

        protected override void ReleaseManagedResources()
        {
            Microphone.End(null);
            Object.Destroy(voiceChatContainer);
            base.ReleaseManagedResources();
        }
    }
}
#endif
