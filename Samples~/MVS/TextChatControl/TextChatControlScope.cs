using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.Controls.TextChatControl
{
    public class TextChatControlScope : LifetimeScope
    {
        [SerializeField] private TextChatControlView textChatControlView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(textChatControlView);

            builder.RegisterEntryPoint<TextChatControlPresenter>();
        }
    }
}
