using System;
using System.IO;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class StartupLogService
    {
        private readonly LauncherStorageService _storageService;

        public StartupLogService(LauncherStorageService storageService)
        {
            _storageService = storageService;
        }

        public string LogFilePath
        {
            get
            {
                LauncherStorageInfo storageInfo = _storageService.GetStorageInfo();
                return Path.Combine(storageInfo.LogsDirectory, "startup.log");
            }
        }

        public void Write(string message)
        {
            try
            {
                LauncherStorageInfo storageInfo = _storageService.EnsureStorage();
                Directory.CreateDirectory(storageInfo.LogsDirectory);
                File.AppendAllText(
                    LogFilePath,
                    $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
