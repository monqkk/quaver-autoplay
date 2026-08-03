using monk.API;
using SimpleIniConfig;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace MonkAutoplay
{
    internal static class Autoplayer
    {
        private static readonly InputSimulator Keyboard = new InputSimulator();
        private static int audioOffsetMs;
        private static int keyCount;
        private static Replay replay;

        public static void Run(Replay replayFrames, bool flipInputs)
        {
            replay = replayFrames;
            keyCount = replay.Mode == GameMode.Keys4 ? 4 : 7;

            var screen = Memory.Quaver.GameplayScreen;
            var clock = screen.GameplayAudioTiming;

            var index = FindFrameAt(clock.Time);
            var lastTime = clock.Time + audioOffsetMs;

            while (screen.IsLoaded && index < replay.Frames.Count)
            {
                var now = clock.Time + audioOffsetMs;

                if (Math.Abs(now - lastTime) >= 50)
                    break;

                lastTime = now;

                if (now >= replay.Frames[index].Time)
                {
                    ApplyFrame(replay.Frames[index].Keys, flipInputs);
                    index++;
                }

                SleepMs(1);
            }

            ReleaseAllKeys(flipInputs);
        }

        public static void ReloadSettings()
        {
            const string configFile = "config.ini";

            if (!File.Exists(configFile))
                File.WriteAllText(configFile, "AudioOffset = 0\n");

            audioOffsetMs = new SimpleIniConfig.Config().GetValue("AudioOffset", 0);
        }

        private static void ApplyFrame(ReplayKeyPressState keys, bool flipInputs)
        {
            var held = Replay.KeyPressStateToLanes(keys);

            for (var lane = 0; lane < keyCount; lane++)
            {
                var mapped = flipInputs ? keyCount - 1 - lane : lane;
                var key = KeyForLane(mapped);

                if (held.Contains(lane))
                    Keyboard.Keyboard.KeyDown(key);
                else
                    Keyboard.Keyboard.KeyUp(key);
            }
        }

        private static void ReleaseAllKeys(bool flipInputs)
        {
            for (var lane = 0; lane < keyCount; lane++)
            {
                var mapped = flipInputs ? keyCount - 1 - lane : lane;
                Keyboard.Keyboard.KeyUp(KeyForLane(mapped));
            }
        }

        private static VirtualKeyCode KeyForLane(int laneIndex)
        {
            var name = replay.Mode == GameMode.Keys4
                ? $"KeyMania4K{laneIndex + 1}"
                : $"KeyMania7K{laneIndex + 1}";

            if (Enum.TryParse(typeof(KeyBind), name, out var key))
                return Memory.Config.GetBindedKey((KeyBind)key);

            return VirtualKeyCode.SPACE;
        }

        private static int FindFrameAt(double timeMs)
        {
            for (var i = replay.Frames.Count - 1; i >= 0; i--)
            {
                if (replay.Frames[i].Time <= timeMs)
                    return i;
            }

            return 0;
        }

        private static void SleepMs(uint ms)
        {
            var wait = new AutoResetEvent(false);
            TimerCallback wake = (_, __, ___, ____, _____) => wait.Set();
            var timer = timeSetEvent(ms, 0, wake, UIntPtr.Zero, 0);
            wait.WaitOne();
            timeKillEvent(timer);
        }

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeSetEvent(uint delay, uint resolution, TimerCallback callback, UIntPtr user, uint flags);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeKillEvent(uint timerId);

        private delegate void TimerCallback(uint timerId, uint msg, UIntPtr user, UIntPtr arg1, UIntPtr arg2);
    }
}
