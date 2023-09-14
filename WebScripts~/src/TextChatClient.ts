import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";

type TextChatConfig = {
    isDebug: boolean;
};

type TextChatCallbacks = {
    onDataReceived: (message: string) => void;
};

class TextChatClient {
    private readonly label: string = "textchat";
    private readonly isDebug: boolean;
    private readonly dcMap: Map<string, RTCDataChannel>;
    private readonly getPeerClient: PeerClientProvider;
    private readonly callbacks: TextChatCallbacks;

    constructor(textChatConfig: TextChatConfig, getPeerClient: PeerClientProvider, callbacks: TextChatCallbacks) {
        this.isDebug = textChatConfig.isDebug;
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
            this.callbacks.onDataReceived(event.data);
        });
    };

    private closePc = (id: string) => {
        const dc = this.dcMap.get(id);
        if (!dc) {
            return;
        }
        dc.close();
        this.dcMap.delete(id);
    };

    public send = (message: string) => [...this.dcMap.values()].forEach((dc) => dc.send(message));

    public clear = () => {
        [...this.dcMap.keys()].forEach(this.closePc);
        this.dcMap.clear();
    };
}

export { TextChatClient };
