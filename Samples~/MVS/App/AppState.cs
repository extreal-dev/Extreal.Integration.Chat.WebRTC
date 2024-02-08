using System;
using System.Diagnostics.CodeAnalysis;
using Extreal.Core.Common.System;
using Extreal.Integration.P2P.WebRTC;
using UniRx;

namespace Extreal.Integration.Chat.WebRTC.MVS.App
{
    public class AppState : DisposableBase
    {
        public IObservable<string> OnNotificationReceived => onNotificationReceived.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly Subject<string> onNotificationReceived = new Subject<string>();

        [SuppressMessage("Usage", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private PeerRole role = PeerRole.Host;

        public bool IsHost => role == PeerRole.Host;
        public void SetRole(PeerRole role) => this.role = role;

        public string GroupName { get; private set; }
        public void SetGroupName(string groupName) => GroupName = groupName;

        public string GroupId { get; private set; }
        public void SetGroupId(string groupId) => GroupId = groupId;

        public void Notify(string message) => onNotificationReceived.OnNext(message);

        public string Name { get; private set; }
        public void SetName(string name) => Name = name;

        public IReadOnlyReactiveDictionary<string, string> NameDict => nameDict.AddTo(disposables);
        [SuppressMessage("Usage", "CC0033")]
        private readonly ReactiveDictionary<string, string> nameDict = new ReactiveDictionary<string, string>();
        public void SetNameDict(string id, string name) => nameDict[id] = name;
        public void RemoveNameDict(string id) => nameDict.Remove(id);
        public void ClearNameDict() => nameDict.Clear();


        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
