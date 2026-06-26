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
        private const int VersionTimeoutMs = 3000;

        public JavaInfo Detect(string configuredJavaPath)
        {
            string firstError = string.Empty;

            foreach (JavaCandidate candidate in GetCandidates(configuredJavaPath))
            {
                JavaInfo javaInfo = ValidateJavaPath(candidate.Path, candidate.Source);
                if (javaInfo.IsDetected)
                {
                    return javaInfo;
                }

                if (string.IsNullOrWhiteSpace(firstError) && candidate.Source == "Config")
                {
                    firstError = javaInfo.ErrorMessage;
                }
            }

            return new JavaInfo
            {
                IsDetected = false,
                Source = string.IsNullOrWhiteSpace(firstError) ? "Not Found" : "Config",
                ErrorMessage = string.IsNullOrWhiteSpace(firstError)
                    ? "Java could not be detected."
                    : firstError
            };
        }

        public JavaInfo ValidateJavaPath(string javaPath, string source)
        {
            string normalizedPath = NormalizeJavaPath(javaPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return CreateInvalid(source, "Java path is empty.");
            }

            if (!File.Exists(normalizedPath))
            {
                return CreateInvalid(source, "Invalid Java path.");
            }

            if (!string.Equals(Path.GetFileName(normalizedPath), "java.exe", StringComparison.OrdinalIgnoreCase))
            {
                return CreateInvalid(source, "Java path must point to java.exe.");
            }

            return ReadJavaVersion(normalizedPath, source);
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
                foreach (string pathEntry in pathValue.Split(Path.PathSeparator))
                {
                    if (!string.IsNullOrWhiteSpace(pathEntry))
                    {
                        yield return new JavaCandidate(Path.Combine(pathEntry.Trim(), "java.exe"), "PATH");
                    }
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

                foreach (string candidate in GetShallowJavaPaths(root))
                {
                    yield return new JavaCandidate(candidate, root);
                }
            }
        }

        private IEnumerable<string> GetShallowJavaPaths(string root)
        {
            string directJava = Path.Combine(root, "java.exe");
            if (File.Exists(directJava))
            {
                yield return directJava;
            }

            string directBinJava = Path.Combine(root, "bin", "java.exe");
            if (File.Exists(directBinJava))
            {
                yield return directBinJava;
            }

            string[] children;
            try
            {
                children = Directory.GetDirectories(root);
            }
            catch
            {
                yield break;
            }

            foreach (string child in children)
            {
                string childJava = Path.Combine(child, "java.exe");
                if (File.Exists(childJava))
                {
                    yield return childJava;
                }

                string childBinJava = Path.Combine(child, "bin", "java.exe");
                if (File.Exists(childBinJava))
                {
                    yield return childBinJava;
                }
            }
        }

        private JavaInfo ReadJavaVersion(string javaPath, string source)
        {
            try
            {
                using Process process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();
                System.Threading.Tasks.Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                System.Threading.Tasks.Task<string> errorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(VersionTimeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    return CreateInvalid(source, "Java version check timed out.");
                }

                string output = outputTask.Result;
                string error = errorTask.Result;
                string versionOutput = $"{error}\n{output}".Trim();
                if (string.IsNullOrWhiteSpace(versionOutput))
                {
                    return CreateInvalid(source, "Java version output was empty.");
                }

                string versionText = ExtractVersionText(versionOutput);
                return new JavaInfo
                {
                    IsDetected = true,
                    JavaPath = javaPath,
                    VersionText = versionText,
                    MajorVersion = ParseMajorVersion(versionText),
                    Source = source,
                    ErrorMessage = string.Empty
                };
            }
            catch
            {
                return CreateInvalid(source, "Java version check failed.");
            }
        }

        private static string NormalizeJavaPath(string javaPath)
        {
            if (string.IsNullOrWhiteSpace(javaPath))
            {
                return string.Empty;
            }

            string trimmedPath = javaPath.Trim().Trim('"');
            if (Directory.Exists(trimmedPath))
            {
                return Path.Combine(trimmedPath, "bin", "java.exe");
            }

            return trimmedPath;
        }

        private static string ExtractVersionText(string versionOutput)
        {
            using StringReader reader = new StringReader(versionOutput);
            string? firstLine = reader.ReadLine();
            return string.IsNullOrWhiteSpace(firstLine) ? versionOutput : firstLine.Trim();
        }

        private static int ParseMajorVersion(string versionText)
        {
            Match match = Regex.Match(versionText, "\"(?<version>[^\"]+)\"");
            string rawVersion = match.Success ? match.Groups["version"].Value : versionText;
            Match numberMatch = Regex.Match(rawVersion, @"(?<major>\d+)(?:\.(?<minor>\d+))?");
            if (!numberMatch.Success)
            {
                return 0;
            }

            if (!int.TryParse(numberMatch.Groups["major"].Value, out int major))
            {
                return 0;
            }

            if (major == 1 && int.TryParse(numberMatch.Groups["minor"].Value, out int legacyMajor))
            {
                return legacyMajor;
            }

            return major;
        }

        private static JavaInfo CreateInvalid(string source, string message)
        {
            return new JavaInfo
            {
                IsDetected = false,
                Source = source,
                ErrorMessage = message
            };
        }

        private readonly struct JavaCandidate
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
