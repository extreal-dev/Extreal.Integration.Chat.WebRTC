using System;
using System.Diagnostics.CodeAnalysis;
using AOT;
using Extreal.Core.Logging;
using Extreal.Integration.Web.Common;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    public class WebGLTextChatClient : TextChatClient
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(WebGLTextChatClient));
        private static readonly WebGLTextChatConfig Config = new WebGLTextChatConfig
        {
            isDebug = Logger.IsDebug()
        };
        private static readonly string JsonConfig = JsonUtility.ToJson(Config);

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

    [SuppressMessage("Usage", "IDE1006")]
    public class WebGLTextChatConfig
    {
        public bool isDebug;
    }
}
