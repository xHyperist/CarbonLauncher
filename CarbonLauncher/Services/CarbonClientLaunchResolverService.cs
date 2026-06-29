using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class CarbonClientLaunchResolverService
    {
        private const int MaxEntriesToScan = 5000;
        private const int MaxDetectedEntries = 50;

        public CarbonClientLaunchInfo Resolve(ClientJarInfo clientJarInfo)
        {
            CarbonClientLaunchInfo launchInfo = new CarbonClientLaunchInfo
            {
                JarPath = clientJarInfo.JarPath
            };

            if (string.IsNullOrWhiteSpace(clientJarInfo.JarPath) || !File.Exists(clientJarInfo.JarPath))
            {
                launchInfo.IsJarFound = false;
                launchInfo.DetectedLaunchMode = "Unknown";
                launchInfo.Errors.Add("Carbon client jar is missing.");
                return launchInfo;
            }

            launchInfo.IsJarFound = true;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(clientJarInfo.JarPath))
                {
                    ReadManifest(archive, launchInfo);
                    ScanEntries(archive, launchInfo);
                }

                ApplyMode(launchInfo);
                return launchInfo;
            }
            catch (Exception ex)
            {
                launchInfo.DetectedLaunchMode = "Unknown";
                launchInfo.Errors.Add($"Carbon client jar could not be analyzed: {ex.Message}");
                return launchInfo;
            }
        }

        private static void ReadManifest(ZipArchive archive, CarbonClientLaunchInfo launchInfo)
        {
            ZipArchiveEntry? manifestEntry = archive.GetEntry("META-INF/MANIFEST.MF");
            if (manifestEntry == null)
            {
                launchInfo.Warnings.Add("Carbon jar manifest was not found.");
                return;
            }

            using (Stream stream = manifestEntry.Open())
            using (StreamReader reader = new StreamReader(stream))
            {
                Dictionary<string, string> manifest = ParseManifest(reader.ReadToEnd());
                launchInfo.ManifestMainClass = GetManifestValue(manifest, "Main-Class");
                launchInfo.ManifestTweakClass = GetManifestValue(manifest, "TweakClass");
                launchInfo.FmlCorePlugin = GetManifestValue(manifest, "FMLCorePlugin");

                string containsFmlMod = GetManifestValue(manifest, "FMLCorePluginContainsFMLMod");
                if (!string.IsNullOrWhiteSpace(containsFmlMod))
                {
                    launchInfo.HasForgeModMetadata = true;
                    launchInfo.DetectedEntries.Add($"FMLCorePluginContainsFMLMod: {containsFmlMod}");
                }
            }
        }

        private static Dictionary<string, string> ParseManifest(string text)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? currentKey = null;

            using (StringReader reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(" ") && currentKey != null)
                    {
                        values[currentKey] += line.Substring(1);
                        continue;
                    }

                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex <= 0)
                    {
                        currentKey = null;
                        continue;
                    }

                    currentKey = line.Substring(0, separatorIndex).Trim();
                    values[currentKey] = line.Substring(separatorIndex + 1).Trim();
                }
            }

            return values;
        }

        private static string GetManifestValue(Dictionary<string, string> manifest, string key)
        {
            return manifest.TryGetValue(key, out string? value) ? value : string.Empty;
        }

        private static void ScanEntries(ZipArchive archive, CarbonClientLaunchInfo launchInfo)
        {
            int scannedEntries = 0;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                scannedEntries++;
                if (scannedEntries > MaxEntriesToScan)
                {
                    launchInfo.Warnings.Add("Carbon jar scan limit reached; diagnostics may be incomplete.");
                    return;
                }

                string entryName = entry.FullName.Replace('\\', '/');

                if (entryName.Equals("mcmod.info", StringComparison.OrdinalIgnoreCase))
                {
                    launchInfo.HasMcmodInfo = true;
                    launchInfo.HasForgeModMetadata = true;
                    AddDetectedEntry(launchInfo, "mcmod.info");
                    continue;
                }

                if (!entryName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string simpleName = Path.GetFileNameWithoutExtension(entryName);
                if (ContainsLaunchHint(simpleName))
                {
                    AddDetectedEntry(launchInfo, entryName);
                }
            }
        }

        private static void AddDetectedEntry(CarbonClientLaunchInfo launchInfo, string entry)
        {
            if (launchInfo.DetectedEntries.Count < MaxDetectedEntries)
            {
                launchInfo.DetectedEntries.Add(entry);
            }
        }

        private static bool ContainsLaunchHint(string simpleName)
        {
            return simpleName.IndexOf("Tweaker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   simpleName.IndexOf("Tweak", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   simpleName.IndexOf("CorePlugin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   simpleName.IndexOf("LoadingPlugin", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   simpleName.IndexOf("Launch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   simpleName.Equals("Main", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyMode(CarbonClientLaunchInfo launchInfo)
        {
            if (!string.IsNullOrWhiteSpace(launchInfo.ManifestTweakClass))
            {
                launchInfo.DetectedLaunchMode = "LaunchWrapperTweak";
                launchInfo.IsCustomLaunchSupported = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(launchInfo.FmlCorePlugin))
            {
                launchInfo.DetectedLaunchMode = "FMLCorePlugin";
                launchInfo.IsCustomLaunchSupported = true;
                launchInfo.HasForgeModMetadata = true;
                return;
            }

            if (!string.IsNullOrWhiteSpace(launchInfo.ManifestMainClass))
            {
                launchInfo.DetectedLaunchMode = "ManifestMainClass";
                launchInfo.Warnings.Add("Manifest Main-Class was detected, but automatic main class override is disabled.");
                return;
            }

            if (launchInfo.HasForgeModMetadata)
            {
                launchInfo.DetectedLaunchMode = "ForgeModMetadataOnly";
                launchInfo.Warnings.Add("Carbon jar looks like a Forge mod jar, but no custom launch entrypoint was detected.");
                return;
            }

            launchInfo.DetectedLaunchMode = "VanillaClasspathOnly";
            launchInfo.Warnings.Add("Carbon client jar is on classpath, but no custom launch entrypoint was detected.");
        }
    }
}
