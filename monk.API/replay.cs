using monk.API.SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace monk.API
{
    [Flags]
    public enum ReplayKeyPressState
    {
        K1 = 1 << 0,
        K2 = 1 << 1,
        K3 = 1 << 2,
        K4 = 1 << 3,
        K5 = 1 << 4,
        K6 = 1 << 5,
        K7 = 1 << 6,
        K8 = 1 << 7,
        K9 = 1 << 8
    }

    public class ReplayFrame
    {
        public int Time { get; }
        public ReplayKeyPressState Keys { get; }

        public ReplayFrame(int time, ReplayKeyPressState keys)
        {
            Time = time;
            Keys = keys;
        }
    }

    public enum ReplayAutoplayFrameType
    {
        Press,
        Release
    }

    public struct ReplayAutoplayFrame
    {
        public ReplayAutoplayFrameType Type { get; }
        public int Time { get; }
        public ReplayKeyPressState Keys { get; }
        public HitObject HitObject { get; }

        public ReplayAutoplayFrame(HitObject hitObject, ReplayAutoplayFrameType type, int time, ReplayKeyPressState keys)
        {
            HitObject = hitObject;
            Type = type;
            Time = time;
            Keys = keys;
        }
    }

    public class Replay
    {
        public GameMode Mode { get; set; }
        public Mods Mods { get; set; }
        public List<ReplayFrame> Frames { get; set; }
        public string ReplayVersion { get; set; }
        public string PlayerName { get; set; }
        public string MapMd5 { get; set; }
        public int RandomizeModifierSeed { get; set; } = -1;

        public static Replay Parse(string filePath)
        {
            var parsedReplay = new Replay();

            using (var fs = new FileStream(filePath, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                parsedReplay.ReplayVersion = br.ReadString();
                parsedReplay.MapMd5 = br.ReadString();
                br.ReadString();
                parsedReplay.PlayerName = br.ReadString();
                br.ReadString();
                br.ReadInt64();
                parsedReplay.Mode = (GameMode)br.ReadInt32();

                if (parsedReplay.ReplayVersion == "0.0.1" || parsedReplay.ReplayVersion == "None")
                    parsedReplay.Mods = (Mods)br.ReadInt32();
                else
                    parsedReplay.Mods = (Mods)br.ReadInt64();

                br.ReadBytes(40);

                if (parsedReplay.ReplayVersion != "None")
                {
                    var replayVersion = new Version(parsedReplay.ReplayVersion);
                    if (replayVersion >= new Version("0.0.1"))
                        parsedReplay.RandomizeModifierSeed = br.ReadInt32();
                }

                parsedReplay.Frames = new List<ReplayFrame>();
                var frames = Encoding.ASCII.GetString(LZMAHelper.Decompress(br.BaseStream).ToArray()).Split(',').ToList();

                foreach (var frame in frames)
                {
                    try
                    {
                        var frameSplit = frame.Split('|');
                        parsedReplay.Frames.Add(new ReplayFrame(
                            int.Parse(frameSplit[0]),
                            (ReplayKeyPressState)Enum.Parse(typeof(ReplayKeyPressState), frameSplit[1])));
                    }
                    catch
                    {
                    }
                }
            }

            return parsedReplay;
        }

        public static Replay GenerateAutoplayReplay(Qua map)
        {
            var replay = new Replay
            {
                PlayerName = "Autoplay",
                Mode = map.Mode,
                Frames = new List<ReplayFrame>()
            };

            var nonCombined = new List<ReplayAutoplayFrame>();

            foreach (var hitObject in map.HitObjects)
            {
                if (!IsValidLane(hitObject.Lane))
                    continue;

                nonCombined.Add(new ReplayAutoplayFrame(hitObject, ReplayAutoplayFrameType.Press, hitObject.StartTime, KeyLaneToPressState(hitObject.Lane)));

                if (hitObject.IsLongNote)
                    nonCombined.Add(new ReplayAutoplayFrame(hitObject, ReplayAutoplayFrameType.Release, hitObject.EndTime - 1, KeyLaneToPressState(hitObject.Lane)));
                else
                    nonCombined.Add(new ReplayAutoplayFrame(hitObject, ReplayAutoplayFrameType.Release, hitObject.StartTime + 30, KeyLaneToPressState(hitObject.Lane)));
            }

            nonCombined = nonCombined.OrderBy(x => x.Time).ToList();
            var state = (ReplayKeyPressState)0;

            replay.Frames.Add(new ReplayFrame(-10000, 0));

            foreach (var item in nonCombined.GroupBy(x => x.Time).ToDictionary(x => x.Key, x => x.ToList()))
            {
                foreach (var frame in item.Value)
                {
                    switch (frame.Type)
                    {
                        case ReplayAutoplayFrameType.Press:
                            state |= KeyLaneToPressState(frame.HitObject.Lane);
                            break;
                        case ReplayAutoplayFrameType.Release:
                            state -= KeyLaneToPressState(frame.HitObject.Lane);
                            break;
                    }
                }

                replay.Frames.Add(new ReplayFrame(item.Key, state));
            }

            return replay;
        }

        public static ReplayKeyPressState KeyLaneToPressState(int lane)
        {
            if (!IsValidLane(lane))
                throw new ArgumentOutOfRangeException(nameof(lane), lane, "Lane must be between 1 and 10.");

            return (ReplayKeyPressState)Enum.Parse(typeof(ReplayKeyPressState), $"K{lane}");
        }

        private static bool IsValidLane(int lane) => lane >= 1 && lane <= 10;

        public static List<int> KeyPressStateToLanes(ReplayKeyPressState keys)
        {
            var lanes = new List<int>();

            if (keys.HasFlag(ReplayKeyPressState.K1)) lanes.Add(0);
            if (keys.HasFlag(ReplayKeyPressState.K2)) lanes.Add(1);
            if (keys.HasFlag(ReplayKeyPressState.K3)) lanes.Add(2);
            if (keys.HasFlag(ReplayKeyPressState.K4)) lanes.Add(3);
            if (keys.HasFlag(ReplayKeyPressState.K5)) lanes.Add(4);
            if (keys.HasFlag(ReplayKeyPressState.K6)) lanes.Add(5);
            if (keys.HasFlag(ReplayKeyPressState.K7)) lanes.Add(6);
            if (keys.HasFlag(ReplayKeyPressState.K8)) lanes.Add(7);
            if (keys.HasFlag(ReplayKeyPressState.K9)) lanes.Add(8);

            return lanes;
        }
    }
}
