using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.P2PControl
{
    public class P2PControlScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
            => builder.RegisterEntryPoint<P2PControlPresenter>();
    }
}
