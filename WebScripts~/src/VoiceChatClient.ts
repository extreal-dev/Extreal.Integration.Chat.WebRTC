import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type VoiceChatConfig = {
    initialMute: boolean;
    initialInVolume: number;
    initialOutVolume: number;
    isDebug: boolean;
};

type VoiceChatClientCallBacks = {
    onAudioLevelChanged: (audioLevels: Map<string, number>) => void;
};

class VoiceChatClient {
    private readonly isDebug: boolean;
    private readonly voiceChatConfig: VoiceChatConfig;
    private readonly getPeerClient: PeerClientProvider;
    private readonly hasMicrophone: boolean;

    private peerConnectionIds: Set<string>;
    private inStreams: Map<string, MediaStream>;
    private inTracks: Map<string, MediaStreamTrack>;
    private outAudios: Map<string, HTMLAudioElement>;
    private outStreams: Map<string, MediaStream>;

    private inGainNodes: Map<string, GainNode>;
    private outGainNodes: Map<string, GainNode>;

    private inAnalyzerNodes: Map<string, AnalyserNode>;
    private outAnalyzerNodes: Map<string, AnalyserNode>;

    private mute: boolean;
    private inVolume: number;
    private outVolume: number;

    private audioContext: AudioContext | undefined;

    private audioLevelList: Map<string, number>;
    private previousAudioLevelList: Map<string, number>;

    private readonly callBacks: VoiceChatClientCallBacks;

    constructor(voiceChatConfig: VoiceChatConfig, getPeerClient: PeerClientProvider, hasMicrophone: boolean, callBacks: VoiceChatClientCallBacks) {
        this.isDebug = voiceChatConfig.isDebug;
        this.voiceChatConfig = voiceChatConfig;
        this.callBacks = callBacks;
        this.mute = voiceChatConfig.initialMute;
        this.inVolume = voiceChatConfig.initialInVolume;
        this.outVolume = voiceChatConfig.initialOutVolume;
        this.getPeerClient = getPeerClient;
        this.hasMicrophone = hasMicrophone;
        this.peerConnectionIds = new Set();
        this.inStreams = new Map();
        this.inTracks = new Map();
        this.outAudios = new Map();
        this.outStreams = new Map();
        this.getPeerClient().addPcCreateHook(this.createPc);
        this.getPeerClient().addPcCloseHook(this.closePc);
        if (this.isDebug) {
            console.log(hasMicrophone ? "Microphone found" : "Microphone not found");
        }
        this.inGainNodes = new Map<string, GainNode>();
        this.outGainNodes = new Map<string, GainNode>();
        this.inAnalyzerNodes = new Map<string, AnalyserNode>();
        this.outAnalyzerNodes = new Map<string, AnalyserNode>();
        this.audioLevelList = new Map<string, number>();
        this.previousAudioLevelList = new Map<string, number>();

        const audioContextResumeFunc = () => {
            if (this.audioContext === undefined)
            {
                this.audioContext = new AudioContext();
            }
            this.audioContext.resume();
            document.getElementById("unity-canvas")?.removeEventListener("touchstart", audioContextResumeFunc);
            document.getElementById("unity-canvas")?.removeEventListener("mousedown", audioContextResumeFunc);
            document.getElementById("unity-canvas")?.removeEventListener("keydown", audioContextResumeFunc);
        };
        document.getElementById("unity-canvas")?.addEventListener("touchstart", audioContextResumeFunc);
        document.getElementById("unity-canvas")?.addEventListener("mousedown", audioContextResumeFunc);
        document.getElementById("unity-canvas")?.addEventListener("keydown", audioContextResumeFunc);
    }

    private createPc = async (id: string, isOffer: boolean, pc: RTCPeerConnection) => {
        if (this.peerConnectionIds.has(id)) {
            return;
        }

        if (this.isDebug) {
            console.log(`New MediaStream: id=${id}`);
        }

        this.peerConnectionIds.add(id);

        const client = this;
        if (!client.audioContext)
        {
            client.audioContext = new AudioContext();
        }

        if (this.hasMicrophone) {
            const micStream = await navigator.mediaDevices.getUserMedia({ audio: true });

            const sourceNode = client.audioContext.createMediaStreamSource(micStream);
            const inGainNode = client.audioContext.createGain();
            const inAnalyzerNode = client.audioContext.createAnalyser();
            const destinationNode = client.audioContext.createMediaStreamDestination();
            const inStream = destinationNode.stream;
            sourceNode.connect(inGainNode);
            inGainNode.connect(inAnalyzerNode);
            inAnalyzerNode.connect(destinationNode);

            client.inStreams.set(id, inStream);
            const inTrack = inStream.getAudioTracks()[0];
            client.inTracks.set(id, inTrack);
            pc.addTrack(inTrack, inStream);
            inTrack.enabled = !this.mute;

            inGainNode.gain.value = client.inVolume;
            client.inGainNodes.set(id, inGainNode);
            client.inAnalyzerNodes.set(id, inAnalyzerNode);
        }

        const outAudio = new Audio();
        client.outAudios.set(id, outAudio);

        pc.addEventListener("track", (event) => {
            if (!client.audioContext)
            {
                client.audioContext = new AudioContext();
            }
            const outStream = event.streams[0];
            const sourceNode = client.audioContext.createMediaStreamSource(outStream);
            const outGainNode = client.audioContext.createGain();
            const outAnalyzerNode = client.audioContext.createAnalyser();

            client.outGainNodes.set(id, outGainNode);
            client.outAnalyzerNodes.set(id, outAnalyzerNode);

            sourceNode.connect(outGainNode);
            outGainNode.connect(outAnalyzerNode);
            outAnalyzerNode.connect(client.audioContext.destination);

            outAudio.srcObject = outStream;
        });
    };

