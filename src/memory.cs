using monk.API;
using SimpleDependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MonkAutoplay
{
    internal static class Memory
    {
        public static ProcessReader QuaverProcess { get; private set; }
        public static Game Quaver { get; private set; }
        public static Config Config { get; private set; }
        public static string QuaverDirectory { get; private set; }
        public static string LastError { get; private set; }

        public static bool Initialize()
        {
            Console.Clear();
            Console.WriteLine("  looking for Quaver...\n");

            Process quaverProcess;
            try
            {
                quaverProcess = Process.GetProcessesByName("Quaver").FirstOrDefault();
            }
            catch (Exception ex)
            {
                LastError = $"Could not enumerate Quaver process: {ex.Message}";
                Console.WriteLine($"\n{LastError}");
                return false;
            }

            if (quaverProcess == null)
            {
                Console.WriteLine("  waiting for Quaver...\n");

                while (quaverProcess == null)
                {
                    quaverProcess = Process.GetProcessesByName("Quaver").FirstOrDefault();
                    Thread.Sleep(1500);
                }
            }

            try
            {
                QuaverProcess = new ProcessReader(quaverProcess);
            }
            catch (Exception ex)
            {
                LastError = $"Could not open Quaver process: {ex.Message}\nTry running monk-autoplay as Administrator.";
                Console.WriteLine($"\n{LastError}");
                return false;
            }

            QuaverProcess.Process.EnableRaisingEvents = true;
            QuaverProcess.Process.Exited += (_, __) => Environment.Exit(1);
            DependencyContainer.Cache(QuaverProcess);

            try
            {
                QuaverDirectory = Path.GetDirectoryName(QuaverProcess.Process.MainModule.FileName);
            }
            catch (Exception ex)
            {
                LastError = $"Could not read Quaver install path: {ex.Message}";
                Console.WriteLine($"\n{LastError}");
                return false;
            }

            Console.WriteLine($"  found Quaver at {QuaverDirectory}\n");

            try
            {
                if (!TryResolveQuaverBase(out UIntPtr quaverBaseAddress))
                    return false;

                Quaver = new Game(quaverBaseAddress);

                var configPath = Path.Combine(QuaverDirectory, "quaver.cfg");
                if (!File.Exists(configPath))
                {
                    LastError = $"quaver.cfg not found at: {configPath}";
                    Console.WriteLine($"\n{LastError}");
                    return false;
                }

                Config = new Config(configPath);
            }
            catch (Exception ex)
            {
                LastError = $"Initialization failed: {ex.Message}";
                Console.WriteLine($"\n{LastError}");
                return false;
            }

            return true;
        }

        private static bool TryResolveQuaverBase(out UIntPtr quaverBaseAddress)
        {
            quaverBaseAddress = UIntPtr.Zero;

            Console.WriteLine("  hooking...\n");

            foreach (var signature in Signatures.QuaverBaseCandidates)
            {
                if (!QuaverProcess.FindPattern(signature.Pattern, out UIntPtr matchAddress))
                    continue;

                var pointerAddress = (UIntPtr)(matchAddress.ToUInt64() + (ulong)signature.Offset);
                quaverBaseAddress = ReadPointer(pointerAddress);

                if (quaverBaseAddress != UIntPtr.Zero)
                    return true;
            }

            LastError = "Could not find Quaver in memory.\nQuaver may have updated — memory signatures need updating.";
            Console.WriteLine($"\n{LastError}");
            return false;
        }

        private static UIntPtr ReadPointer(UIntPtr address)
        {
            if (QuaverProcess.Is64BitProcess)
                return (UIntPtr)QuaverProcess.ReadUInt64(address);

            return (UIntPtr)QuaverProcess.ReadUInt32(address);
        }
    }
}
