using System;
using System.Windows;
using System.Windows.Media;

namespace LANShare.CSharp.ViewModels
{
    public static class ThemeManager
    {
        public static void ApplyTheme(string themeName)
        {
            bool isDark = !string.Equals(themeName, "Light", StringComparison.OrdinalIgnoreCase);

            var resources = Application.Current.Resources;

            if (isDark)
            {
                SetColor(resources, "WindowBackgroundBrush", Color.FromRgb(0x11, 0x11, 0x1B));
                SetColor(resources, "CardBackgroundBrush", Color.FromRgb(0x18, 0x18, 0x25));
                SetColor(resources, "SubCardBackgroundBrush", Color.FromRgb(0x1E, 0x1E, 0x2E));
                SetColor(resources, "BorderBrush", Color.FromRgb(0x31, 0x32, 0x44));
                SetColor(resources, "TextPrimaryBrush", Color.FromRgb(0xCD, 0xD6, 0xF4));
                SetColor(resources, "TextSecondaryBrush", Color.FromRgb(0xBA, 0xC2, 0xDE));
                SetColor(resources, "TextMutedBrush", Color.FromRgb(0xA6, 0xAD, 0xC8));
                SetColor(resources, "AccentColorBrush", Color.FromRgb(0x89, 0xB4, 0xFA));
                SetColor(resources, "AccentSecondaryBrush", Color.FromRgb(0xCB, 0xA6, 0xF7));
            }
            else
            {
                SetColor(resources, "WindowBackgroundBrush", Color.FromRgb(0xEF, 0xF1, 0xF5));
                SetColor(resources, "CardBackgroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetColor(resources, "SubCardBackgroundBrush", Color.FromRgb(0xE6, 0xE9, 0xEF));
                SetColor(resources, "BorderBrush", Color.FromRgb(0xBC, 0xC0, 0xCC));
                SetColor(resources, "TextPrimaryBrush", Color.FromRgb(0x4C, 0x4F, 0x69));
                SetColor(resources, "TextSecondaryBrush", Color.FromRgb(0x5C, 0x5F, 0x77));
                SetColor(resources, "TextMutedBrush", Color.FromRgb(0x6C, 0x6F, 0x85));
                SetColor(resources, "AccentColorBrush", Color.FromRgb(0x1E, 0x66, 0xF5));
                SetColor(resources, "AccentSecondaryBrush", Color.FromRgb(0x88, 0x39, 0xEF));
            }
        }

        private static void SetColor(ResourceDictionary resources, string key, Color color)
        {
            resources[key] = new SolidColorBrush(color);
        }
    }
}
