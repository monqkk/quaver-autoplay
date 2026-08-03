using System;

namespace monk.API
{
    public enum GameMode
    {
        Keys4 = 1,
        Keys7 = 2
    }

    [Flags]
    public enum Mods : long
    {
        None = -1L,
        NoSliderVelocity = 1L << 0,
        Speed05X = 1L << 1,
        Speed06X = 1L << 2,
        Speed07X = 1L << 3,
        Speed08X = 1L << 4,
        Speed09X = 1L << 5,
        Speed11X = 1L << 6,
        Speed12X = 1L << 7,
        Speed13X = 1L << 8,
        Speed14X = 1L << 9,
        Speed15X = 1L << 10,
        Speed16X = 1L << 11,
        Speed17X = 1L << 12,
        Speed18X = 1L << 13,
        Speed19X = 1L << 14,
        Speed20X = 1L << 15,
        Strict = 1L << 16,
        Chill = 1L << 17,
        NoPause = 1L << 18,
        Autoplay = 1L << 19,
        Paused = 1L << 20,
        NoFail = 1L << 21,
        NoLongNotes = 1L << 22,
        Randomize = 1L << 23,
        Speed055X = 1L << 24,
        Speed065X = 1L << 25,
        Speed075X = 1L << 26,
        Speed085X = 1L << 27,
        Speed095X = 1L << 28,
        Inverse = 1L << 29,
        FullLN = 1L << 30,
        Mirror = 1L << 31,
        Coop = 1L << 32,
        Speed105X = 1L << 33,
        Speed115X = 1L << 34,
        Speed125X = 1L << 35,
        Speed135X = 1L << 36,
        Speed145X = 1L << 37,
        Speed155X = 1L << 38,
        Speed165X = 1L << 39,
        Speed175X = 1L << 40,
        Speed185X = 1L << 41,
        Speed195X = 1L << 42,
        HeatlthAdjust = 1L << 43
    }

    public enum KeyBind
    {
        KeyMania4K1,
        KeyMania4K2,
        KeyMania4K3,
        KeyMania4K4,
        KeyMania7K1,
        KeyMania7K2,
        KeyMania7K3,
        KeyMania7K4,
        KeyMania7K5,
        KeyMania7K6,
        KeyMania7K7
    }

    public enum MemoryProtect
    {
        PageNoAccess = 0x00000001,
        PageReadonly = 0x00000002,
        PageReadWrite = 0x00000004,
        PageWriteCopy = 0x00000008,
        PageExecute = 0x00000010,
        PageExecuteRead = 0x00000020,
        PageExecuteReadWrite = 0x00000040,
        PageExecuteWriteCopy = 0x00000080,
        PageGuard = 0x00000100,
        PageNoCache = 0x00000200,
        PageWriteCombine = 0x00000400
    }

    public enum MemoryState
    {
        MemCommit = 0x1000,
        MemReserved = 0x2000,
        MemFree = 0x10000
    }

    public enum MemoryType
    {
        MemPrivate = 0x20000,
        MemMapped = 0x40000,
        MemImage = 0x1000000
    }
}
