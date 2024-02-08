#if !UNITY_WEBGL || UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extreal.Core.Logging;
using Extreal.Integration.P2P.WebRTC;
using Unity.WebRTC;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public class NativeNameDataClient : NameDataClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(NativeNameDataClient));

        private static readonly string Label = "namedata";

        private readonly Dictionary<string, RTCDataChannel> dcDict;

        public NativeNameDataClient(NativePeerClient peerClient)
        {
            dcDict = new Dictionary<string, RTCDataChannel>();
            peerClient.AddPcCreateHook(CreatePc);
            peerClient.AddPcCloseHook(ClosePc);
        }

        private void CreatePc(string id, bool isOffer, RTCPeerConnection pc)
        {
            if (dcDict.ContainsKey(id))
            {
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
                return;
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"New DataChannel: id={id} label={dc.Label}");
            }

            dcDict.Add(id, dc);
            dc.OnMessage = message =>
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Receive message: from={id}, message={Encoding.UTF8.GetString(message)}");
                }
                FireOnMessageReceived(id, Encoding.UTF8.GetString(message));
            };

            dc.OnOpen = () => FireOnOpen(id);
            dc.OnClose = () => FireOnClose(id);

            if (dc.ReadyState == RTCDataChannelState.Open)
            {
                FireOnOpen(id);
            }
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

        protected override void DoSend(string to, string message)
        {
            if (dcDict.TryGetValue(to, out var dc))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Send message: to={to}, message={message}");
                }
                dc.Send(message);
            }
        }

        public override void Clear()
        {
            dcDict.Keys.ToList().ForEach(ClosePc);
            dcDict.Clear();
        }
    }
}
#endif
