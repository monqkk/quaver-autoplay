namespace monk.API
{
    public class Signature
    {
        public string Pattern;
        public int Offset;
        public string Name;
    }

    public static class Signatures
    {
        public static readonly Signature[] QuaverBaseCandidates =
        {
            new Signature
            {
                Name = "primary",
                Pattern = "48 89 0C 25 ?? ?? ?? ?? 48 B9 ?? ?? ?? ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 89 45 F8",
                Offset = 0x4
            },
            new Signature
            {
                Name = "fallback",
                Pattern = "BA 02 00 00 00 E8 ?? ?? ?? ?? 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 48 8B ??",
                Offset = 0xC
            }
        };
    }
}
