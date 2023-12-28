import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { addAction, addFunction, callback } from "@extreal-dev/extreal.integration.web.common";
import { VoiceChatClient } from "./VoiceChatClient";

let hasMicrophone = false;
(async () => {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        stream.getTracks().forEach((track) => track.stop());
        hasMicrophone = true;
    } catch (e) {
        hasMicrophone = false;
    }
})();

class VoiceChatAdapter {
    private voiceChatClient: VoiceChatClient | undefined;

    public adapt = (getPeerClient: PeerClientProvider) => {
        addAction(this.withPrefix("WebGLVoiceChatClient"), (jsonConfig) => {
            this.voiceChatClient = new VoiceChatClient(JSON.parse(jsonConfig), getPeerClient, hasMicrophone, {
                onAudioLevelChanged: (audioLevels) => callback(this.withPrefix("HandleOnAudioLevelChanged"), JSON.stringify(Object.fromEntries(audioLevels))),
            });
        });

        addFunction(this.withPrefix("HasMicrophone"), () => hasMicrophone.toString());

        addFunction(this.withPrefix("ToggleMute"), () => this.getVoiceChatClient().toggleMute().toString());

        addAction(this.withPrefix("SetInVolume"), (volume: string) => this.getVoiceChatClient().setInVolume(Number(volume)));

        addAction(this.withPrefix("SetOutVolume"), (volume: string) => this.getVoiceChatClient().setOutVolume(Number(volume)));

        addAction(this.withPrefix("Clear"), () => this.getVoiceChatClient().clear());
    };

    private withPrefix = (name: string) => `WebGLVoiceChatClient#${name}`;

    private getVoiceChatClient = () => {
        if (!this.voiceChatClient) {
            throw new Error("Call the WebGLVoiceChatClient constructor first in Unity.");
        }
        return this.voiceChatClient;
    };
}

export { VoiceChatAdapter };
