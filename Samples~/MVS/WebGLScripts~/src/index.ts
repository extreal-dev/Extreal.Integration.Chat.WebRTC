import { PeerAdapter } from "@extreal-dev/extreal.integration.p2p.webrtc";
import { TextChatAdapter, VoiceChatAdapter } from "@extreal-dev/extreal.integration.chat.webrtc";

const peerAdapter = new PeerAdapter();
peerAdapter.adapt();

const textChatAdapter = new TextChatAdapter();
textChatAdapter.adapt(peerAdapter.getPeerClient);

const voiceChatAdapter = new VoiceChatAdapter();
voiceChatAdapter.adapt(peerAdapter.getPeerClient);
