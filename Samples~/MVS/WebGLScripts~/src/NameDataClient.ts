import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type NameDataCallbacks = {
    onOpen: (id: string) => void;
    onClose: (id: string) => void;
    onMessageReceived: (id: string, message: string) => void;
}

class NameDataClient {
    private readonly label: string = "namedata";
    private readonly isDebug: boolean;
    private readonly dcMap: Map<string, RTCDataChannel>;
    private readonly getPeerClient: PeerClientProvider;
    private readonly callbacks: NameDataCallbacks;

    constructor(getPeerClient: PeerClientProvider, callbacks: NameDataCallbacks) {
        this.isDebug = true;
        this.dcMap = new Map();
        this.getPeerClient = getPeerClient;
        this.callbacks = callbacks;
        this.getPeerClient().addPcCreateHook(this.createPc);
        this.getPeerClient().addPcCloseHook(this.closePc);
    }

    private createPc = (id: string, isOffer: boolean, pc: RTCPeerConnection) => {
        if (this.dcMap.has(id)) {
            return;
        }

        if (isOffer) {
            const dc = pc.createDataChannel(this.label);
            this.handleDc(id, dc);
        } else {
            pc.addEventListener("datachannel", (event) => this.handleDc(id, event.channel));
        }
    };

    private handleDc = (id: string, dc: RTCDataChannel) => {
        if (dc.label !== this.label) {
            return;
        }

        if (this.isDebug) {
            console.log(`New DataChannel: id=${id} label=${dc.label}`);
        }

        this.dcMap.set(id, dc);
        dc.addEventListener("message", (event) => {
            this.callbacks.onMessageReceived(id, event.data);
        });

        dc.onopen = () => this.callbacks.onOpen(id);
        dc.onclose = () => this.callbacks.onClose(id);

        if (dc.readyState === "open") {
            this.callbacks.onOpen(id);
        }
    };

    private closePc = (id: string) => {
        const dc = this.dcMap.get(id);
        if (!dc) {
            return;
        }
        dc.close();
        this.dcMap.delete(id);
    };

    public send = (to: string, message: string) => {
        const dc = this.dcMap.get(to);
        if (dc) {
            if (this.isDebug) {
                console.log(`Send message: to=${to}, message=${message}`);
            }
            dc.send(message);
        }
    }

    public clear = () => {
        [...this.dcMap.keys()].forEach(this.closePc);
        this.dcMap.clear();
    };
}

export { NameDataClient };
