using System;
using System.IO;
using System.Text.Json;

namespace LANShare.CSharp.Models
{
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LANShare.CSharp",
            "settings.json"
        );

        public string DeviceName { get; set; } = Environment.MachineName;
        public string Theme { get; set; } = "Dark";
        public string DownloadDirectory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "LANShareDownloads"
        );
        public int BroadcastPort { get; set; } = 45454;
        public int TransferPort { get; set; } = 45455;
        public int BroadcastIntervalMs { get; set; } = 3000;
        public int FileBufferSizeKb { get; set; } = 64;

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        EnsureDownloadDirectory(settings.DownloadDirectory);
                        return settings;
                    }
                }
            }
            catch
            {
                // Fallback to defaults if load fails
            }

            var defaultSettings = new AppSettings();
            EnsureDownloadDirectory(defaultSettings.DownloadDirectory);
            defaultSettings.Save();
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                EnsureDownloadDirectory(DownloadDirectory);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Ignore save errors on write
            }
        }

        private static void EnsureDownloadDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch
            {
                // Ignore path errors
            }
        }
    }
}
