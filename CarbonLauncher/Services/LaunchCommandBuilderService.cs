using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LaunchCommandBuilderService
    {
        private const string DefaultMainClass = "net.minecraft.launchwrapper.Launch";

        public LaunchCommand Build(
            LaunchProfile profile,
            JavaInfo javaInfo,
            MinecraftDirectoryInfo minecraftDirectoryInfo,
            ClientJarInfo clientJarInfo,
            LauncherVersion selectedVersion,
            LaunchSession launchSession)
        {
            LaunchCommand command = new LaunchCommand
            {
                JavaExecutablePath = profile.JavaPath,
                WorkingDirectory = string.IsNullOrWhiteSpace(profile.GameDirectory)
                    ? profile.MinecraftDirectory
                    : profile.GameDirectory,
                MainClass = DefaultMainClass,
                JvmArguments = new List<string>(profile.JvmArguments),
                ClasspathEntries = BuildClasspath(clientJarInfo),
                GameArguments = BuildGameArguments(profile, launchSession)
            };

            Validate(command, profile, javaInfo, minecraftDirectoryInfo, clientJarInfo, selectedVersion, launchSession);
            command.IsBuildable = command.Errors.Count == 0;
            command.FullCommandPreview = BuildPreview(command);
            return command;
        }

        private static List<string> BuildClasspath(ClientJarInfo clientJarInfo)
        {
            List<string> classpathEntries = new List<string>();

            if (!string.IsNullOrWhiteSpace(clientJarInfo.JarPath))
            {
                classpathEntries.Add(clientJarInfo.JarPath);
            }

            return classpathEntries;
        }

        private static List<string> BuildGameArguments(LaunchProfile profile, LaunchSession launchSession)
        {
            string assetsDirectory = string.IsNullOrWhiteSpace(profile.MinecraftDirectory)
                ? string.Empty
                : Path.Combine(profile.MinecraftDirectory, "assets");

            return new List<string>
            {
                "--username",
                launchSession.Username,
                "--version",
                profile.MinecraftVersion,
                "--gameDir",
                profile.MinecraftDirectory,
                "--assetsDir",
                assetsDirectory,
                "--assetIndex",
                "1.8",
                "--uuid",
                launchSession.Uuid,
                "--accessToken",
                launchSession.AccessToken,
                "--userType",
                launchSession.UserType
            };
        }

        private static void Validate(
            LaunchCommand command,
            LaunchProfile profile,
            JavaInfo javaInfo,
            MinecraftDirectoryInfo minecraftDirectoryInfo,
            ClientJarInfo clientJarInfo,
            LauncherVersion selectedVersion,
            LaunchSession launchSession)
        {
            if (string.IsNullOrWhiteSpace(command.JavaExecutablePath) ||
                !File.Exists(command.JavaExecutablePath))
            {
                command.Errors.Add("Java path is missing or invalid.");
            }

            if (string.IsNullOrWhiteSpace(profile.MinecraftDirectory) ||
                !Directory.Exists(profile.MinecraftDirectory) ||
                !minecraftDirectoryInfo.IsValid)
            {
                command.Errors.Add("Minecraft directory is missing or invalid.");
            }

            if (!selectedVersion.IsAvailable || selectedVersion.IsComingSoon)
            {
                command.Errors.Add("Selected version is not available.");
            }

            if (clientJarInfo.Status == "Missing" || !clientJarInfo.Exists)
            {
                command.Errors.Add("Client jar is missing.");
            }
            else if (clientJarInfo.Exists && clientJarInfo.FileSizeBytes == 0)
            {
                command.Errors.Add("Client jar file is empty.");
            }
            else if (clientJarInfo.Status == "Error")
            {
                command.Errors.Add(string.IsNullOrWhiteSpace(clientJarInfo.ErrorMessage)
                    ? "Client jar check failed."
                    : clientJarInfo.ErrorMessage);
            }

            if (!string.IsNullOrWhiteSpace(launchSession.ErrorMessage))
            {
                command.Errors.Add(launchSession.ErrorMessage);
            }
            else
            {
                string usernameError = ValidateOfflineIgn(launchSession.Username);
                if (!string.IsNullOrWhiteSpace(usernameError))
                {
                    command.Errors.Add(usernameError);
                }
            }

            if (string.IsNullOrWhiteSpace(launchSession.Uuid))
            {
                command.Errors.Add("Launch session UUID is missing.");
            }

            if (string.IsNullOrWhiteSpace(command.MainClass))
            {
                command.Errors.Add("Main class is missing.");
            }

            if (javaInfo.IsDetected && javaInfo.MajorVersion == 0)
            {
                command.Warnings.Add("Java major version could not be parsed.");
            }

            command.Warnings.Add("Minecraft libraries are not fully resolved yet.");
            command.Warnings.Add("Assets index is not fully resolved yet.");
            command.Warnings.Add("Offline launch session is not the same as a real authenticated session.");
            command.Warnings.Add("This command is preview only; the game process is not started.");
        }

        private static string BuildPreview(LaunchCommand command)
        {
            List<string> parts = new List<string>
            {
                Quote(command.JavaExecutablePath)
            };

            parts.AddRange(command.JvmArguments);

            if (command.ClasspathEntries.Count > 0)
            {
                parts.Add("-cp");
                parts.Add(Quote(string.Join(";", command.ClasspathEntries)));
            }

            parts.Add(command.MainClass);

            foreach (string argument in command.GameArguments)
            {
                parts.Add(Quote(argument));
            }

            return string.Join(" ", parts);
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "\"\"";
            }

            return value.Contains(" ") || value.Contains("\\")
                ? $"\"{value}\""
                : value;
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
