using System;
using System.IO;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LaunchWrapperResolverService
    {
        private const string LaunchWrapperVersion = "1.12";
        private const string LaunchWrapperFileName = "launchwrapper-1.12.jar";

        public LaunchWrapperInfo Resolve(CarbonTweakerInfo carbonTweakerInfo, MinecraftDirectoryInfo minecraftDirectoryInfo)
        {
            LaunchWrapperInfo info = new LaunchWrapperInfo
            {
                IsRequired = carbonTweakerInfo.HasTweakClass,
                Version = LaunchWrapperVersion
            };

            if (!info.IsRequired)
            {
                info.Status = "NotRequired";
                return info;
            }

            try
            {
                string minecraftDirectory = minecraftDirectoryInfo.DirectoryPath;
                info.ExpectedPath = Path.Combine(
                    minecraftDirectory,
                    "libraries",
                    "net",
                    "minecraft",
                    "launchwrapper",
                    LaunchWrapperVersion,
                    LaunchWrapperFileName);

                if (string.IsNullOrWhiteSpace(minecraftDirectory) || !Directory.Exists(minecraftDirectory))
                {
                    info.Status = "Missing";
                    info.ErrorMessage = "Minecraft directory is missing; LaunchWrapper path could not be resolved.";
                    return info;
                }

                if (File.Exists(info.ExpectedPath))
                {
                    info.IsFound = true;
                    info.Status = "Ready";
                    return info;
                }

                info.Status = "Missing";
                info.ErrorMessage = $"LaunchWrapper is missing at {info.ExpectedPath}.";
            }
            catch (Exception ex)
            {
                info.Status = "Error";
                info.ErrorMessage = $"LaunchWrapper check failed: {ex.Message}";
            }

            return info;
        }
    }
}
