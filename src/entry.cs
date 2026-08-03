using monk.API;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MonkAutoplay
{
    internal static class Entry
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [STAThread]
        private static void Main()
        {
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n  something went wrong:");
                Console.WriteLine($"  {ex.Message}");
                Console.ResetColor();
                Pause();
            }
        }

        private static void Run()
        {
            AllocConsole();
            Console.Title = "monk-autoplay";
            ShowBanner();

            if (!Memory.Initialize())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n  couldn't attach to quaver.");
                if (!string.IsNullOrEmpty(Memory.LastError))
                    Console.WriteLine($"  {Memory.LastError.Replace("\n", "\n  ")}");
                Console.ResetColor();
                ShowHelp();
                Pause();
                return;
            }

            Autoplayer.ReloadSettings();
            Status("  hook initialised.");

            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    return;

                if (!Memory.Quaver.GameplayScreen.IsLoaded)
                {
                    Thread.Sleep(150);
                    continue;
                }

                try
                {
                    Autoplayer.ReloadSettings();
                    Memory.Config.RefreshConfig();

                    var screen = Memory.Quaver.GameplayScreen;
                    var map = MapLoader.Load(
                        Memory.Config.SongsDirectory,
                        screen.CurrentMapChecksum,
                        screen.CurrentMap,
                        Memory.QuaverDirectory);

                    if (map.HitObjects.Count == 0)
                    {
                        Status("  couldn't load notes, skipping...");
                        Thread.Sleep(1000);
                        continue;
                    }

                    var replay = Replay.GenerateAutoplayReplay(map);
                    var mirror = screen.Ruleset.ScoreProcessor.CurrentMods.HasFlag(Mods.Mirror);
                    var mode = map.Mode == GameMode.Keys4 ? "4k" : "7k";

                    Console.WriteLine();
                    Song($"  [{mode}] {map.Artist} - {map.Title} [{map.DifficultyName}]");
                    Song($"       {map.HitObjects.Count} notes");

                    if (mirror)
                        Song("       mirror on");

                    Autoplayer.Run(replay, mirror);

                    Status("  done.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  error: {ex.Message}");
                    Console.ResetColor();
                }

                Thread.Sleep(500);
            }
        }

        private static void ShowBanner()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(@"
    __  __            _     _   _____      _       _   
   |  \/  | ___   __| | __| | |  _ \ _ __ | | __ _| |_ 
   | |\/| |/ _ \ / _` |/ _` | | |_) | '_ \| |/ _` | __|
   | |  | | (_) | (_| | (_| | |  __/| |_) | | (_| | |_ 
   |_|  |_|\___/ \__,_|\__,_| |_|   | .__/|_|\__,_|\__|
                                    |_|   autoplay
");
            Console.ResetColor();
            Console.WriteLine("  plays your maps for you. 4k and 7k. offline use only.\n");
        }

        private static void ShowHelp()
        {
            Status("  tips:");
            Status("  - open quaver first, then run monk-autoplay");
            Status("  - run as admin if it can't attach");
            Console.WriteLine("  - esc to quit\n");
        }

        private static void Status(string message) => Console.WriteLine($"{message}\n");

        private static void Song(string message) => Console.WriteLine(message);

        private static void Pause()
        {
            Console.WriteLine("\n  press enter to close...");
            Console.ReadLine();
        }
    }
}
