using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class CarbonTweakerManifestService
    {
        private const string ManifestPath = "META-INF/MANIFEST.MF";

        public CarbonTweakerInfo Read(ClientJarInfo clientJarInfo)
        {
            CarbonTweakerInfo info = new CarbonTweakerInfo
            {
                JarPath = clientJarInfo.JarPath
            };

            if (string.IsNullOrWhiteSpace(clientJarInfo.JarPath) ||
                !File.Exists(clientJarInfo.JarPath))
            {
                info.Status = "MissingJar";
                info.ErrorMessage = "Carbon client jar was not found.";
                return info;
            }

            info.IsJarFound = true;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(clientJarInfo.JarPath))
                {
                    ZipArchiveEntry? manifestEntry = archive.GetEntry(ManifestPath);
                    if (manifestEntry == null)
                    {
                        info.Status = "MissingTweakClass";
                        info.ErrorMessage = "Jar manifest was not found.";
                        return info;
                    }

                    Dictionary<string, string> attributes = ReadManifestAttributes(manifestEntry);
                    info.TweakClass = GetAttribute(attributes, "TweakClass");
                    info.LaunchMode = GetAttribute(attributes, "Carbon-Launch-Mode");
                }

                info.HasTweakClass = !string.IsNullOrWhiteSpace(info.TweakClass);
                info.Status = info.HasTweakClass ? "Ready" : "MissingTweakClass";

                if (!info.HasTweakClass)
                {
                    info.ErrorMessage = "Carbon TweakClass was not found.";
                }
            }
            catch (Exception ex)
            {
                info.Status = "Error";
                info.ErrorMessage = $"Carbon tweaker manifest could not be read: {ex.Message}";
            }

            return info;
        }

        private static Dictionary<string, string> ReadManifestAttributes(ZipArchiveEntry manifestEntry)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (Stream stream = manifestEntry.Open())
            using (StreamReader reader = new StreamReader(stream))
            {
                string currentKey = string.Empty;
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    if (line.StartsWith(" ") && !string.IsNullOrWhiteSpace(currentKey))
                    {
                        attributes[currentKey] = attributes[currentKey] + line.Substring(1);
                        continue;
                    }

                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex <= 0)
                    {
                        currentKey = string.Empty;
                        continue;
                    }

                    currentKey = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    attributes[currentKey] = value;
                }
            }

            return attributes;
        }

        private static string GetAttribute(Dictionary<string, string> attributes, string key)
        {
            return attributes.TryGetValue(key, out string? value) ? value : string.Empty;
        }
    }
}
