using Extreal.Core.StageNavigation;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC.MVS.App
{
    [CreateAssetMenu(
        menuName = "Chat.WebRTC.MVS/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : StageConfigBase<StageName, SceneName>
    {
    }
}
