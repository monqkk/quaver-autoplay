using SimpleDependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace monk.API
{
    public class GamePtr
    {
        protected ProcessReader Process;

        public bool SingleComponentLoaded => Parent?.SingleComponentLoaded ?? true && BaseAddress != UIntPtr.Zero;
        public virtual bool IsLoaded => SingleComponentLoaded && Children.All(child => child.IsLoaded);

        private UIntPtr? pointerToBaseAddress;

        public virtual UIntPtr BaseAddress
        {
            get
            {
                if (pointerToBaseAddress.HasValue)
                    return (UIntPtr)Process.ReadUInt64(pointerToBaseAddress.Value);

                if (Parent.SingleComponentLoaded)
                    return (UIntPtr)Process.ReadUInt64(Parent.BaseAddress + Offset);

                return UIntPtr.Zero;
            }
        }

        public int Offset;
        public GamePtr Parent { get; set; }

        private List<GamePtr> children = new List<GamePtr>();
        public GamePtr[] Children
        {
            get => children.ToArray();
            set
            {
                children = value.ToList();
                foreach (var child in children)
                    child.Parent = this;
            }
        }

        public GamePtr(UIntPtr? pointerToBaseAddress = null)
        {
            this.pointerToBaseAddress = pointerToBaseAddress;
            Process = DependencyContainer.Get<ProcessReader>();
        }
    }

    public class Game : GamePtr
    {
        public GameplayScreen GameplayScreen { get; private set; }

        public Game(UIntPtr pointerToBaseAddress) : base(pointerToBaseAddress)
        {
            Children = new GamePtr[]
            {
                GameplayScreen = new GameplayScreen { Offset = 0x128 }
            };
        }
    }

    public class GameplayScreen : GamePtr
    {
        public override bool IsLoaded => base.IsLoaded && Process.ReadInt32(BaseAddress + 0xF0) == 2;

        public AudioTiming GameplayAudioTiming { get; private set; }
        public Ruleset Ruleset { get; private set; }

        public string CurrentMapChecksum => Process.ReadString(BaseAddress + 0x70, true);

        public Qua CurrentMap
        {
            get
            {
                var qua = new Qua();
                var mapsetPointer = (UIntPtr)Process.ReadUInt64(BaseAddress + 0x58);

                qua.Mode = (GameMode)Process.ReadInt32(mapsetPointer + 0xA4);
                qua.Title = Process.ReadString(mapsetPointer + 0x20, true);
                qua.Artist = Process.ReadString(mapsetPointer + 0x28, true);
                qua.Creator = Process.ReadString(mapsetPointer + 0x40, true);
                qua.DifficultyName = Process.ReadString(mapsetPointer + 0x48, true);
                qua.Checksum = CurrentMapChecksum;

                var hitObjectsList = (UIntPtr)Process.ReadUInt64(mapsetPointer + 0x88);
                var hitObjectsElements = (UIntPtr)Process.ReadUInt64(hitObjectsList + 0x8);
                var count = Process.ReadInt32(hitObjectsElements + 0x8);

                for (var i = 0; i < count; i++)
                {
                    var currentElement = (UIntPtr)Process.ReadUInt64(hitObjectsElements + 0x10 + 0x8 * i);
                    qua.HitObjects.Add(new HitObject
                    {
                        Lane = Process.ReadInt32(currentElement + 0x10),
                        StartTime = Process.ReadInt32(currentElement + 0x14),
                        EndTime = Process.ReadInt32(currentElement + 0x18)
                    });
                }

                return qua;
            }
        }

        public GameplayScreen()
        {
            Children = new GamePtr[]
            {
                GameplayAudioTiming = new AudioTiming { Offset = 0x48 },
                Ruleset = new Ruleset { Offset = 0x50 }
            };
        }
    }

    public class AudioTiming : GamePtr
    {
        public double Time => Process.ReadDouble(BaseAddress + 0x10);
    }

    public class Ruleset : GamePtr
    {
        public ScoreProcessor ScoreProcessor { get; private set; }

        public Ruleset()
        {
            Children = new GamePtr[]
            {
                ScoreProcessor = new ScoreProcessor { Offset = 0x30 }
            };
        }
    }

    public class ScoreProcessor : GamePtr
    {
        public Mods CurrentMods => (Mods)Process.ReadInt64(BaseAddress + 0x40);
    }
}
