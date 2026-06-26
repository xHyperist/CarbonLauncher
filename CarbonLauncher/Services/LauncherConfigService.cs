using System;
using System.IO;
using System.Text.Json;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LauncherConfigService
    {
        private const int MinimumMemoryMb = 512;
        private const int DefaultMemoryMb = 2048;
        private const double DefaultWindowWidth = 1180;
        private const double DefaultWindowHeight = 720;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public string ConfigDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CarbonLauncher");

        public string ConfigPath => Path.Combine(ConfigDirectory, "config.json");

        public LauncherConfig Load()
        {
            EnsureConfigDirectory();

            if (!File.Exists(ConfigPath))
            {
                LauncherConfig defaultConfig = CreateDefault();
                Save(defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                LauncherConfig? config = JsonSerializer.Deserialize<LauncherConfig>(json, _jsonOptions);
                config = Normalize(config ?? CreateDefault());
                Save(config);
                return config;
            }
            catch
            {
                BackupBrokenConfig();
                LauncherConfig defaultConfig = CreateDefault();
                Save(defaultConfig);
                return defaultConfig;
            }
        }

        public void Save(LauncherConfig config)
        {
            EnsureConfigDirectory();
            LauncherConfig normalizedConfig = Normalize(config);
            string json = JsonSerializer.Serialize(normalizedConfig, _jsonOptions);
            File.WriteAllText(ConfigPath, json);
        }

        private LauncherConfig CreateDefault()
        {
            return new LauncherConfig();
        }

        private LauncherConfig Normalize(LauncherConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.SelectedVersion))
            {
                config.SelectedVersion = "1.8.9";
            }

            if (string.IsNullOrWhiteSpace(config.GuestUsername) || config.GuestUsername == "Guest")
            {
                config.GuestUsername = "CarbonPlayer";
            }

            config.JavaPath = config.JavaPath ?? string.Empty;
            config.MinecraftDirectory = config.MinecraftDirectory ?? string.Empty;

            if (config.AllocatedMemoryMb < MinimumMemoryMb)
            {
                config.AllocatedMemoryMb = DefaultMemoryMb;
            }

            if (string.IsNullOrWhiteSpace(config.LastSelectedPage))
            {
                config.LastSelectedPage = "Home";
            }

            if (config.WindowWidth <= 0)
            {
                config.WindowWidth = DefaultWindowWidth;
            }

            if (config.WindowHeight <= 0)
            {
                config.WindowHeight = DefaultWindowHeight;
            }

            return config;
        }

        private void EnsureConfigDirectory()
        {
            Directory.CreateDirectory(ConfigDirectory);
        }

        private void BackupBrokenConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupPath = Path.Combine(ConfigDirectory, $"config.broken.{timestamp}.json");
            File.Copy(ConfigPath, backupPath, true);
        }
    }
}
