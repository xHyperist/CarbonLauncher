using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LaunchProfileService
    {
        public LaunchProfile CreateDefaultProfile(
            LauncherConfig config,
            JavaInfo javaInfo,
            MinecraftDirectoryInfo minecraftDirectoryInfo,
            LauncherVersion selectedVersion,
            ClientJarInfo clientJarInfo)
        {
            string minecraftVersion = string.IsNullOrWhiteSpace(selectedVersion.MinecraftVersion)
                ? "1.8.9"
                : selectedVersion.MinecraftVersion;
            string username = NormalizeOfflineIgn(config.GuestUsername);
            int allocatedMemoryMb = config.AllocatedMemoryMb;

            return new LaunchProfile
            {
                ProfileName = string.IsNullOrWhiteSpace(selectedVersion.DisplayName)
                    ? $"Carbon {minecraftVersion}"
                    : selectedVersion.DisplayName,
                MinecraftVersion = minecraftVersion,
                LoaderType = string.IsNullOrWhiteSpace(selectedVersion.LoaderType)
                    ? "Forge"
                    : selectedVersion.LoaderType,
                JavaPath = javaInfo.IsDetected ? javaInfo.JavaPath : config.JavaPath,
                MinecraftDirectory = minecraftDirectoryInfo.IsValid
                    ? minecraftDirectoryInfo.DirectoryPath
                    : config.MinecraftDirectory,
                GameDirectory = string.Empty,
                ClientJarPath = clientJarInfo.JarPath,
                Username = username,
                AllocatedMemoryMb = allocatedMemoryMb,
                JvmArguments = CreateDefaultJvmArguments(allocatedMemoryMb),
                GameArguments = new List<string>(),
                IsGuestMode = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public LaunchValidationResult Validate(
            LaunchProfile profile,
            JavaInfo javaInfo,
            MinecraftDirectoryInfo minecraftDirectoryInfo,
            LauncherVersion selectedVersion,
            ClientJarInfo clientJarInfo)
        {
            LaunchValidationResult result = new LaunchValidationResult();

            if (string.IsNullOrWhiteSpace(profile.JavaPath) || !File.Exists(profile.JavaPath))
            {
                result.Errors.Add("Java path is missing or invalid.");
            }

            if (string.IsNullOrWhiteSpace(profile.MinecraftDirectory) ||
                !Directory.Exists(profile.MinecraftDirectory))
            {
                result.Errors.Add("Minecraft directory is missing or invalid.");
            }

            if (!minecraftDirectoryInfo.IsValid)
            {
                result.Errors.Add("Minecraft directory does not contain versions, assets, or libraries.");
            }

            string usernameError = ValidateOfflineIgn(profile.Username);
            if (!string.IsNullOrWhiteSpace(usernameError))
            {
                result.Errors.Add(usernameError);
            }

            if (profile.AllocatedMemoryMb < 512)
            {
                result.Errors.Add("Allocated memory must be at least 512 MB.");
            }

            if (string.IsNullOrWhiteSpace(profile.MinecraftVersion))
            {
                result.Errors.Add("Selected version is required.");
            }

            if (!selectedVersion.IsAvailable || selectedVersion.IsComingSoon)
            {
                result.Errors.Add("Selected version is not available.");
            }
            else if (clientJarInfo.Status == "Missing" || !clientJarInfo.Exists)
            {
                result.Errors.Add("Client jar is missing.");
            }
            else if (clientJarInfo.Status == "Error")
            {
                result.Errors.Add(string.IsNullOrWhiteSpace(clientJarInfo.ErrorMessage)
                    ? "Client jar check failed."
                    : clientJarInfo.ErrorMessage);
            }
            else if (clientJarInfo.Exists && clientJarInfo.FileSizeBytes == 0)
            {
                result.Errors.Add("Client jar file is empty.");
            }

            if (profile.AllocatedMemoryMb < 2048)
            {
                result.Warnings.Add("Allocated memory is below 2048 MB.");
            }

            if (javaInfo.IsDetected && javaInfo.MajorVersion == 0)
            {
                result.Warnings.Add("Java major version could not be parsed.");
            }

            if (string.IsNullOrWhiteSpace(profile.GameDirectory))
            {
                result.Warnings.Add("Game directory is empty; Minecraft directory will be used.");
            }

            result.IsValid = result.Errors.Count == 0;
            result.Summary = result.IsValid
                ? "Ready"
                : $"{result.Errors.Count} issue(s) need attention.";

            return result;
        }

        private static List<string> CreateDefaultJvmArguments(int allocatedMemoryMb)
        {
            return new List<string>
            {
                $"-Xmx{allocatedMemoryMb}M",
                "-Xms512M",
                "-Djava.net.preferIPv4Stack=true"
            };
        }

        private static string NormalizeOfflineIgn(string username)
        {
            return username == "Guest"
                ? "CarbonPlayer"
                : username.Trim();
        }

        private static string ValidateOfflineIgn(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Player IGN is required.";
            }

            if (username.Length < 3 || username.Length > 16)
            {
                return "Player IGN must be 3-16 characters.";
            }

            return Regex.IsMatch(username, "^[A-Za-z0-9_]+$")
                ? string.Empty
                : "Player IGN can only contain A-Z, a-z, 0-9, and underscore. Turkish characters, spaces and special characters are not allowed.";
        }
    }
}
