using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using AOT;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;

namespace Extreal.Integration.Chat.WebRTC
{
    public class WebGLTextChatClient : TextChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLTextChatClient));
        private static readonly WebGLTextChatConfig Config = new WebGLTextChatConfig()
        {
            IsDebug = Logger.IsDebug()
        };
        private static readonly string JsonConfig = JsonSerializer.Serialize(Config);

        private static WebGLTextChatClient instance;

        public WebGLTextChatClient()
        {
            instance = this;
            WebGLHelper.CallAction(WithPrefix(nameof(WebGLTextChatClient)), JsonConfig);
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnDataReceived)), HandleOnDataReceived);
        }

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnDataReceived(string message, string unused)
            => instance.FireOnMessageReceived(message);

        protected override void DoSend(string message) => WebGLHelper.CallAction(WithPrefix(nameof(DoSend)), message);

        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLTextChatClient)}#{name}";
    }

    [SuppressMessage("Usage", "CC0047")]
    public class WebGLTextChatConfig
    {
        [JsonPropertyName("isDebug")]
        public bool IsDebug { get; set; }
    }
}
