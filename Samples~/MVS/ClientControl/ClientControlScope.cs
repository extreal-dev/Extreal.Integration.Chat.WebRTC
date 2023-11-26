using System;
using System.Collections.Generic;
using Extreal.Integration.P2P.WebRTC;
using SocketIOClient;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.ClientControl
{
    public class ClientControlScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var peerConfig = new PeerConfig(
                "http://127.0.0.1:3010",
                new SocketIOOptions
                {
                    ConnectionTimeout = TimeSpan.FromSeconds(3),
                    Reconnection = false
                },
                new List<IceServerConfig>
                {
                    new IceServerConfig(new List<string>
                    {
                        "stun:stun.l.google.com:19302",
                        "stun:stun1.l.google.com:19302",
                        "stun:stun2.l.google.com:19302",
                        "stun:stun3.l.google.com:19302",
                        "stun:stun4.l.google.com:19302"
                    }, "test-name", "test-credential")
                });

            var peerClient = PeerClientProvider.Provide(peerConfig);
            builder.RegisterComponent(peerClient);

            var textChatClient = TextChatClientProvider.Provide(peerClient);
            builder.RegisterComponent(textChatClient);

            var voiceChatConfig = new VoiceChatConfig();
            var voiceChatClient = VoiceChatClientProvider.Provide(peerClient, voiceChatConfig);
            builder.RegisterComponent(voiceChatConfig);
            builder.RegisterComponent(voiceChatClient);

            builder.RegisterEntryPoint<ClientControlPresenter>();
        }
    }
}
