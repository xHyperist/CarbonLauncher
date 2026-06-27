using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LaunchProcessService
    {
        private readonly LauncherStorageService _storageService;

        public event Action<LaunchProcessInfo>? ProcessExited;

        public LaunchProcessService()
            : this(new LauncherStorageService())
        {
        }

        public LaunchProcessService(LauncherStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<LaunchProcessInfo> StartAsync(LaunchCommand command, LaunchProfile profile)
        {
            LaunchProcessInfo info = CreateInitialInfo();

            if (!command.IsBuildable)
            {
                info.Status = "Error";
                info.ErrorMessage = command.Errors.Count == 0
                    ? "Launch command is not buildable."
                    : string.Join(Environment.NewLine, command.Errors);
                return info;
            }

            if (string.IsNullOrWhiteSpace(command.JavaExecutablePath) ||
                !File.Exists(command.JavaExecutablePath))
            {
                info.Status = "Error";
                info.ErrorMessage = "Java executable path is missing or invalid.";
                return info;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(command.WorkingDirectory))
                {
                    info.Status = "Error";
                    info.ErrorMessage = "Working directory is missing.";
                    return info;
                }

                Directory.CreateDirectory(command.WorkingDirectory);
                WriteLaunchHeader(info.LogFilePath, command, profile);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = command.JavaExecutablePath,
                    Arguments = BuildArguments(command),
                    WorkingDirectory = command.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                Process process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (_, e) => AppendLogLine(info.LogFilePath, e.Data);
                process.ErrorDataReceived += (_, e) => AppendLogLine(info.LogFilePath, e.Data);

                bool started = process.Start();
                if (!started)
                {
                    info.Status = "Error";
                    info.ErrorMessage = "Minecraft process could not be started.";
                    return info;
                }

                info.HasStarted = true;
                info.IsRunning = true;
                info.Status = "Running";
                info.ProcessId = process.Id;
                info.StartedAt = DateTime.Now;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                _ = TrackExitAsync(process, info);

                await Task.CompletedTask;
                return info;
            }
            catch (Exception ex)
            {
                AppendLogLine(info.LogFilePath, $"Launch failed: {ex}");
                info.Status = "Error";
                info.ErrorMessage = $"Launch failed: {ex.Message}";
                info.IsRunning = false;
                return info;
            }
        }

        private LaunchProcessInfo CreateInitialInfo()
        {
            LauncherStorageInfo storageInfo = _storageService.EnsureStorage();
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string logFilePath = Path.Combine(storageInfo.LogsDirectory, $"launch-{timestamp}.log");

            return new LaunchProcessInfo
            {
                Status = "Starting",
                LogFilePath = logFilePath
            };
        }

        private static string BuildArguments(LaunchCommand command)
        {
            List<string> arguments = new List<string>();
            arguments.AddRange(command.JvmArguments);

            if (command.ClasspathEntries.Count > 0)
            {
                arguments.Add("-cp");
                arguments.Add(string.Join(";", command.ClasspathEntries));
            }

            arguments.Add(command.MainClass);
            arguments.AddRange(command.GameArguments);

            return JoinArguments(arguments);
        }

        private static string JoinArguments(IEnumerable<string> arguments)
        {
            List<string> escapedArguments = new List<string>();

            foreach (string argument in arguments)
            {
                escapedArguments.Add(EscapeArgument(argument));
            }

            return string.Join(" ", escapedArguments);
        }

        private static string EscapeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            bool needsQuotes = argument.IndexOfAny(new[] { ' ', '\t', '\n', '\r', '"' }) >= 0;
            if (!needsQuotes)
            {
                return argument;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('"');

            foreach (char character in argument)
            {
                if (character == '"' || character == '\\')
                {
                    builder.Append('\\');
                }

                builder.Append(character);
            }

            builder.Append('"');
            return builder.ToString();
        }

        private static void WriteLaunchHeader(string logFilePath, LaunchCommand command, LaunchProfile profile)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath) ?? string.Empty);
                File.AppendAllText(
                    logFilePath,
                    $"StartedAt: {DateTime.Now:O}{Environment.NewLine}" +
                    $"Java path: {command.JavaExecutablePath}{Environment.NewLine}" +
                    $"Working directory: {command.WorkingDirectory}{Environment.NewLine}" +
                    $"MainClass: {command.MainClass}{Environment.NewLine}" +
                    $"NativesDirectory: {GetNativeDirectory(command)}{Environment.NewLine}" +
                    $"Username: {profile.Username}{Environment.NewLine}" +
                    $"Version: {profile.MinecraftVersion}{Environment.NewLine}" +
                    $"Full command preview: {command.FullCommandPreview}{Environment.NewLine}" +
                    $"---- output ----{Environment.NewLine}");
            }
            catch
            {
            }
        }

        private static string GetNativeDirectory(LaunchCommand command)
        {
            foreach (string argument in command.JvmArguments)
            {
                const string javaLibraryPathPrefix = "-Djava.library.path=";
                if (argument.StartsWith(javaLibraryPathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return argument.Substring(javaLibraryPathPrefix.Length);
                }
            }

            return string.Empty;
        }

        private static void AppendLogLine(string logFilePath, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            try
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
            catch
            {
            }
        }

        private async Task TrackExitAsync(Process process, LaunchProcessInfo info)
        {
            try
            {
                await Task.Run(() => process.WaitForExit());
                info.IsRunning = false;
                info.HasExited = true;
                info.ExitCode = process.ExitCode;
                info.ExitedAt = DateTime.Now;
                info.Status = "Exited";
                AppendLogLine(info.LogFilePath, $"Process exited with code {process.ExitCode} at {info.ExitedAt:O}");
                ProcessExited?.Invoke(info);
                process.Dispose();
            }
            catch (Exception ex)
            {
                info.IsRunning = false;
                info.Status = "Error";
                info.ErrorMessage = $"Process tracking failed: {ex.Message}";
                AppendLogLine(info.LogFilePath, info.ErrorMessage);
                ProcessExited?.Invoke(info);
            }
        }
    }
}
