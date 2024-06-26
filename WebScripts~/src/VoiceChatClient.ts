import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type VoiceChatConfig = {
    initialMute: boolean;
    initialInVolume: number;
    initialOutVolume: number;
    audioLevelCheckIntervalSeconds: number;
    isDebug: boolean;
};

type VoiceChatClientCallBacks = {
    onAudioLevelChanged: (id: string, audioLevel: number) => void;
};

class Resource {
    inStream: MediaStream | undefined;
    inTrack: MediaStreamTrack | undefined;
    inTransceiver: RTCRtpTransceiver | undefined;
    outAudio: HTMLAudioElement | undefined;
    outStream: MediaStream | undefined;
    inGainNode: GainNode | undefined;
    outGainNode: GainNode | undefined;
    inAnalyzerNode: AnalyserNode | undefined;
    outAnalyzerNode: AnalyserNode | undefined;
}

class VoiceChatClient {
    private readonly isDebug: boolean;
    private readonly voiceChatConfig: VoiceChatConfig;
    private readonly getPeerClient: PeerClientProvider;
    private readonly hasMicrophone: boolean;

    private resources: Map<string, Resource>;
    private mute: boolean;
    private inVolume: number;
    private outVolume: number;

    private audioContext: AudioContext | undefined;

    private audioLevels: Map<string, number>;

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
        this.resources = new Map();
        this.getPeerClient().addPcCreateHook(this.createPc);
        this.getPeerClient().addPcCloseHook(this.closePc);
        if (this.isDebug) {
            console.log(hasMicrophone ? "Microphone found" : "Microphone not found");
        }
        this.audioLevels = new Map<string, number>();

        const resumeAudioContext = () => {
            if (!this.audioContext)
            {
                this.audioContext = new AudioContext();
            }
            this.audioContext.resume();
        }

        this.executeOnceOnSpecifiedEvents("unity-canvas", ["touchstart", "mousedown", "keydown"], resumeAudioContext);

        setInterval(this.handleAudioLevelChange, voiceChatConfig.audioLevelCheckIntervalSeconds * 1000);
    }

    private executeOnceOnSpecifiedEvents = (targetElementId: string, eventNames: string[], action: () => void) => {
        const executeActionAndRemove = () => {
            action();
            eventNames.forEach(eventName => {
                document.getElementById(targetElementId)?.removeEventListener(eventName, executeActionAndRemove);
            });
        };

        eventNames.forEach(eventName => {
            document.getElementById(targetElementId)?.addEventListener(eventName, executeActionAndRemove);
        });
    };

    private createPc = async (id: string, isOffer: boolean, pc: RTCPeerConnection) => {
        if (this.resources.has(id)) {
            return;
        }

        if (this.isDebug) {
            console.log(`New MediaStream: id=${id}`);
        }

        const client = this;
        if (!client.audioContext)
        {
            client.audioContext = new AudioContext();
        }

        const resource: Resource = new Resource();
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

            const inTrack = inStream.getAudioTracks()[0];
            pc.addTrack(inTrack, inStream);
            inTrack.enabled = !this.mute;

            inGainNode.gain.value = client.inVolume;

            resource.inStream = inStream;
            resource.inTrack = inTrack;
            resource.inGainNode = inGainNode;
            resource.inAnalyzerNode = inAnalyzerNode;
        }
        else {
            resource.inTransceiver = pc.addTransceiver("audio", { direction: "recvonly" });
        }

        const outAudio = new Audio();
        resource.outAudio = outAudio;

        pc.addEventListener("track", (event) => {
            if (!client.audioContext)
            {
                client.audioContext = new AudioContext();
            }
            const outStream = event.streams[0];
            const sourceNode = client.audioContext.createMediaStreamSource(outStream);
            const outGainNode = client.audioContext.createGain();
            const outAnalyzerNode = client.audioContext.createAnalyser();
            
            sourceNode.connect(outGainNode);
            outGainNode.connect(outAnalyzerNode);
            outAnalyzerNode.connect(client.audioContext.destination);
            
            outAudio.srcObject = outStream;

            resource.outGainNode = outGainNode;
            resource.outAnalyzerNode = outAnalyzerNode;
        });

        this.resources.set(id, resource);
    };

    private closePc = (id: string) => {
        const resource = this.resources.get(id);
        if (!resource) {
            return;
        }
        if (resource.inStream) {
            resource.inStream.getTracks().forEach((track) => track.stop());
        }
        if (resource.inTransceiver) {
            resource.inTransceiver.stop();
        }
        if (resource.outAudio) {
            resource.outAudio.pause();
            resource.outAudio.remove();
        }
        if (resource.outStream) {
            resource.outStream.getTracks().forEach((track) => track.stop());
        }
        this.resources.delete(id);
    };

    public clear = () => {
        [...this.resources.keys()].forEach(this.closePc);
        this.resources.clear();
        this.mute = this.voiceChatConfig.initialMute;
        this.inVolume = this.voiceChatConfig.initialInVolume;
        this.inVolume = this.voiceChatConfig.initialOutVolume;
    };

    public toggleMute = () => {
        this.mute = !this.mute;
        this.resources.forEach((resource) => {
            if (resource.inTrack) {
                resource.inTrack.enabled = !this.mute;
            }
        });
        return this.mute;
    };

    public setInVolume = (volume: number) => {
        this.inVolume = this.clamp(volume, 0, 1);
        this.resources.forEach(resource => {
            if (!this.audioContext)
            {
                this.audioContext = new AudioContext();
            }
            if (resource.inGainNode) {
                resource.inGainNode.gain.setValueAtTime(this.inVolume, this.audioContext.currentTime);
            }
        })
    }

    public setOutVolume = (volume: number) => {
        this.outVolume = this.clamp(volume, 0, 1);
        this.resources.forEach(resource => {
            if (!this.audioContext)
            {
                this.audioContext = new AudioContext();
            }
            if (resource.outGainNode) {
                resource.outGainNode.gain.setValueAtTime(this.outVolume, this.audioContext.currentTime);
            }
        })
    }

    public handleAudioLevelChange = () => {
        const localId = this.getPeerClient().getClientId();
        if (!localId) {
            return
        }

        this.handleInAudioLevelChange(localId);
        this.handleOutAudioLevelChange();
    }

    private handleInAudioLevelChange = (localId: string) => {
        const resource = [...this.resources.values()].find(resource => resource.inAnalyzerNode);
        if (resource && resource.inAnalyzerNode) {
            const audioLevel = this.mute ? 0 : this.getAudioLevel(resource.inAnalyzerNode);
            if (!this.audioLevels.has(localId) || this.audioLevels.get(localId) != audioLevel) {
                this.audioLevels.set(localId, audioLevel);
                this.callBacks.onAudioLevelChanged(localId, audioLevel);
            }
        }
    }

    private handleOutAudioLevelChange = () => {
        this.resources.forEach((resource, id) => {
            if (resource.outAnalyzerNode) {
                const audioLevel = this.getAudioLevel(resource.outAnalyzerNode);
                if (!this.audioLevels.has(id) || this.audioLevels.get(id) != audioLevel) {
                    this.audioLevels.set(id, audioLevel);
                    this.callBacks.onAudioLevelChanged(id, audioLevel);
                }
            }
        });
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
