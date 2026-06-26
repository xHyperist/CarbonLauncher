using System;
using System.Collections.Generic;
using System.IO;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class MinecraftDirectoryService
    {
        public MinecraftDirectoryInfo Detect(string configuredDirectory)
        {
            string firstError = string.Empty;

            foreach (MinecraftDirectoryCandidate candidate in GetCandidates(configuredDirectory))
            {
                MinecraftDirectoryInfo directoryInfo = ValidateDirectory(candidate.Path, candidate.Source);
                if (directoryInfo.IsValid)
                {
                    return directoryInfo;
                }

                if (string.IsNullOrWhiteSpace(firstError) && candidate.Source == "Config")
                {
                    firstError = directoryInfo.ErrorMessage;
                }
            }

            return new MinecraftDirectoryInfo
            {
                IsDetected = false,
                IsValid = false,
                Source = string.IsNullOrWhiteSpace(firstError) ? "Not Found" : "Config",
                ErrorMessage = string.IsNullOrWhiteSpace(firstError)
                    ? "Minecraft directory could not be detected."
                    : firstError
            };
        }

        public MinecraftDirectoryInfo ValidateDirectory(string directoryPath, string source)
        {
            string normalizedPath = NormalizeDirectoryPath(directoryPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return CreateInvalid(source, "Minecraft directory is empty.");
            }

            if (!Directory.Exists(normalizedPath))
            {
                return CreateInvalid(source, "Minecraft directory does not exist.");
            }

            bool hasVersionsFolder = Directory.Exists(Path.Combine(normalizedPath, "versions"));
            bool hasAssetsFolder = Directory.Exists(Path.Combine(normalizedPath, "assets"));
            bool hasLibrariesFolder = Directory.Exists(Path.Combine(normalizedPath, "libraries"));
            bool isValid = hasVersionsFolder || hasAssetsFolder || hasLibrariesFolder;

            return new MinecraftDirectoryInfo
            {
                IsDetected = isValid,
                IsValid = isValid,
                DirectoryPath = normalizedPath,
                Source = source,
                ErrorMessage = isValid
                    ? string.Empty
                    : "Directory must contain versions, assets, or libraries.",
                HasVersionsFolder = hasVersionsFolder,
                HasAssetsFolder = hasAssetsFolder,
                HasLibrariesFolder = hasLibrariesFolder
            };
        }

        private IEnumerable<MinecraftDirectoryCandidate> GetCandidates(string configuredDirectory)
        {
            if (!string.IsNullOrWhiteSpace(configuredDirectory))
            {
                yield return new MinecraftDirectoryCandidate(configuredDirectory, "Config");
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(appDataPath))
            {
                yield return new MinecraftDirectoryCandidate(Path.Combine(appDataPath, ".minecraft"), "APPDATA");
            }

            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfilePath))
            {
                yield return new MinecraftDirectoryCandidate(
                    Path.Combine(userProfilePath, "AppData", "Roaming", ".minecraft"),
                    "USERPROFILE");
            }
        }

        private static string NormalizeDirectoryPath(string directoryPath)
        {
            return string.IsNullOrWhiteSpace(directoryPath)
                ? string.Empty
                : directoryPath.Trim().Trim('"');
        }

        private static MinecraftDirectoryInfo CreateInvalid(string source, string message)
        {
            return new MinecraftDirectoryInfo
            {
                IsDetected = false,
                IsValid = false,
                Source = source,
                ErrorMessage = message
            };
        }

        private readonly struct MinecraftDirectoryCandidate
        {
            public MinecraftDirectoryCandidate(string path, string source)
            {
                Path = path;
                Source = source;
            }

            public string Path { get; }

            public string Source { get; }
        }
    }
}
