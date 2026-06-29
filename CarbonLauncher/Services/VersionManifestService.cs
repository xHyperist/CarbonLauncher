using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class VersionManifestService
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly LauncherStorageService _storageService;
        private readonly LauncherStorageInfo _storageInfo;

        public VersionManifestService()
            : this(new LauncherStorageService())
        {
        }

        public VersionManifestService(LauncherStorageService storageService)
        {
            _storageService = storageService;
            _storageInfo = _storageService.GetStorageInfo();
        }

        public string ManifestDirectory => _storageInfo.RootDirectory;

        public string ManifestPath => _storageInfo.VersionManifestPath;

        public VersionManifest Load()
        {
            EnsureManifestDirectory();

            if (!File.Exists(ManifestPath))
            {
                VersionManifest defaultManifest = CreateDefaultManifest();
                Save(defaultManifest);
                return defaultManifest;
            }

            try
            {
                string json = File.ReadAllText(ManifestPath);
                VersionManifest? manifest = JsonSerializer.Deserialize<VersionManifest>(json, _jsonOptions);
                manifest = Normalize(manifest ?? CreateDefaultManifest());
                Save(manifest);
                return manifest;
            }
            catch
            {
                BackupBrokenManifest();
                VersionManifest defaultManifest = CreateDefaultManifest();
                Save(defaultManifest);
                return defaultManifest;
            }
        }

        public void Save(VersionManifest manifest)
        {
            EnsureManifestDirectory();
            VersionManifest normalizedManifest = Normalize(manifest);
            string json = JsonSerializer.Serialize(normalizedManifest, _jsonOptions);
            File.WriteAllText(ManifestPath, json);
        }

        private VersionManifest CreateDefaultManifest()
        {
            return new VersionManifest
            {
                ManifestVersion = "1",
                UpdatedAt = DateTime.Now,
                Versions = new List<LauncherVersion>
                {
                    new LauncherVersion
                    {
                        Id = "carbon-1.8.9",
                        DisplayName = "Carbon 1.8.9",
                        MinecraftVersion = "1.8.9",
                        LoaderType = "Forge",
                        Status = "Available",
                        IsAvailable = true,
                        IsComingSoon = false,
                        Description = "Primary Carbon Client version.",
                        LocalJarPath = string.Empty,
                        RemoteManifestUrl = string.Empty,
                        ReleaseChannel = "Stable",
                        LaunchMode = "CustomClient",
                        ReleaseDate = null
                    },
                    new LauncherVersion
                    {
                        Id = "carbon-1.7.10",
                        DisplayName = "Carbon 1.7.10",
                        MinecraftVersion = "1.7.10",
                        LoaderType = "Forge",
                        Status = "Coming Soon",
                        IsAvailable = false,
                        IsComingSoon = true,
                        Description = "Legacy Carbon Client version planned for a future release.",
                        LocalJarPath = string.Empty,
                        RemoteManifestUrl = string.Empty,
                        ReleaseChannel = "Planned",
                        ReleaseDate = null
                    }
                }
            };
        }

        private VersionManifest Normalize(VersionManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.ManifestVersion))
            {
                manifest.ManifestVersion = "1";
            }

            if (manifest.UpdatedAt == default)
            {
                manifest.UpdatedAt = DateTime.Now;
            }

            if (manifest.Versions == null || manifest.Versions.Count == 0)
            {
                manifest.Versions = CreateDefaultManifest().Versions;
            }

            foreach (LauncherVersion version in manifest.Versions)
            {
                NormalizeVersion(version);
            }

            return manifest;
        }

        private static void NormalizeVersion(LauncherVersion version)
        {
            if (string.IsNullOrWhiteSpace(version.MinecraftVersion))
            {
                version.MinecraftVersion = "1.8.9";
            }

            if (string.IsNullOrWhiteSpace(version.Id))
            {
                version.Id = $"carbon-{version.MinecraftVersion}";
            }

            if (string.IsNullOrWhiteSpace(version.DisplayName))
            {
                version.DisplayName = $"Carbon {version.MinecraftVersion}";
            }

            if (string.IsNullOrWhiteSpace(version.LoaderType))
            {
                version.LoaderType = "Forge";
            }

            if (version.IsAvailable && string.IsNullOrWhiteSpace(version.LaunchMode))
            {
                version.LaunchMode = "CustomClient";
            }

            version.TweakClass = version.TweakClass ?? string.Empty;
            version.MainClassOverride = version.MainClassOverride ?? string.Empty;

            if (string.IsNullOrWhiteSpace(version.Status))
            {
                version.Status = version.IsAvailable ? "Available" : "Coming Soon";
            }

            if (version.IsComingSoon)
            {
                version.IsAvailable = false;
            }

            if (!version.IsAvailable && version.Status == "Available")
            {
                version.IsAvailable = true;
            }

            if (!version.IsComingSoon && !version.IsAvailable)
            {
                version.IsComingSoon = true;
            }
        }

        private void EnsureManifestDirectory()
        {
            _storageService.EnsureStorage();
        }

        private void BackupBrokenManifest()
        {
            if (!File.Exists(ManifestPath))
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string backupPath = Path.Combine(ManifestDirectory, $"versions.broken.{timestamp}.json");
            File.Copy(ManifestPath, backupPath, true);
        }
    }
}
