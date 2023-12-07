import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type VoiceChatConfig = {
    initialMute: boolean;
    isDebug: boolean;
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

    private audioContext: AudioContext;

    constructor(voiceChatConfig: VoiceChatConfig, getPeerClient: PeerClientProvider, hasMicrophone: boolean) {
        this.isDebug = voiceChatConfig.isDebug;
        this.voiceChatConfig = voiceChatConfig;
        this.mute = voiceChatConfig.initialMute;
        this.inVolume = 1.0;
        this.outVolume = 1.0;
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
        this.audioContext = new AudioContext();
        this.inGainNodes = new Map<string, GainNode>();
        this.outGainNodes = new Map<string, GainNode>();
        this.inAnalyzerNodes = new Map<string, AnalyserNode>();
        this.outAnalyzerNodes = new Map<string, AnalyserNode>();
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

        pc.addEventListener("track", async (event) => {
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
        if (inGainNode)
        {
            this.inGainNodes.delete(id);
        }

        const outGainNode = this.outGainNodes.get(id);
        if (outGainNode)
        {
            this.outGainNodes.delete(id);
        }

        const inAnalyzerNode = this.inAnalyzerNodes.get(id);
        if (inAnalyzerNode)
        {
            this.inAnalyzerNodes.delete(id);
        }

        const outAnalyzerNode = this.outAnalyzerNodes.get(id);
        if (outAnalyzerNode)
        {
            this.outAnalyzerNodes.delete(id);
        }

        if (this.peerConnectionIds.has(id))
        {
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
        this.inVolume = volume;
        this.inGainNodes.forEach(gainNode => {
            gainNode.gain.setValueAtTime(this.inVolume, this.audioContext.currentTime);
        })
    }

    public setOutVolume = (volume: number) => {
        this.outVolume = volume;
        this.outGainNodes.forEach(gainNode => {
            gainNode.gain.setValueAtTime(this.outVolume, this.audioContext.currentTime);
        })
    }

    public getLocalAudioLevel = () => {
        if (this.inAnalyzerNodes.size == 0 || this.mute)
        {
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

    private getAudioLevel = (analyserNode: AnalyserNode) => {
        const samples = new Float32Array(analyserNode.fftSize);
        analyserNode.getFloatTimeDomainData(samples);
        const audioLevel = this.absAverage(samples);
        return audioLevel;
    }

    private absAverage = (values: Float32Array) =>
    {
        const total = values.reduce((sum, current) => sum += Math.abs(current));
        return total / values.length;
    }
}

export { VoiceChatClient };
