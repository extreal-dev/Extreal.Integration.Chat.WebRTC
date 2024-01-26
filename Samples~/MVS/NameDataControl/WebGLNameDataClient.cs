#if UNITY_WEBGL

using System;
using AOT;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public class WebGLNameDataClient : NameDataClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLNameDataClient));

        private static WebGLNameDataClient instance;

        public WebGLNameDataClient()
        {
            instance = this;
            WebGLHelper.CallAction(WithPrefix(nameof(WebGLNameDataClient)));
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnOpen)), HandleOnOpen);
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnClose)), HandleOnClose);
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnMessageReceived)), HandleOnMessageReceived);
        }

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnOpen(string id, string unused) => instance.FireOnOpen(id);

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnClose(string id, string unused) => instance.FireOnClose(id);

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnMessageReceived(string id, string message)
            => instance.FireOnMessageReceived(id, message);

        protected override void DoSend(string to, string message) => WebGLHelper.CallAction(WithPrefix(nameof(DoSend)), to, message);

        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLNameDataClient)}#{name}";
    }
}
#endif
