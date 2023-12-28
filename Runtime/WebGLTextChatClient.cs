#if UNITY_WEBGL
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AOT;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;

namespace Extreal.Integration.Chat.WebRTC
{
    /// <summary>
    /// Class that handles text chat client for WebGL application.
    /// </summary>
    public class WebGLTextChatClient : TextChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLTextChatClient));
        private static readonly WebGLTextChatConfig Config = new WebGLTextChatConfig
        {
            isDebug = Logger.IsDebug()
        };
        private static readonly string JsonConfig = JsonSerializer.Serialize(Config);

        private static WebGLTextChatClient instance;

        /// <summary>
        /// Creates WebGLTextChatClient
        /// </summary>
        public WebGLTextChatClient()
        {
            instance = this;
            WebGLHelper.CallAction(WithPrefix(nameof(WebGLTextChatClient)), JsonConfig);
            WebGLHelper.AddCallback(WithPrefix(nameof(HandleOnDataReceived)), HandleOnDataReceived);
        }

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        private static void HandleOnDataReceived(string message, string unused)
            => instance.FireOnMessageReceived(message);

        /// <inheritdoc/>
        protected override void DoSend(string message) => WebGLHelper.CallAction(WithPrefix(nameof(DoSend)), message);

        /// <inheritdoc/>
        public override void Clear() => WebGLHelper.CallAction(WithPrefix(nameof(Clear)));

        private static string WithPrefix(string name) => $"{nameof(WebGLTextChatClient)}#{name}";
    }

    [SuppressMessage("Usage", "IDE1006"), SuppressMessage("Usage", "CC0047")]
    public class WebGLTextChatConfig
    {
        public bool isDebug { get; set; }
    }
}
#endif
