using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class JavaDetectionService
    {
        private const int VersionTimeoutMs = 3500;

        public JavaInfo Detect(string configuredJavaPath)
        {
            foreach (JavaCandidate candidate in GetCandidates(configuredJavaPath))
            {
                JavaInfo info = ValidateJavaPath(candidate.Path, candidate.Source);
                if (info.IsDetected)
                {
                    return info;
                }
            }

            return new JavaInfo
            {
                IsDetected = false,
                Source = "Not Found",
                ErrorMessage = "Java was not found on this system."
            };
        }

        public JavaInfo ValidateJavaPath(string javaPath, string source = "Manual")
        {
            if (string.IsNullOrWhiteSpace(javaPath))
            {
                return new JavaInfo
                {
                    IsDetected = false,
                    Source = source,
                    ErrorMessage = "Java path is empty."
                };
            }

            string normalizedPath = NormalizeJavaPath(javaPath);
            if (!File.Exists(normalizedPath) || !string.Equals(Path.GetFileName(normalizedPath), "java.exe", StringComparison.OrdinalIgnoreCase))
            {
                return new JavaInfo
                {
                    IsDetected = false,
                    JavaPath = normalizedPath,
                    Source = source,
                    ErrorMessage = "Invalid Java path."
                };
            }

            return ReadVersion(normalizedPath, source);
        }

        private IEnumerable<JavaCandidate> GetCandidates(string configuredJavaPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredJavaPath))
            {
                yield return new JavaCandidate(configuredJavaPath, "Config");
            }

            string? javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                yield return new JavaCandidate(Path.Combine(javaHome, "bin", "java.exe"), "JAVA_HOME");
            }

            string? pathValue = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathValue))
            {
                foreach (string pathEntry in pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    yield return new JavaCandidate(Path.Combine(pathEntry.Trim(), "java.exe"), "PATH");
                }
            }

            foreach (JavaCandidate candidate in GetProgramFilesCandidates())
            {
                yield return candidate;
            }
        }

        private IEnumerable<JavaCandidate> GetProgramFilesCandidates()
        {
            string[] roots =
            {
                @"C:\Program Files\Java",
                @"C:\Program Files\Eclipse Adoptium",
                @"C:\Program Files\Microsoft",
                @"C:\Program Files\BellSoft"
            };

            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (string directory in SafeEnumerateDirectories(root))
                {
                    yield return new JavaCandidate(Path.Combine(directory, "bin", "java.exe"), root);

                    foreach (string childDirectory in SafeEnumerateDirectories(directory))
                    {
                        yield return new JavaCandidate(Path.Combine(childDirectory, "bin", "java.exe"), root);
                    }
                }
            }
        }

        private JavaInfo ReadVersion(string javaExePath, string source)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = javaExePath,
                        Arguments = "-version",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    process.Start();
                    if (!process.WaitForExit(VersionTimeoutMs))
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        return new JavaInfo
                        {
                            IsDetected = false,
                            JavaPath = javaExePath,
                            Source = source,
                            ErrorMessage = "Java version check timed out."
                        };
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    string combinedOutput = (output + Environment.NewLine + error).Trim();
                    string versionText = ExtractVersionText(combinedOutput);
                    return new JavaInfo
                    {
                        IsDetected = process.ExitCode == 0 && !string.IsNullOrWhiteSpace(versionText),
                        JavaPath = javaExePath,
                        VersionText = versionText,
                        MajorVersion = ParseMajorVersion(versionText),
                        Source = source,
                        ErrorMessage = string.IsNullOrWhiteSpace(versionText) ? "Java version could not be read." : string.Empty
                    };
                }
            }
            catch
            {
                return new JavaInfo
                {
                    IsDetected = false,
                    JavaPath = javaExePath,
                    Source = source,
                    ErrorMessage = "Java could not be started."
                };
            }
        }

        private string NormalizeJavaPath(string javaPath)
        {
            string trimmedPath = javaPath.Trim().Trim('"');
            if (Directory.Exists(trimmedPath))
            {
                return Path.Combine(trimmedPath, "java.exe");
            }

            return trimmedPath;
        }

        private string ExtractVersionText(string versionOutput)
        {
            if (string.IsNullOrWhiteSpace(versionOutput))
            {
                return string.Empty;
            }

            using (StringReader reader = new StringReader(versionOutput))
            {
                return reader.ReadLine() ?? string.Empty;
            }
        }

        private int ParseMajorVersion(string versionText)
        {
            Match quotedVersion = Regex.Match(versionText, "\"(?<version>[^\"]+)\"");
            string version = quotedVersion.Success ? quotedVersion.Groups["version"].Value : versionText;

            Match legacyVersion = Regex.Match(version, @"^1\.(?<major>\d+)");
            if (legacyVersion.Success && int.TryParse(legacyVersion.Groups["major"].Value, out int legacyMajor))
            {
                return legacyMajor;
            }

            Match modernVersion = Regex.Match(version, @"(?<major>\d+)");
            if (modernVersion.Success && int.TryParse(modernVersion.Groups["major"].Value, out int modernMajor))
            {
                return modernMajor;
            }

            return 0;
        }

        private IEnumerable<string> SafeEnumerateDirectories(string path)
        {
            try
            {
                return Directory.EnumerateDirectories(path);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private sealed class JavaCandidate
        {
            public JavaCandidate(string path, string source)
            {
                Path = path;
                Source = source;
            }

            public string Path { get; }

            public string Source { get; }
        }
    }
}
