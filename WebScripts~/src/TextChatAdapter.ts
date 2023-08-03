import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { addAction, callback } from "@extreal-dev/extreal.integration.web.common";
import { TextChatClient } from "./TextChatClient";

class TextChatAdapter {
    private textChatClient: TextChatClient | undefined;

    public adapt = (getPeerClient: PeerClientProvider) => {
        addAction(this.withPrefix("WebGLTextChatClient"), (jsonConfig) => {
            this.textChatClient = new TextChatClient(JSON.parse(jsonConfig), getPeerClient, {
                onDataReceived: (message) => callback(this.withPrefix("HandleOnDataReceived"), message),
            });
        });

        addAction(this.withPrefix("DoSend"), (message) => this.getTextChatClient().send(message));

        addAction(this.withPrefix("Clear"), () => this.getTextChatClient().clear());
    };

    private withPrefix = (name: string) => `WebGLTextChatClient#${name}`;

    private getTextChatClient = () => {
        if (!this.textChatClient) {
            throw new Error("Call the WebGLTextChatClient constructor first in Unity.");
        }
        return this.textChatClient;
    };
}

export { TextChatAdapter };
