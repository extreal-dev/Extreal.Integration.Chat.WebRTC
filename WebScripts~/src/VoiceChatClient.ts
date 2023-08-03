import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type VoiceChatConfig = {
    initialMute: boolean;
    isDebug: boolean;
};

class VoiceChatClient {
    private readonly isDebug: boolean;
    private readonly initialMute: boolean;
    private readonly getPeerClient: PeerClientProvider;

    private inStream: MediaStream | null;
    private inTrack: MediaStreamTrack | null;
    private outAudios: Map<string, HTMLAudioElement>;
    private outStreams: Map<string, MediaStream>;

    constructor(voiceChatConfig: VoiceChatConfig, getPeerClient: PeerClientProvider) {
        this.isDebug = voiceChatConfig.isDebug;
        this.initialMute = voiceChatConfig.initialMute;
        this.getPeerClient = getPeerClient;
        this.inStream = null;
        this.inTrack = null;
        this.outAudios = new Map();
        this.outStreams = new Map();
        this.getPeerClient().addPcCreateHook(this.createPc);
        this.getPeerClient().addPcCloseHook(this.closePc);
    }

    private createPc = async (id: string, isOffer: boolean, pc: RTCPeerConnection) => {
        if (this.outAudios.has(id)) {
            return;
        }

        if (this.isDebug) {
            console.log(`New MediaStream: id=${id}`);
        }

        const client = this;

        const inStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        client.inStream = inStream;
        const inTrack = inStream.getAudioTracks()[0];
        client.inTrack = inTrack;

        pc.addTrack(inTrack, inStream);
        inTrack.enabled = !this.initialMute;

        const outAudio = new Audio();
        client.outAudios.set(id, outAudio);

        pc.addEventListener("track", async (event) => {
            const outStream = event.streams[0];
            outAudio.srcObject = outStream;
            outAudio.loop = true;
            await outAudio.play();
            client.outStreams.set(id, outStream);
        });
    };

    private closePc = (id: string) => {
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
    };

    public clear = () => {
        if (this.inStream !== null) {
            this.inStream.getTracks().forEach((track) => track.stop());
            this.inStream = null;
        }
        this.inTrack = null;
        [...this.outAudios.keys()].forEach(this.closePc);
        this.outAudios.clear();
        this.outStreams.clear();
    };

    public toggleMute = () => {
        const track = this.inTrack;
        if (!track) {
            return true;
        }
        track.enabled = !track.enabled;
        return !track.enabled;
    };
}

export { VoiceChatClient };
