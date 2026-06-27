using System;
using System.IO;
using System.Text.Json;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class MinecraftRuntimeResolverService
    {
        private readonly LauncherStorageService _storageService;

        public MinecraftRuntimeResolverService()
            : this(new LauncherStorageService())
        {
        }

        public MinecraftRuntimeResolverService(LauncherStorageService storageService)
        {
            _storageService = storageService;
        }

        public MinecraftRuntimeInfo Resolve(
            MinecraftDirectoryInfo minecraftDirectoryInfo,
            LauncherVersion selectedVersion)
        {
            MinecraftRuntimeInfo runtimeInfo = new MinecraftRuntimeInfo
            {
                MinecraftVersion = selectedVersion.MinecraftVersion
            };

            try
            {
                LauncherStorageInfo storageInfo = _storageService.EnsureStorage();
                string versionId = string.IsNullOrWhiteSpace(selectedVersion.Id)
                    ? $"carbon-{selectedVersion.MinecraftVersion}"
                    : selectedVersion.Id;

                runtimeInfo.NativesDirectory = Path.Combine(storageInfo.RuntimeDirectory, "natives", versionId);
                Directory.CreateDirectory(runtimeInfo.NativesDirectory);

                if (!minecraftDirectoryInfo.IsValid ||
                    string.IsNullOrWhiteSpace(minecraftDirectoryInfo.DirectoryPath) ||
                    !Directory.Exists(minecraftDirectoryInfo.DirectoryPath))
                {
                    runtimeInfo.Errors.Add("Minecraft directory is missing or invalid.");
                    runtimeInfo.IsResolved = false;
                    return runtimeInfo;
                }

                string minecraftDirectory = minecraftDirectoryInfo.DirectoryPath;
                string minecraftVersion = string.IsNullOrWhiteSpace(selectedVersion.MinecraftVersion)
                    ? "1.8.9"
                    : selectedVersion.MinecraftVersion;

                runtimeInfo.MinecraftVersion = minecraftVersion;
                runtimeInfo.VersionDirectory = Path.Combine(minecraftDirectory, "versions", minecraftVersion);
                runtimeInfo.VersionJsonPath = Path.Combine(runtimeInfo.VersionDirectory, $"{minecraftVersion}.json");
                runtimeInfo.ClientJarPath = Path.Combine(runtimeInfo.VersionDirectory, $"{minecraftVersion}.jar");
                runtimeInfo.AssetsDirectory = Path.Combine(minecraftDirectory, "assets");
                runtimeInfo.LibrariesDirectory = Path.Combine(minecraftDirectory, "libraries");

                if (!File.Exists(runtimeInfo.VersionJsonPath))
                {
                    runtimeInfo.Errors.Add("Minecraft version json is missing.");
                }

                if (!File.Exists(runtimeInfo.ClientJarPath))
                {
                    runtimeInfo.Errors.Add("Minecraft vanilla client jar is missing.");
                }

                if (!Directory.Exists(runtimeInfo.LibrariesDirectory))
                {
                    runtimeInfo.Warnings.Add("Minecraft libraries directory is missing.");
                }

                if (!Directory.Exists(runtimeInfo.AssetsDirectory))
                {
                    runtimeInfo.Warnings.Add("Minecraft assets directory is missing.");
                }

                if (File.Exists(runtimeInfo.VersionJsonPath))
                {
                    ReadVersionJson(runtimeInfo);
                }

                if (string.IsNullOrWhiteSpace(runtimeInfo.AssetIndex))
                {
                    runtimeInfo.AssetIndex = minecraftVersion == "1.8.9" ? "1.8" : minecraftVersion;
                }

                runtimeInfo.AssetIndexPath = Path.Combine(
                    runtimeInfo.AssetsDirectory,
                    "indexes",
                    $"{runtimeInfo.AssetIndex}.json");

                if (!File.Exists(runtimeInfo.AssetIndexPath))
                {
                    runtimeInfo.Warnings.Add("Minecraft asset index is missing.");
                }

                if (runtimeInfo.MissingLibraries.Count > 0)
                {
                    runtimeInfo.Warnings.Add($"{runtimeInfo.MissingLibraries.Count} Minecraft library file(s) are missing.");
                }

                runtimeInfo.IsResolved = runtimeInfo.Errors.Count == 0;
                return runtimeInfo;
            }
            catch (Exception ex)
            {
                runtimeInfo.Errors.Add($"Minecraft runtime resolve failed: {ex.Message}");
                runtimeInfo.IsResolved = false;
                return runtimeInfo;
            }
        }

        private static void ReadVersionJson(MinecraftRuntimeInfo runtimeInfo)
        {
            try
            {
                string json = File.ReadAllText(runtimeInfo.VersionJsonPath);
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    if (root.TryGetProperty("mainClass", out JsonElement mainClassElement))
                    {
                        runtimeInfo.MainClass = mainClassElement.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("assetIndex", out JsonElement assetIndexElement) &&
                        assetIndexElement.TryGetProperty("id", out JsonElement assetIndexIdElement))
                    {
                        runtimeInfo.AssetIndex = assetIndexIdElement.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("libraries", out JsonElement librariesElement) &&
                        librariesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement libraryElement in librariesElement.EnumerateArray())
                        {
                            ReadLibrary(runtimeInfo, libraryElement);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                runtimeInfo.Errors.Add($"Minecraft version json could not be read: {ex.Message}");
            }
        }

        private static void ReadLibrary(MinecraftRuntimeInfo runtimeInfo, JsonElement libraryElement)
        {
            if (!libraryElement.TryGetProperty("name", out JsonElement nameElement))
            {
                return;
            }

            string libraryName = nameElement.GetString() ?? string.Empty;
            string libraryPath = ResolveMavenLibraryPath(runtimeInfo.LibrariesDirectory, libraryName);

            if (string.IsNullOrWhiteSpace(libraryPath))
            {
                return;
            }

            if (File.Exists(libraryPath))
            {
                runtimeInfo.LibraryPaths.Add(libraryPath);
                return;
            }

            runtimeInfo.MissingLibraries.Add(libraryName);
        }

        private static string ResolveMavenLibraryPath(string librariesDirectory, string libraryName)
        {
            string[] parts = libraryName.Split(':');
            if (parts.Length < 3)
            {
                return string.Empty;
            }

            string groupPath = parts[0].Replace('.', Path.DirectorySeparatorChar);
            string artifact = parts[1];
            string version = parts[2];
            string classifier = parts.Length >= 4 ? $"-{parts[3]}" : string.Empty;
            string fileName = $"{artifact}-{version}{classifier}.jar";

            return Path.Combine(librariesDirectory, groupPath, artifact, version, fileName);
        }
    }
}
