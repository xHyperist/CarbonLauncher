using System;
using System.IO;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LauncherStorageService
    {
        public LauncherStorageInfo GetStorageInfo()
        {
            string rootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CarbonLauncher");

            return new LauncherStorageInfo
            {
                RootDirectory = rootDirectory,
                ConfigFilePath = Path.Combine(rootDirectory, "config.json"),
                VersionManifestPath = Path.Combine(rootDirectory, "versions.json"),
                ClientDirectory = Path.Combine(rootDirectory, "client"),
                LogsDirectory = Path.Combine(rootDirectory, "logs"),
                CacheDirectory = Path.Combine(rootDirectory, "cache"),
                RuntimeDirectory = Path.Combine(rootDirectory, "runtime"),
                TempDirectory = Path.Combine(rootDirectory, "temp"),
                CrashReportsDirectory = Path.Combine(rootDirectory, "crash-reports")
            };
        }

        public LauncherStorageInfo EnsureStorage()
        {
            LauncherStorageInfo storageInfo = GetStorageInfo();

            try
            {
                Directory.CreateDirectory(storageInfo.RootDirectory);
                Directory.CreateDirectory(storageInfo.ClientDirectory);
                Directory.CreateDirectory(storageInfo.LogsDirectory);
                Directory.CreateDirectory(storageInfo.CacheDirectory);
                Directory.CreateDirectory(storageInfo.RuntimeDirectory);
                Directory.CreateDirectory(storageInfo.TempDirectory);
                Directory.CreateDirectory(storageInfo.CrashReportsDirectory);

                storageInfo.IsReady = true;
                return storageInfo;
            }
            catch (Exception ex)
            {
                storageInfo.IsReady = false;
                storageInfo.ErrorMessage = $"Storage setup failed: {ex.Message}";
                return storageInfo;
            }
        }

        public void CleanTempDirectory()
        {
            LauncherStorageInfo storageInfo = GetStorageInfo();

            try
            {
                Directory.CreateDirectory(storageInfo.TempDirectory);

                foreach (string filePath in Directory.GetFiles(storageInfo.TempDirectory))
                {
                    TryDeleteFile(filePath);
                }

                foreach (string directoryPath in Directory.GetDirectories(storageInfo.TempDirectory))
                {
                    TryDeleteDirectory(directoryPath);
                }
            }
            catch
            {
                // Temp cleanup should never block launcher startup.
            }
        }

        private static void TryDeleteFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch
            {
            }
        }
    }
}
