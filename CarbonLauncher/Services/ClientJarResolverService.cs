using System;
using System.IO;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class ClientJarResolverService
    {
        private readonly LauncherStorageService _storageService;
        private readonly LauncherStorageInfo _storageInfo;

        public ClientJarResolverService()
            : this(new LauncherStorageService())
        {
        }

        public ClientJarResolverService(LauncherStorageService storageService)
        {
            _storageService = storageService;
            _storageInfo = _storageService.GetStorageInfo();
        }

        public ClientJarInfo Resolve(LauncherVersion selectedVersion)
        {
            ClientJarInfo info = new ClientJarInfo();

            if (selectedVersion == null)
            {
                info.Status = "NotAvailable";
                info.ErrorMessage = "Selected version is not available.";
                return info;
            }

            string versionId = string.IsNullOrWhiteSpace(selectedVersion.Id)
                ? $"carbon-{selectedVersion.MinecraftVersion}"
                : selectedVersion.Id;

            info.VersionId = versionId;

            if (!selectedVersion.IsAvailable || selectedVersion.IsComingSoon)
            {
                info.Status = "NotAvailable";
                info.ErrorMessage = "Selected version is not available.";
                return info;
            }

            try
            {
                _storageService.EnsureStorage();
                string versionDirectory = Path.Combine(_storageInfo.ClientDirectory, versionId);
                Directory.CreateDirectory(versionDirectory);

                string expectedFileName = GetExpectedFileName(selectedVersion);
                string jarPath = GetJarPath(versionDirectory, selectedVersion.LocalJarPath, expectedFileName);
                string? jarDirectory = Path.GetDirectoryName(jarPath);

                if (!string.IsNullOrWhiteSpace(jarDirectory))
                {
                    Directory.CreateDirectory(jarDirectory);
                }

                info.ExpectedFileName = Path.GetFileName(jarPath);
                info.JarPath = jarPath;

                FileInfo fileInfo = new FileInfo(jarPath);
                if (!fileInfo.Exists)
                {
                    info.Status = "Missing";
                    info.ErrorMessage = "Client jar is missing.";
                    return info;
                }

                info.Exists = true;
                info.Status = "Ready";
                info.FileSizeBytes = fileInfo.Length;
                info.LastModifiedAt = fileInfo.LastWriteTime;
                return info;
            }
            catch (Exception ex)
            {
                info.Status = "Error";
                info.ErrorMessage = $"Client jar check failed: {ex.Message}";
                return info;
            }
        }

        private static string GetExpectedFileName(LauncherVersion selectedVersion)
        {
            if (!string.IsNullOrWhiteSpace(selectedVersion.LocalJarPath))
            {
                return Path.GetFileName(selectedVersion.LocalJarPath);
            }

            return $"carbon-client-{selectedVersion.MinecraftVersion}.jar";
        }

        private static string GetJarPath(string versionDirectory, string localJarPath, string expectedFileName)
        {
            if (string.IsNullOrWhiteSpace(localJarPath))
            {
                return Path.Combine(versionDirectory, expectedFileName);
            }

            return Path.IsPathRooted(localJarPath)
                ? localJarPath
                : Path.Combine(versionDirectory, localJarPath);
        }
    }
}
