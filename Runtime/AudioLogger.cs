using System.Linq;
using Extreal.Core.Logging;
using UnityEngine;

namespace Extreal.Integration.Chat.WebRTC
{
    public class AudioSourceLogger : MonoBehaviour
    {
        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(AudioSourceLogger));

        private const int SampleRate = 48000;
        private const float MovingAveTime = 0.05f;
        private const int MovingAveSample = (int)(SampleRate * MovingAveTime);

        private AudioSource audioSource;

        private void Start() => audioSource = GetComponent<AudioSource>();

        private void Update()
        {
            if (Logger.IsDebug())
            {
                LogAudioLevel();
            }
        }

        private void LogAudioLevel()
        {
            var samples = new float[MovingAveSample];
            audioSource.GetOutputData(samples, 0);
            var audioLevel = samples.Average(Mathf.Abs);
            if (audioLevel != 0)
            {
                Logger.LogDebug($"*** Audio Level: {audioLevel}");
            }
        }
    }
}