    private closePc = (id: string) => {
        const inStream = this.inStreams.get(id);
        if (inStream) {
            inStream.getTracks().forEach((track) => track.stop());
            this.inStreams.delete(id);
        }
        this.inTracks.delete(id);
        const outAudio = this.outAudios.get(id);
        if (outAudio) {
            outAudio.pause();
            this.outAudios.delete(id);
        }
        const outStream = this.outStreams.get(id);
        if (outStream) {
            outStream.getTracks().forEach((track) => track.stop());
            this.outStreams.delete(id);
        }

        const inGainNode = this.inGainNodes.get(id);
        if (inGainNode) {
            this.inGainNodes.delete(id);
        }

        const outGainNode = this.outGainNodes.get(id);
        if (outGainNode) {
            this.outGainNodes.delete(id);
        }

        const inAnalyzerNode = this.inAnalyzerNodes.get(id);
        if (inAnalyzerNode) {
            this.inAnalyzerNodes.delete(id);
        }

        const outAnalyzerNode = this.outAnalyzerNodes.get(id);
        if (outAnalyzerNode) {
            this.outAnalyzerNodes.delete(id);
        }

        if (this.peerConnectionIds.has(id)) {
            this.peerConnectionIds.delete(id);
        }
    };

    public clear = () => {
        [...this.outAudios.keys()].forEach(this.closePc);
        this.inStreams.clear();
        this.inTracks.clear();
        this.outAudios.clear();
        this.outStreams.clear();
        this.inGainNodes.clear();
        this.outGainNodes.clear();
        this.inAnalyzerNodes.clear();
        this.outAnalyzerNodes.clear();
        this.mute = this.voiceChatConfig.initialMute;
    };

    public toggleMute = () => {
        this.mute = !this.mute;
        [...this.inTracks.values()].forEach((track) => {
            track.enabled = !this.mute;
        });
        return this.mute;
    };

    public setInVolume = (volume: number) => {
        this.inVolume = this.clamp(volume, 0, 1);
        this.inGainNodes.forEach(gainNode => {
            if (!this.audioContext)
            {
                this.audioContext = new AudioContext();
            }
            gainNode.gain.setValueAtTime(this.inVolume, this.audioContext.currentTime);
        })
    }

    public setOutVolume = (volume: number) => {
        this.outVolume = this.clamp(volume, 0, 1);
        this.outGainNodes.forEach(gainNode => {
            if (!this.audioContext)
            {
                this.audioContext = new AudioContext();
            }
            gainNode.gain.setValueAtTime(this.outVolume, this.audioContext.currentTime);
        })
    }

    public getLocalAudioLevel = () => {
        if (this.inAnalyzerNodes.size == 0 || this.mute) {
            return 0;
        }
        const inAnalyzerNode = this.inAnalyzerNodes.values().next().value;
        const audioLevel = this.getAudioLevel(inAnalyzerNode);
        return audioLevel;
    }

    public getRemoteAudioLevelList = () => {
        const remoteAudioLevelList = new Map<string, number>();
        this.outAnalyzerNodes.forEach((outAnalyzerNode, id) => {
            const audioLevel = this.getAudioLevel(outAnalyzerNode);
            remoteAudioLevelList.set(id, audioLevel);
        });
        return remoteAudioLevelList;
    }

    public handleAudioLevels = () => {
        const localId = this.getPeerClient().getSocketId();
        if (localId) {
            this.previousAudioLevelList.clear();
            this.audioLevelList.forEach((level, id) => {
                this.previousAudioLevelList.set(id, level);
            });
            this.audioLevelList.clear();

            if (this.inAnalyzerNodes.size > 0) {
                let audioLevel;
                if (this.mute) {
                    audioLevel = 0;
                }
                else {
                    const inAnalyzerNode = this.inAnalyzerNodes.values().next().value;
                    audioLevel = this.getAudioLevel(inAnalyzerNode);
                }
                this.audioLevelList.set(localId, audioLevel);

                this.outAnalyzerNodes.forEach((outAnalyzerNode, id) => {
                    audioLevel = this.getAudioLevel(outAnalyzerNode);
                    this.audioLevelList.set(id, audioLevel);
                });
            }

            this.previousAudioLevelList.forEach((level, id) => {
                if (!this.audioLevelList.has(id) || this.audioLevelList.get(id) != level) {
                    this.callBacks.onAudioLevelChanged(this.audioLevelList);
                    return;
                }
            });
            this.audioLevelList.forEach((_, id) => {
                if (!this.previousAudioLevelList.has(id)) {
                    this.callBacks.onAudioLevelChanged(this.audioLevelList);
                    return;
                }
            });
        }
    }

    private getAudioLevel = (analyserNode: AnalyserNode) => {
        const samples = new Float32Array(analyserNode.fftSize);
        analyserNode.getFloatTimeDomainData(samples);
        const audioLevel = this.absAverage(samples);
        return audioLevel;
    }

    private absAverage = (values: Float32Array) => {
        const total = values.reduce((sum, current) => sum += Math.abs(current));
        return total / values.length;
    }

    private clamp = (value: number, min: number, max: number) => Math.max(Math.min(value, max), min);
}

export { VoiceChatClient };
