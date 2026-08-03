using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace monk.API
{
    public class HitObject
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int Lane { get; set; }

        public bool IsLongNote => EndTime > 0;
    }

    public class Qua
    {
        public GameMode Mode { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Creator { get; set; }
        public string DifficultyName { get; set; }
        public string Checksum { get; set; }
        public List<HitObject> HitObjects { get; set; } = new List<HitObject>();

        public static Qua Parse(string filePath)
        {
            var parsedMap = new Qua();
            var lines = File.ReadAllLines(filePath);
            var isHitobjects = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineSplit = line.Split(new[] { ':' }, 2);
                var variable = lineSplit[0].Trim().TrimStart('-').Trim();
                var value = lineSplit[1].Trim();

                if (variable == "HitObjects")
                    isHitobjects = true;

                switch (variable)
                {
                    case "Mode":
                        parsedMap.Mode = (GameMode)Enum.Parse(typeof(GameMode), value);
                        break;
                    case "Title":
                        parsedMap.Title = value;
                        break;
                    case "Artist":
                        parsedMap.Artist = value;
                        break;
                    case "Creator":
                        parsedMap.Creator = value;
                        break;
                    case "DifficultyName":
                        parsedMap.DifficultyName = value;
                        break;
                    case "StartTime" when isHitobjects:
                        var ho = new HitObject { StartTime = int.Parse(value) };
                        ho.Lane = int.Parse(lines[++i].Split(new[] { ':' }, 2)[1].Trim());
                        if (i + 1 < lines.Length && lines[i + 1].Contains("EndTime"))
                            ho.EndTime = int.Parse(lines[++i].Split(new[] { ':' }, 2)[1].Trim());
                        parsedMap.HitObjects.Add(ho);
                        break;
                }
            }

            return parsedMap;
        }
    }

    public static class MapLoader
    {
        public static Qua Load(string songsDirectory, string checksum, Qua memoryMetadata, string quaverDirectory = null)
        {
            if (memoryMetadata == null)
                throw new ArgumentNullException(nameof(memoryMetadata));

            songsDirectory = ResolveSongsDirectory(songsDirectory, quaverDirectory);

            var fromDisk = TryLoadFromDisk(songsDirectory, checksum, memoryMetadata);
            if (fromDisk != null && fromDisk.HitObjects.Count > 0)
                return fromDisk;

            FixHitObjectLayout(memoryMetadata);
            return memoryMetadata;
        }

        private static Qua TryLoadFromDisk(string songsDirectory, string checksum, Qua memoryMetadata)
        {
            if (string.IsNullOrWhiteSpace(songsDirectory) || !Directory.Exists(songsDirectory))
                return null;

            Qua metadataMatch = null;

            foreach (var quaPath in Directory.EnumerateFiles(songsDirectory, "*.qua", SearchOption.AllDirectories))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(checksum))
                    {
                        var fileChecksum = ComputeChecksum(quaPath);
                        if (string.Equals(fileChecksum, checksum, StringComparison.OrdinalIgnoreCase))
                            return Qua.Parse(quaPath);
                    }

                    var parsed = Qua.Parse(quaPath);
                    if (MetadataMatches(parsed, memoryMetadata))
                        metadataMatch = parsed;
                }
                catch
                {
                }
            }

            return metadataMatch;
        }

        private static bool MetadataMatches(Qua parsed, Qua memoryMetadata)
        {
            return string.Equals(parsed.Title, memoryMetadata.Title, StringComparison.Ordinal)
                && string.Equals(parsed.Artist, memoryMetadata.Artist, StringComparison.Ordinal)
                && string.Equals(parsed.DifficultyName, memoryMetadata.DifficultyName, StringComparison.Ordinal)
                && parsed.Mode == memoryMetadata.Mode;
        }

        private static string ComputeChecksum(string quaPath)
        {
            var content = File.ReadAllBytes(quaPath);

            using (var md5 = MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(content)).Replace("-", "").ToLowerInvariant();
        }

        private static void FixHitObjectLayout(Qua map)
        {
            if (map.HitObjects.Count == 0)
                return;

            if (map.HitObjects.All(x => IsValidLane(x.Lane)))
                return;

            foreach (var hitObject in map.HitObjects)
            {
                if (IsValidLane(hitObject.Lane))
                    continue;

                var startTime = hitObject.StartTime;
                var endTime = hitObject.EndTime;
                var lane = hitObject.Lane;

                if (IsValidLane(startTime))
                {
                    hitObject.Lane = startTime;
                    hitObject.StartTime = lane;

                    if (endTime > 0 && endTime < lane)
                        hitObject.EndTime = lane;
                }
                else if (IsValidLane(endTime))
                {
                    hitObject.Lane = endTime;
                    hitObject.EndTime = lane;
                }
            }

            map.HitObjects.RemoveAll(x => !IsValidLane(x.Lane));
        }

        private static bool IsValidLane(int lane) => lane >= 1 && lane <= 10;

        private static string ResolveSongsDirectory(string songsDirectory, string quaverDirectory)
        {
            if (string.IsNullOrWhiteSpace(songsDirectory))
                return songsDirectory;

            if (Path.IsPathRooted(songsDirectory) || string.IsNullOrWhiteSpace(quaverDirectory))
                return songsDirectory;

            return Path.GetFullPath(Path.Combine(quaverDirectory, songsDirectory));
        }
    }
}
