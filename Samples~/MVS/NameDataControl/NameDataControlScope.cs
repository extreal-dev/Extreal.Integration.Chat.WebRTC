using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.WebRTC.MVS.NameDataControl
{
    public class NameDataControlScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
            => builder.RegisterEntryPoint<NameDataControlPresenter>();
    }
}
