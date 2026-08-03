using SimpleDependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WindowsInput.Native;

namespace monk.API
{
    public class Config
    {
        private readonly string configPath;
        private string[] rawConfig;

        public string SongsDirectory { get; private set; }
        public Dictionary<KeyBind, VirtualKeyCode> KeyBindings = new Dictionary<KeyBind, VirtualKeyCode>();

        public Config(string configPath)
        {
            this.configPath = configPath;
            RefreshConfig();
        }

        public void RefreshConfig()
        {
            rawConfig = File.ReadAllLines(configPath);
            SongsDirectory = FindLine("SongDirectory").Split('=')[1].Trim();

            foreach (KeyBind key in Enum.GetValues(typeof(KeyBind)))
            {
                var line = FindLine(key.ToString());
                var rawKey = line.Split('=')[1].Trim();
                KeyBindings[key] = (VirtualKeyCode)(int)(XNAKeys)Enum.Parse(typeof(XNAKeys), rawKey);
            }
        }

        public VirtualKeyCode GetBindedKey(KeyBind key) => KeyBindings[key];

        private string FindLine(string configKey)
        {
            var line = Array.Find(rawConfig, l => l.StartsWith(configKey));
            if (line == default)
                throw new Exception($"Configuration key [{configKey}] was not found in the config file!");

            return line;
        }
    }
}
