namespace Extreal.Integration.Chat.WebRTC
{
    public enum AudioLevelChangeState
    {
        New,
        Change,
        Leave,
    }

    public class AudioLevelChange
    {
        public string Id { get; }
        public AudioLevelChangeState State { get; }
        public float Value { get; }

        public AudioLevelChange(string id, AudioLevelChangeState audioLevelChangeState, float value)
        {
            Id = id;
            State = audioLevelChangeState;
            Value = value;
        }
    }
}
