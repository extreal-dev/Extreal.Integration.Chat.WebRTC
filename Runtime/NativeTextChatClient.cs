#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Unity.WebRTC;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that handles text chat client for native application.
    /// </summary>
    public class NativeTextChatClient : TextChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeTextChatClient));

        private static readonly string Label = "textchat";

        private readonly Dictionary<string, RTCDataChannel> dcDict;

        /// <summary>
        /// Creates NativeTextChatClient with peerClient.
        /// </summary>
        /// <param name="peerClient">Peer client.</param>
        public NativeTextChatClient(NativePeerClient peerClient)
        {
            dcDict = new Dictionary<string, RTCDataChannel>();
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (dcDict.ContainsKey(id))
            {
                // Not covered by testing due to defensive implementation
                return;
            }

            if (isOffer)
            {
                var dc = pc.CreateDataChannel(Label);
                HandleDc(id, dc);
            }
            else
            {
                pc.OnDataChannel += (dc) => HandleDc(id, dc);
            }
        }

        private void HandleDc(string id, RTCDataChannel dc)
        {
            if (dc.Label != Label)
            {
                // Not covered by testing but passed by peer review
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"New DataChannel: id={id} label={dc.Label}");
            }

            dcDict.Add(id, dc);
            dc.OnMessage = message => FireOnMessageReceived(Encoding.UTF8.GetString(message));
        }

        private void ClosePc(string id)
        {
            if (!dcDict.TryGetValue(id, out var dc))
            {
                return;
            }
            dc.Close();
            dcDict.Remove(id);
        }

        /// <inheritdoc/>
        protected override void DoSend(string message)
            => dcDict.Values.ToList().ForEach(dc => dc.Send(message));

        /// <inheritdoc/>
        public override void Clear()
        {
            dcDict.Keys.ToList().ForEach(ClosePc);
            dcDict.Clear();
        }
    }
}
#endif
