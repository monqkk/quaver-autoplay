using SimpleDependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace monk.API
{
    public class ProcessReader
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern int VirtualQueryEx32(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION_32 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern int VirtualQueryEx64(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION_64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);

        public Process Process { get; private set; }

        public ProcessReader(Process process) => Process = process;

        public bool Is64BitProcess
        {
            get
            {
                if (!Environment.Is64BitOperatingSystem)
                    return false;

                if (IsWow64Process(Process.Handle, out bool isWow64))
                    return !isWow64;

                return true;
            }
        }

        public bool FindPattern(string pattern, out UIntPtr result)
        {
            var parsedPattern = Pattern.Parse(pattern);
            var regions = EnumerateMemoryRegions();

            foreach (var region in regions)
            {
                if (region.RegionSize.ToUInt64() > uint.MaxValue)
                    continue;

                var buffer = ReadMemory(region.BaseAddress, region.RegionSize.ToUInt32());
                if (FindMatch(parsedPattern, buffer, out UIntPtr match))
                {
                    result = (UIntPtr)(region.BaseAddress.ToUInt64() + match.ToUInt64());
                    return true;
                }
            }

            result = UIntPtr.Zero;
            return false;
        }

        public List<MemoryRegion> EnumerateMemoryRegions()
        {
            var regions = new List<MemoryRegion>();
            ulong address = 0;

            do
            {
                MemoryRegion region;
                if (Is64BitProcess)
                {
                    VirtualQueryEx64(Process.Handle, (UIntPtr)address, out MEMORY_BASIC_INFORMATION_64 info64, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION_64)));
                    region = new MemoryRegion(info64);
                }
                else
                {
                    VirtualQueryEx32(Process.Handle, (UIntPtr)address, out MEMORY_BASIC_INFORMATION_32 info32, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION_32)));
                    region = new MemoryRegion(info32);
                }

                if (region.State != MemoryState.MemFree && !region.Protect.HasFlag(MemoryProtect.PageGuard))
                    regions.Add(region);

                if (address == (ulong)region.BaseAddress + (ulong)region.RegionSize)
                    break;

                address = (ulong)region.BaseAddress + (ulong)region.RegionSize;
            }
            while (address <= ulong.MaxValue);

            return regions;
        }

        public byte[] ReadMemory(UIntPtr address, uint size)
        {
            var result = new byte[size];
            ReadProcessMemory(Process.Handle, address, result, size, out _);
            return result;
        }

        public int ReadInt32(UIntPtr address) => BitConverter.ToInt32(ReadMemory(address, sizeof(int)), 0);
        public uint ReadUInt32(UIntPtr address) => BitConverter.ToUInt32(ReadMemory(address, sizeof(uint)), 0);
        public long ReadInt64(UIntPtr address) => BitConverter.ToInt64(ReadMemory(address, sizeof(long)), 0);
        public ulong ReadUInt64(UIntPtr address) => BitConverter.ToUInt64(ReadMemory(address, sizeof(ulong)), 0);
        public double ReadDouble(UIntPtr address) => BitConverter.ToDouble(ReadMemory(address, sizeof(double)), 0);

        public string ReadString(UIntPtr address, bool multiply = false, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            var stringAddress = (UIntPtr)ReadUInt64(address);
            var length = ReadInt32(stringAddress + 0x8) * (multiply ? 2 : 1);
            return encoding.GetString(ReadMemory(stringAddress + 0xC, (uint)length)).Replace("\0", string.Empty);
        }

        private unsafe bool FindMatch(Pattern pattern, byte[] buffer, out UIntPtr result)
        {
            result = UIntPtr.Zero;
            var patternLength = pattern.Bytes.Length;
            var bufferLength = buffer.Length;

            fixed (byte* bufferPtr = buffer)
            fixed (bool* maskPtr = pattern.Mask)
            fixed (byte* patternPtr = pattern.Bytes)
            {
                for (var i = 0; i + patternLength <= bufferLength; i++)
                {
                    for (var j = 0; j < patternLength; j++)
                    {
                        if (!maskPtr[j] || patternPtr[j] == bufferPtr[i + j])
                            continue;

                        goto next;
                    }

                    result = (UIntPtr)i;
                    return true;

                next:;
                }
            }

            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION_32
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public MemoryState State;
            public MemoryProtect Protect;
            public MemoryType Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION_64
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            private int alignment1;
            public UIntPtr RegionSize;
            public MemoryState State;
            public MemoryProtect Protect;
            public MemoryType Type;
            private int alignment2;
        }
    }

    public class Pattern
    {
        public byte[] Bytes;
        public bool[] Mask;

        public static Pattern Parse(string pattern)
        {
            var patternSplit = pattern.Split(' ');
            return new Pattern
            {
                Bytes = Array.ConvertAll(patternSplit, b => b == "??" ? (byte)0x0 : Convert.ToByte(b, 16)),
                Mask = Array.ConvertAll(patternSplit, b => b != "??")
            };
        }
    }

    public class MemoryRegion
    {
        public UIntPtr BaseAddress { get; }
        public UIntPtr RegionSize { get; }
        public MemoryState State { get; }
        public MemoryProtect Protect { get; }
        public MemoryType Type { get; }

        public MemoryRegion(ProcessReader.MEMORY_BASIC_INFORMATION_32 info)
        {
            BaseAddress = info.BaseAddress;
            RegionSize = info.RegionSize;
            State = info.State;
            Protect = info.Protect;
            Type = info.Type;
        }

        public MemoryRegion(ProcessReader.MEMORY_BASIC_INFORMATION_64 info)
        {
            BaseAddress = info.BaseAddress;
            RegionSize = info.RegionSize;
            State = info.State;
            Protect = info.Protect;
            Type = info.Type;
        }
    }
}
