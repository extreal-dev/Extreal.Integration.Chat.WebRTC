import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { addAction, callback } from "@extreal-dev/extreal.integration.web.common";
import { NameDataClient } from "./NameDataClient";

class NameDataAdapter {
    private nameDataClient: NameDataClient | undefined;

    public adapt = (getPeerClient: PeerClientProvider) => {
        addAction(this.withPrefix("WebGLNameDataClient"), () => {
            this.nameDataClient = new NameDataClient(getPeerClient, {
                onOpen: id => callback(this.withPrefix("HandleOnOpen"), id),
                onClose: id => callback(this.withPrefix("HandleOnClose"), id),
                onMessageReceived: (id, message) => callback(this.withPrefix("HandleOnMessageReceived"), id, message),
            })
        });

        addAction(this.withPrefix("DoSend"), (id, message) => this.getNameDataClient().send(id, message));

        addAction(this.withPrefix("Clear"), () => this.getNameDataClient().clear());
    }
    
    private withPrefix = (name: string) => `WebGLNameDataClient#${name}`;

    private getNameDataClient = () => {
        if (!this.nameDataClient) {
            throw new Error("Call the WebGLNameDataClient constructor first in Unity.");
        }
        return this.nameDataClient;
    }
}

export { NameDataAdapter };
