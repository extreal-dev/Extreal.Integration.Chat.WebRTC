using System;
using Extreal.Core.Common.System;
using Extreal.Integration.P2P.WebRTC;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC.MVS.App
{
    public class AppState : DisposableBase
    {
        public IObservable<string> OnNotificationReceived => onNotificationReceived.AddTo(disposables);
        private readonly Subject<string> onNotificationReceived = new Subject<string>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private PeerRole role = PeerRole.Host;

        public bool IsHost => role == PeerRole.Host;
        public void SetRole(PeerRole role) => this.role = role;

        public string GroupName { get; private set; }
        public void SetGroupName(string groupName) => GroupName = groupName;

        public string GroupId { get; private set; }
        public void SetGroupId(string groupId) => GroupId = groupId;

        public void Notify(string message) => onNotificationReceived.OnNext(message);

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
