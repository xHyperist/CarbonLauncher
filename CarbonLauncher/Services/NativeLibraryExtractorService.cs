using System;
using System.IO;
using System.IO.Compression;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class NativeLibraryExtractorService
    {
        public MinecraftRuntimeInfo PrepareNatives(MinecraftRuntimeInfo runtimeInfo)
        {
            bool extractionFailed = false;

            try
            {
                Directory.CreateDirectory(runtimeInfo.NativesDirectory);

                if (runtimeInfo.NativeLibraryJarPaths.Count == 0)
                {
                    runtimeInfo.Errors.Add("No Windows native library jars were found.");
                    runtimeInfo.AreNativesPrepared = false;
                    return runtimeInfo;
                }

                foreach (string missingNativeJarPath in runtimeInfo.MissingNativeLibraryJarPaths)
                {
                    runtimeInfo.Errors.Add($"Native library jar is missing: {missingNativeJarPath}");
                }

                foreach (string nativeJarPath in runtimeInfo.NativeLibraryJarPaths)
                {
                    extractionFailed |= !ExtractNativeJar(runtimeInfo, nativeJarPath);
                }

                runtimeInfo.AreNativesPrepared =
                    !extractionFailed &&
                    runtimeInfo.MissingNativeLibraryJarPaths.Count == 0 &&
                    runtimeInfo.ExtractedNativeFiles.Count > 0;

                if (!runtimeInfo.AreNativesPrepared)
                {
                    runtimeInfo.Errors.Add("Native libraries could not be prepared.");
                }

                return runtimeInfo;
            }
            catch (Exception ex)
            {
                runtimeInfo.Errors.Add($"Native extraction failed: {ex.Message}");
                runtimeInfo.AreNativesPrepared = false;
                return runtimeInfo;
            }
        }

        private static bool ExtractNativeJar(MinecraftRuntimeInfo runtimeInfo, string nativeJarPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(nativeJarPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!IsDllEntry(entry))
                        {
                            continue;
                        }

                        string destinationPath = Path.Combine(runtimeInfo.NativesDirectory, Path.GetFileName(entry.FullName));
                        entry.ExtractToFile(destinationPath, true);
                        runtimeInfo.ExtractedNativeFiles.Add(destinationPath);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                runtimeInfo.Errors.Add($"Native jar could not be extracted: {nativeJarPath} ({ex.Message})");
                return false;
            }
        }

        private static bool IsDllEntry(ZipArchiveEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                return false;
            }

            if (entry.FullName.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        }
    }
}
