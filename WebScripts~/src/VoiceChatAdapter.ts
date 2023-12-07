import { PeerClientProvider } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { addAction, addFunction } from "@extreal-dev/extreal.integration.web.common";
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
            this.voiceChatClient = new VoiceChatClient(JSON.parse(jsonConfig), getPeerClient, hasMicrophone);
        });

        addFunction(this.withPrefix("HasMicrophone"), () => hasMicrophone.toString());

        addFunction(this.withPrefix("ToggleMute"), () => this.getVoiceChatClient().toggleMute().toString());

        addAction(this.withPrefix("SetMicVolume"), (volume: string) => this.getVoiceChatClient().setMicVolume(Number(volume)));

        addAction(this.withPrefix("SetSpeakersVolume"), (volume: string) => this.getVoiceChatClient().setSpeakersVolume(Number(volume)));

        addFunction(this.withPrefix("LocalAudioLevel"), () => this.getVoiceChatClient().getLocalAudioLevel().toString());

        addFunction(this.withPrefix("RemoteAudioLevelList"), () => {
            const remoteAudioLevelList = this.getVoiceChatClient().getRemoteAudioLevelList();
            return JSON.stringify(Object.fromEntries(remoteAudioLevelList));
        });

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
