﻿using Extreal.Integration.Chat.WebRTC.MVS.App;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.ClientControl
{
    public class ClientControlPresenter : IInitializable
    {
        private readonly AppState appState;
        private readonly VoiceChatClient voiceChatClient;

        public ClientControlPresenter(AppState appState, VoiceChatClient voiceChatClient)
        {
            this.appState = appState;
            this.voiceChatClient = voiceChatClient;
        }

        public void Initialize()
        {
            if (!voiceChatClient.HasMicrophone())
            {
                appState.Notify("Microphone not found");
            }
        }
    }
}
