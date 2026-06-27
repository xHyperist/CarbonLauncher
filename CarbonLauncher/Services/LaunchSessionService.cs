using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LaunchSessionService
    {
        public LaunchSession CreateGuestSession(string username)
        {
            string validationError = ValidateOfflineIgn(username);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return CreateInvalidGuestSession(username, validationError);
            }

            return new LaunchSession
            {
                IsAuthenticated = false,
                IsGuestMode = true,
                Username = username.Trim(),
                Uuid = CreateOfflineUuid(username.Trim()),
                AccessToken = "0",
                UserType = "legacy",
                SessionType = "offline",
                CreatedAt = DateTime.Now
            };
        }

        public LaunchSession CreateInvalidGuestSession(string username, string errorMessage)
        {
            return new LaunchSession
            {
                IsAuthenticated = false,
                IsGuestMode = true,
                Username = username ?? string.Empty,
                AccessToken = "0",
                UserType = "legacy",
                SessionType = "offline",
                CreatedAt = DateTime.Now,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "Player IGN is invalid."
                    : errorMessage
            };
        }

        private static string CreateOfflineUuid(string username)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes("OfflinePlayer:" + username);
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(inputBytes);
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

                return FormatUuid(hash);
            }
        }

        private static string FormatUuid(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            return string.Concat(
                hex.Substring(0, 8),
                "-",
                hex.Substring(8, 4),
                "-",
                hex.Substring(12, 4),
                "-",
                hex.Substring(16, 4),
                "-",
                hex.Substring(20, 12));
        }

        private static string ValidateOfflineIgn(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Player IGN is required.";
            }

            if (username.Length < 3 || username.Length > 16)
            {
                return "Player IGN must be 3-16 characters.";
            }

            return Regex.IsMatch(username, "^[A-Za-z0-9_]+$")
                ? string.Empty
                : "Player IGN can only contain A-Z, a-z, 0-9, and underscore. Turkish characters, spaces and special characters are not allowed.";
        }
    }
}
